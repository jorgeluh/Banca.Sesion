// <copyright file="ConexionClienteRedis.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.IO;
#if !NET461
    using System.Threading.Tasks;
#endif
    using System.Web.SessionState;
    using StackExchange.Redis;

    /// <summary>
    /// Implementa las funcionalidades básicas en Redis para poder usarlo como un almacén de estado de sesión.
    /// </summary>
    internal class ConexionClienteRedis : IConexionClienteRedis
    {
        /// <summary>
        /// Proveedor de configuración con los valores de tiempos de espera.
        /// </summary>
        private readonly IProveedorConfiguracion configuracion;

        /// <summary>
        /// La conexión hascia la instancia de Redis que fue configurada.
        /// </summary>
        private readonly ConexionCompartidaRedis conexionCompartida;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ConexionClienteRedis"/>.
        /// </summary>
        /// <param name="configuracion">Proveedor de configuración con los valores de tiempos de espera.</param>
        /// <param name="conexionCompartida">La conexión hascia la instancia de Redis que fue configurada.</param>
        public ConexionClienteRedis(IProveedorConfiguracion configuracion, ConexionCompartidaRedis conexionCompartida)
        {
            this.configuracion = configuracion;
            this.conexionCompartida = conexionCompartida;
        }

        /// <summary>
        /// Obtiene un acceso directo a la conexión de Redis.
        /// </summary>
        private IDatabase ConexionReal => this.conexionCompartida.Conexion;

        /// <summary>
        /// Ejecuta un script de comandos en Redis con las llaves y argumentos indicados.
        /// </summary>
        /// <param name="comando">El comando de Redis.</param>
        /// <param name="llaves">Las llaves a emplear dentro del script del comando de Redis. Se acceden por medio del vector <c>KEYS</c>.
        /// </param>
        /// <param name="valores">Los argumentos a emplear dentro del script del comando de Redis. Se acceden por medio del vector
        /// <c>ARGV</c>.</param>
#if !NET461
        /// <returns>Una tarea cuyo resultado es un objeto de tipo <see cref="RedisResult"/> que luego debe ser casteado según el tipo de
        /// resultado que se espera del script.</returns>
        public async Task<RedisResult> EvaluarAsync(string comando, string[] llaves, object[] valores)
#else
        /// <returns>Un objeto de tipo <see cref="RedisResult"/> que luego debe ser casteado según el tipo de resultado que se espera del
        /// script.</returns>
        public RedisResult Evaluar(string comando, string[] llaves, object[] valores)
#endif
        {
            RedisKey[] llavesRedis = new RedisKey[llaves.Length];
            RedisValue[] valoresRedis = new RedisValue[valores.Length];

            int i = 0;
            foreach (string llave in llaves)
            {
                llavesRedis[i++] = llave;
            }

            i = 0;
            foreach (object valor in valores)
            {
                valoresRedis[i++] = valor is byte[] v ? (RedisValue)v : (RedisValue)valor?.ToString();
            }

#if !NET461
            return await LogicaReintentos.EjecutarFuncionAsync(
                () => this.EjecutarOperacionAsync(comando, llavesRedis, valoresRedis), this.configuracion.TiempoEsperaReintentos);
#else
            return LogicaReintentos.EjecutarFuncion(
                () => this.EjecutarOperacion(comando, llavesRedis, valoresRedis), this.configuracion.TiempoEsperaReintentos);
#endif
        }

        /// <summary>
        /// Lee el identificador del bloqueo a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>El identificador del bloqueo de sesión.</returns>
        public string ObtenerIdentificadorBloqueo(RedisResult datosDesdeRedis)
        {
            RedisResult[] vectorValoresComandoBloqueo = (RedisResult[])datosDesdeRedis;
            return (string)vectorValoresComandoBloqueo[0];
        }

        /// <summary>
        /// Lee el tiempo de espera de la sesión a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>El tiempo de espera de la sesión en segundos.</returns>
        public int ObtenerSegundosEsperaSesion(RedisResult datosDesdeRedis)
        {
            RedisResult[] vectorValoresComandoBloqueo = (RedisResult[])datosDesdeRedis;
            int tiempoEsperaSesion = (int)vectorValoresComandoBloqueo[2];
            if (tiempoEsperaSesion != -1)
            {
                tiempoEsperaSesion = (int)this.configuracion.TiempoEsperaSesion.TotalSeconds;
            }

            tiempoEsperaSesion /= 60;
            return tiempoEsperaSesion;
        }

        /// <summary>
        /// Obtiene la bandera que indica si la sesión se encuentra bloqueada a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>Un valor que indica si la sesión se encuentra bloqueada (<c>true</c>) o no.</returns>
        public bool EstaBloqueada(object datosDesdeRedis)
        {
            RedisResult datosComoResultadoRedis = (RedisResult)datosDesdeRedis;
            RedisResult[] vectorValoresComandoBloqueo = (RedisResult[])datosComoResultadoRedis;
            return (bool)vectorValoresComandoBloqueo[3];
        }

        /// <summary>
        /// Recrea la colección de elementos de estado de sesión a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>La colección de elementos de estado de sesión que se encontraba almacenada en Redis.</returns>
        public ISessionStateItemCollection ObtenerDatosSesion(object datosDesdeRedis)
        {
            RedisResult datosComoResultadoRedis = (RedisResult)datosDesdeRedis;
            RedisResult[] vectorValoresComandoBloqueo = (RedisResult[])datosComoResultadoRedis;

            ISessionStateItemCollection datosSesion = null;
            if (vectorValoresComandoBloqueo.Length > 1 && vectorValoresComandoBloqueo[1] != null)
            {
                RedisResult datos = vectorValoresComandoBloqueo[1];
                RedisResult coleccionElementosEstadoSesionSerializada = datos;

                datosSesion = this.DeserializarColeccionElementosEstadoSesion(coleccionElementosEstadoSesionSerializada);
            }

            return datosSesion;
        }

        /// <summary>
        /// Ejecuta el comando de Redis con sus llaves y argumentos manejando diferentes tipos de excepción propios de Redis.
        /// </summary>
        /// <param name="comando">El comando de Redis.</param>
        /// <param name="llavesRedis">Las llaves a emplear dentro del script del comando de Redis convertidas a un tipo de dato específico
        /// para Redis.</param>
        /// <param name="valoresRedis">Los argumentos a emplear dentro del script del comando de Redis convertidos a un tipo de dato
        /// específico para Redis.</param>
#if !NET461
        /// <returns>Una tarea cuyo resultado es el resultado de ejecutar el comando en Redis.</returns>
        private Task<RedisResult> EjecutarOperacionAsync(string comando, RedisKey[] llavesRedis, RedisValue[] valoresRedis)
#else
        /// <returns>El resultado de ejecutar el comando en Redis.</returns>
        private RedisResult EjecutarOperacion(string comando, RedisKey[] llavesRedis, RedisValue[] valoresRedis)
#endif
        {
            try
            {
#if !NET461
                return this.ConexionReal.ScriptEvaluateAsync(comando, llavesRedis, valoresRedis);
#else
                return this.ConexionReal.ScriptEvaluate(comando, llavesRedis, valoresRedis);
#endif
            }
            catch (ObjectDisposedException)
            {
#if !NET461
                return this.ConexionReal.ScriptEvaluateAsync(comando, llavesRedis, valoresRedis);
#else
                return this.ConexionReal.ScriptEvaluate(comando, llavesRedis, valoresRedis);
#endif
            }
            catch (RedisConnectionException)
            {
                this.conexionCompartida.ForzarReconexion();
#if !NET461
                return this.ConexionReal.ScriptEvaluateAsync(comando, llavesRedis, valoresRedis);
#else
                return this.ConexionReal.ScriptEvaluate(comando, llavesRedis, valoresRedis);
#endif
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("NOSCRIPT"))
                {
#if !NET461
                    return this.ConexionReal.ScriptEvaluateAsync(comando, llavesRedis, valoresRedis);
#else
                    return this.ConexionReal.ScriptEvaluate(comando, llavesRedis, valoresRedis);
#endif
                }

                throw;
            }
        }

        /// <summary>
        /// Deserializa los datos de los elementos de estado de sesión recuperados desde Redis.
        /// </summary>
        /// <param name="coleccionElementosEstadoSesionSerializada">Los datos serializados como se encontraban en Redis.</param>
        /// <returns>La colección deserializada de elementos de estado de sesión.</returns>
        private ISessionStateItemCollection DeserializarColeccionElementosEstadoSesion(
            RedisResult coleccionElementosEstadoSesionSerializada)
        {
            try
            {
                using (MemoryStream flujoMemoria = new MemoryStream((byte[])coleccionElementosEstadoSesionSerializada))
                {
                    using (BinaryReader lector = new BinaryReader(flujoMemoria))
                    {
                        ColeccionElementosEstadoSesion coleccionElementosEstadoSesion = new ColeccionElementosEstadoSesion();
                        coleccionElementosEstadoSesion.Deserializar(lector);
                        return coleccionElementosEstadoSesion;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
