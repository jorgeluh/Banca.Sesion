// <copyright file="EnvoltorioConexionRedis.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.SessionState;
    using StackExchange.Redis;

    /// <summary>
    /// Clase basada en
    /// <see href="https://github.com/Azure/aspnet-redis-providers/blob/main/src/RedisSessionStateProvider/RedisConnectionWrapper.cs">RedisConnectionWrapper.cs</see>
    /// que tiene las operaciones necesarias para la implementación de un proveedor de estado de sesión de .NET Framework basado en Redis.
    /// </summary>
    public class EnvoltorioConexionRedis : IConexionAlmacen
    {
        /// <summary>
        /// Comando de Redis que consulta los datos de la sesión asociada al identificador de sesión de la petición actual y que además crea
        /// una llave de bloqueo que impide leer los valores mientras exista. Al terminar extiende la expiración de las llaves existentes.
        /// </summary>
        /// <remarks>
        /// El concepto de bloqueo de sesión de .NET Framework no existe en la sesión de .NET por lo que no va a respetar la llave adicional
        /// que se crea con esta implementación.
        /// </remarks>
        private const string ComandoEscribirBloqueoObtenerDatos = @"
            local retArray = {}
            local llaveNet = redis.call('GET', KEYS[1])
            local datosSesion = redis.call('HMGET', llaveNet, ARGV[1], ARGV[2])
            local lockValue = ARGV[3]
            local locked = redis.call('SETNX', KEYS[2], ARGV[3])
            local IsLocked = true

            if locked == 0 then
                lockValue = redis.call('GET', KEYS[2])
            else
                redis.call('EXPIRE', KEYS[2], ARGV[4])
                IsLocked = false
            end

            retArray[1] = lockValue
            if lockValue == ARGV[3] then retArray[2] = datosSesion[2] else retArray[2] = '' end

            local SessionTimeout = datosSesion[1]
            if SessionTimeout ~= false then
                SessionTimeout = SessionTimeout / 10000000
                retArray[3] = SessionTimeout
                redis.call('EXPIRE', KEYS[1], SessionTimeout)
                redis.call('EXPIRE', llaveNet, SessionTimeout)
                redis.call('EXPIRE', KEYS[2], SessionTimeout)
            else
                retArray[3] = -1
            end

            retArray[4] = IsLocked
            return retArray";

        /// <summary>
        /// Comando que consulta tanto los datos como el identificador del bloqueo. Al terminar extiende la expiración de las llaves
        /// existentes.
        /// </summary>
        private const string ComandoLeerBloqueoYObtenerDatos = @"
            local llaveNet = redis.call('GET', KEYS[1])
            local datosSesion = redis.call('HMGET', llaveNet, ARGV[1], ARGV[2])
            local retArray = {}
            local lockValue = ''
            local writeLockValue = redis.call('GET', KEYS[2])
            if writeLockValue ~= false then
                lockValue = writeLockValue
            end

            retArray[1] = lockValue
            if lockValue == '' then retArray[2] = datosSesion[2] else retArray[2] = '' end
            local SessionTimeout = datosSesion[1]
            if SessionTimeout ~= false then
                SessionTimeout = SessionTimeout / 10000000
                retArray[3] = SessionTimeout
                redis.call('EXPIRE', llaveNet, SessionTimeout)
                redis.call('EXPIRE', KEYS[1], SessionTimeout)
                redis.call('EXPIRE', KEYS[2], SessionTimeout)
            else
                retArray[3] = '-1'
            end

            return retArray";

        /// <summary>
        /// Comando que elimina el bloqueo si su identificador coincide con el recibido. Al terminar extiende la expiración de las llaves
        /// existentes.
        /// </summary>
        private const string ComandoLiberarBloqueoEscrituraSiIdentificadorBloqueoCoincide = @"
            local llaveNet = redis.call('GET', KEYS[1])
            local writeLockValueFromCache = redis.call('GET', KEYS[2])
            if writeLockValueFromCache == ARGV[2] then
                redis.call('DEL', KEYS[2])
            end

            local SessionTimeout = redis.call('HMGET', llaveNet, ARGV[1])[1]
            if SessionTimeout and (type(SessionTimeout) == 'number' or type(SessionTimeout) == 'string') then
                SessionTimeout = SessionTimeout / 10000000
            else
                SessionTimeout = ARGV[3]
            end

            redis.call('EXPIRE', llaveNet, SessionTimeout)
            redis.call('EXPIRE', KEYS[1], SessionTimeout)
            return 1";

        /// <summary>
        /// Comando que extiende la expiración de las llaves existentes.
        /// </summary>
        private const string ComandoActualizarTiempoParaExpirar = @"
            local llaveNet = redis.call('GET', KEYS[1])
            local dataExists = redis.call('EXISTS', llaveNet)
            if dataExists == 0 then
                return 1
            end

            local SessionTimeout = redis.call('HMGET', llaveNet, ARGV[1])[1]
            if SessionTimeout then
                SessionTimeout = SessionTimeout / 10000000
            else
                SessionTimeout = ARGV[2] * 10000000
                redis.call('HSET', llaveNet, ARGV[1], SessionTimeout)
            end

            redis.call('EXPIRE', llaveNet, SessionTimeout)
            redis.call('EXPIRE', KEYS[1], SessionTimeout)
            return 1";

        /// <summary>
        /// Comando que actualiza los valores de los elementos de estado de sesión y extiende la expiración de las llaves existentes.
        /// </summary>
        private const string ComandoFijar = @"
            local llaveNet = redis.call('GET', KEYS[1])
            local SessionTimeout = ARGV[4] * 10000000
            redis.call('HSET', llaveNet, ARGV[1], ARGV[2], ARGV[3], SessionTimeout)
            redis.call('EXPIRE', llaveNet, ARGV[4])
            redis.call('EXPIRE', KEYS[1], ARGV[4])
            return 1";

        /// <summary>
        /// Actualiza el tiempo de expiración de las llaves de estado de sesión en Redis y extiende su expiración para que coincida con ese
        /// tiempo. También elimina la llave de bloqueo.
        /// </summary>
        private const string ComandoEliminarBloqueoYActualizarDatosSesion = @"
            if ARGV[3] ~= '' then
                local writeLockValueFromCache = redis.call('GET', KEYS[2])
                if writeLockValueFromCache ~= ARGV[3] then
                    return 1
                end
            end

            local llaveNet = redis.call('GET', KEYS[1])
            if tonumber(ARGV[8]) ~= 0 then redis.call('HSET', llaveNet, ARGV[2], ARGV[12]) end
            redis.call('HSET', llaveNet, ARGV[1], ARGV[4] * 10000000)
            redis.call('EXPIRE', KEYS[1], ARGV[4])
            redis.call('EXPIRE', llaveNet, ARGV[4])
            redis.call('DEL', KEYS[2])";

        /// <summary>
        /// Comando que elimina todas las llaves de sesión.
        /// </summary>
        private const string ComandoEliminarSesion = @"
            if ARGV[1] ~= '' then
                local lockValue = redis.call('GET', KEYS[2])
                if lockValue ~=  ARGV[1] then
                    return 1
                end
            end

            local llaveNet = redis.call('GET', KEYS[1])
            redis.call('DEL', KEYS[1])
            redis.call('DEL', llaveNet)
            redis.call('DEL', KEYS[2])";

        /// <summary>
        /// Objeto usado para bloquear la creación del atributo <see cref="conexionCompartida"/>.
        /// </summary>
        private static readonly object BloqueoConexionCompartida = new object();

        /// <summary>
        /// Provee la conexión hacia Redis.
        /// </summary>
        private static ConexionCompartidaRedis conexionCompartida;

        /// <summary>
        /// Provee operaciones para ejecutar comandos en Redis y consultar las propiedades de la sesión.
        /// </summary>
        private readonly IConexionClienteRedis conexionRedis;

        /// <summary>
        /// Generador de llaves y campos usados para leer y escribir los valores del estado de sesión en Redis.
        /// </summary>
        private readonly GeneradorLlaves generadorLlaves;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="EnvoltorioConexionRedis"/>.
        /// </summary>
        /// <param name="configuracion">La configuración con los parámetros necesarios para establecer la conexión con Redis.</param>
        /// <param name="identificadorSesionNet">El identificador de sesión obtenido de la cookie de sesión de .NET. Este no debe tener
        /// ninguna codificación para evitar problemas al enviarlo a la API de enlace de sesión.</param>
        /// <param name="identificadorSesionNetFramework">El identificador de la sesión de .NET Framework.</param>
        /// <param name="existeCookieEnlace">Indica si ya existe la cookie de enlace en la petición que se recibió (<c>true</c>) o no.
        /// </param>
        /// <param name="cookieEnlace">La cookie bandera de enlace de sesión. Es <c>null</c> si no es necesario agregarla a la
        /// respuesta.</param>
        public EnvoltorioConexionRedis(
            IProveedorConfiguracion configuracion,
            string identificadorSesionNet,
            string identificadorSesionNetFramework,
            bool existeCookieEnlace,
            out HttpCookie cookieEnlace)
        {
            EnlazadorSesion enlazadorSesion = new EnlazadorSesion(identificadorSesionNet, configuracion, existeCookieEnlace);
            this.generadorLlaves = new GeneradorLlaves(identificadorSesionNetFramework, enlazadorSesion, out cookieEnlace);

            if (conexionCompartida == null)
            {
                lock (BloqueoConexionCompartida)
                {
                    if (conexionCompartida == null)
                    {
                        conexionCompartida = new ConexionCompartidaRedis(configuracion);
                    }
                }
            }

            this.conexionRedis = new ConexionClienteRedis(configuracion, conexionCompartida);
        }

        /// <summary>
        /// Actualiza el tiempo de expiración de las llaves en Redis para extenderlo.
        /// </summary>
        /// <param name="segundosParaExpirar">El tiempo de vida de la sesión en segundos.</param>
        /// <returns>Una tarea que permite esperar a que se actualice el tiempo de vida de la sesión.</returns>
        public Task ActualizarTiempoExpiracionAsync(int segundosParaExpirar)
        {
            string[] llaves = new string[] { this.generadorLlaves.LlaveSesion };
            object[] valores = new object[] { GeneradorLlaves.CampoExpiracionSesion, segundosParaExpirar };
            return this.conexionRedis.EvaluarAsync(ComandoActualizarTiempoParaExpirar, llaves, valores);
        }

        /// <summary>
        /// Actualiza el valor de los elementos de estado de sesión en Redis.
        /// </summary>
        /// <param name="datos">La colección de elementos de estado de sesión cuyos valores se van a actualizar en Redis.</param>
        /// <param name="segundosEsperaSesion">El tiempo de vida de la sesión en segundos.</param>
        /// <returns>Una tarea que permite esperar que se actualicen los datos en Redis.</returns>
        public async Task FijarAsync(ISessionStateItemCollection datos, int segundosEsperaSesion)
        {
            if (this.PrepararFijar(datos, segundosEsperaSesion, out string[] llaves, out object[] valores))
            {
                await this.conexionRedis.EvaluarAsync(ComandoFijar, llaves, valores);
            }
        }

        /// <summary>
        /// Actualiza los valores de los elementos de estado de sesión en Redis y elimina la llave del bloqueo.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo que se va a eliminar.</param>
        /// <param name="datos">La colección de elementos de estado de sesión que se va a actualizar en Redis.</param>
        /// <param name="segundosEsperaSesion">Los segundos de tiempo de espera de la sesión sin peticiones antes que sea
        /// descartada.</param>
        /// <returns>Una tarea que permite esperar que se actualicen los valores y se elimine el bloqueo en Redis.</returns>
        public async Task IntentarActualizarYLiberarBloqueoAsync(
            object identificadorBloqueo, ISessionStateItemCollection datos, int segundosEsperaSesion)
        {
            if (this.PrepararIntentarActualizarYLiberarBloqueo(
                identificadorBloqueo, datos, segundosEsperaSesion, out string[] llaves, out object[] valores))
            {
                await this.conexionRedis.EvaluarAsync(ComandoEliminarBloqueoYActualizarDatosSesion, llaves, valores);
            }
        }

        /// <summary>
        /// Elimina todas las llaves pertenecientes a la sesión en Redis.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo a eliminar.</param>
        /// <returns>Una tarea que permite esperar que se eliminen las llaves de sesión y sus bloqueos en Redis.</returns>
        public Task IntentarEliminarYLiberarBloqueoAsync(object identificadorBloqueo)
        {
            string[] llaves = new string[] { this.generadorLlaves.LlaveSesion, this.generadorLlaves.LlaveBloqueo };
            identificadorBloqueo = identificadorBloqueo ?? string.Empty;
            object[] valores = { identificadorBloqueo.ToString() };
            return this.conexionRedis.EvaluarAsync(ComandoEliminarSesion, llaves, valores);
        }

        /// <summary>
        /// Elimina el bloqueo si su identificador coincide con el proporcionado. Luego extiende la expiración de las llaves de la sesión.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo a eliminar.</param>
        /// <param name="segundosEsperaSesion">La duración en segundos que se extenderá la validez de las llaves de la sesión.</param>
        /// <returns>Una tarea que permite esperar que se libere el bloqueo en Redis.</returns>
        public Task IntentarLiberarBloqueoSiIdentificadorBloqueoCoincideAsync(object identificadorBloqueo, int segundosEsperaSesion)
        {
            string[] llaves = new string[] { this.generadorLlaves.LlaveSesion, this.generadorLlaves.LlaveBloqueo };
            object[] valores = new object[] { GeneradorLlaves.CampoExpiracionSesion, identificadorBloqueo, segundosEsperaSesion };
            return this.conexionRedis.EvaluarAsync(ComandoLiberarBloqueoEscrituraSiIdentificadorBloqueoCoincide, llaves, valores);
        }

        /// <summary>
        /// Crea una llave de bloqueo para la sesión y consulta los datos de los elementos de estado de sesión.
        /// </summary>
        /// <param name="horaBloqueo">La hora en la que inicia el bloqueo.</param>
        /// <param name="segundosEsperaBloqueo">La duración del bloqueo en segundos.</param>
        /// <returns>Una tarea cuyo resultado es un objeto que contiene los elementos de estado de sesión y del bloqueo en Redis.</returns>
        public async Task<DatosSesion> IntentarTomarBloqueoEscrituraYObtenerDatosAsync(DateTime horaBloqueo, int segundosEsperaBloqueo)
        {
            string identificadorBloqueoEsperado = horaBloqueo.Ticks.ToString();
            string[] llaves = new string[] { this.generadorLlaves.LlaveSesion, this.generadorLlaves.LlaveBloqueo };
            object[] valores = new object[]
                {
                    GeneradorLlaves.CampoExpiracionSesion,
                    GeneradorLlaves.CampoDatos,
                    identificadorBloqueoEsperado,
                    segundosEsperaBloqueo,
                };
            RedisResult datosSesionDesdeRedis = await this.conexionRedis.EvaluarAsync(ComandoEscribirBloqueoObtenerDatos, llaves, valores);

            bool seTomoBloqueo = false;
            ISessionStateItemCollection elementosEstadoSesion = null;
            object identificadorBloqueo = this.conexionRedis.ObtenerIdentificadorBloqueo(datosSesionDesdeRedis);
            int segundosEsperaSesion = this.conexionRedis.ObtenerSegundosEsperaSesion(datosSesionDesdeRedis);
            bool estaBloqueada = this.conexionRedis.EstaBloqueada(datosSesionDesdeRedis);
            if (!estaBloqueada && identificadorBloqueo.ToString().Equals(identificadorBloqueoEsperado))
            {
                seTomoBloqueo = true;
                elementosEstadoSesion = this.conexionRedis.ObtenerDatosSesion(datosSesionDesdeRedis);
            }

            return new DatosSesion(seTomoBloqueo, estaBloqueada, identificadorBloqueo, segundosEsperaSesion, elementosEstadoSesion);
        }

        /// <summary>
        /// Comprueba si el identificador de bloqueo recibido es el mismo en Redis y consulta los valores de los elementos de estado de
        /// sesión.
        /// </summary>
        /// <returns>Una tarea cuyo resultado es un objeto que contiene los elementos de estado de sesión y del bloqueo en Redis.</returns>
        public async Task<DatosSesion> IntentarVerificarBloqueoEscrituraYObtenerDatosAsync()
        {
            string[] llaves = new string[] { this.generadorLlaves.LlaveSesion, this.generadorLlaves.LlaveBloqueo };
            object[] valores = new object[] { GeneradorLlaves.CampoExpiracionSesion, GeneradorLlaves.CampoDatos };
            RedisResult datosSesionDesdeRedis = await this.conexionRedis.EvaluarAsync(ComandoLeerBloqueoYObtenerDatos, llaves, valores);

            bool seTomoBloqueo = false;
            ISessionStateItemCollection elementosEstadoSesion = null;
            object identificadorBloqueo = this.conexionRedis.ObtenerIdentificadorBloqueo(datosSesionDesdeRedis);
            int segundosEsperaSesion = this.conexionRedis.ObtenerSegundosEsperaSesion(datosSesionDesdeRedis);
            if (string.Empty.Equals(identificadorBloqueo.ToString()))
            {
                identificadorBloqueo = null;
                seTomoBloqueo = true;
                elementosEstadoSesion = this.conexionRedis.ObtenerDatosSesion(datosSesionDesdeRedis);
            }

            return new DatosSesion(seTomoBloqueo, false, identificadorBloqueo, segundosEsperaSesion, elementosEstadoSesion);
        }

        /// <summary>
        /// Obtiene el tiempo transcurrido desde que se estableció el bloqueo con el identificador dado.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo cuya antigüedad se consulta.</param>
        /// <returns>El tiempo transcurrido desde que se estableció el bloqueo indicado.</returns>
        public TimeSpan ObtenerTiempoBloqueo(object identificadorBloqueo)
        {
            string ticksHoraBloqueoDesdeIdentificador = identificadorBloqueo.ToString();
            if (long.TryParse(ticksHoraBloqueoDesdeIdentificador, out long ticksBloqueo))
            {
                return DateTime.Now.Subtract(new DateTime(ticksBloqueo, DateTimeKind.Unspecified));
            }
            else
            {
                return DateTime.Now.Subtract(new DateTime(0, DateTimeKind.Unspecified));
            }
        }

        /// <summary>
        /// Vuelve a generar los identificadores de las llaves de bloqueo y de sesión si el identificador de sesión ha sido modificado. Esto
        /// obliga a enlazar la sesión de nuevo.
        /// </summary>
        /// <param name="identificadorSesion">El nuevo identificador de sesión.</param>
        /// <returns>Una tarea cuyo resutlado es la cookie bandera de enlace de sesión.</returns>
        public Task<HttpCookie> RegenerarCadenaLlaveSiIdentificadorModificadoAsync(string identificadorSesion)
        {
            return this.generadorLlaves.RegenerarCadenaLlaveSiIdentificadorModificadoAsync(identificadorSesion);
        }

        /// <summary>
        /// Prepara los argumentos para ejecutar el comando que actualiza los valores de los elementos de estado de sesión en Redis.
        /// </summary>
        /// <param name="datos">La colección de elementos de estado de sesión que se van a grabar en Redis.</param>
        /// <param name="segundosEsperaSesion">Los segundos de tiempo de vida de las llaves de sesión.</param>
        /// <param name="llaves">Las llaves para usar para el comando de Redis.</param>
        /// <param name="valores">Los argumentos que se van a usar para el comando de Redis.</param>
        /// <returns><c>true</c> si las llaves y argumentos para el comando de Redis se pudieron crear sin problemas.</returns>
        private bool PrepararFijar(
            ISessionStateItemCollection datos, int segundosEsperaSesion, out string[] llaves, out object[] valores)
        {
            llaves = null;
            valores = null;
            try
            {
                byte[] coleccionElementosSesionSerializada = this.SerializarColeccionElementosEstadoSesion(datos);
                llaves = new string[] { this.generadorLlaves.LlaveSesion };
                valores = new object[]
                {
                    GeneradorLlaves.CampoDatos,
                    coleccionElementosSesionSerializada,
                    GeneradorLlaves.CampoExpiracionSesion,
                    segundosEsperaSesion,
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prepara los argumentos para el comando que actualiza los valores de los elementos de estado de sesión y libera el bloqueo en
        /// Redis.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo a liberar.</param>
        /// <param name="datos">La colección de elementos de estado de sesión por actualizar en Redis.</param>
        /// <param name="segundosEsperaSesion">El tiempo de validez de las llaves de sesión en segundos.</param>
        /// <param name="llaves">Las llaves para usar para el comando de Redis.</param>
        /// <param name="valores">Los argumentos que se van a usar para el comando de Redis.</param>
        /// <returns><c>true</c> si las llaves y argumentos para el comando de Redis se pudieron crear sin problemas.</returns>
        private bool PrepararIntentarActualizarYLiberarBloqueo(
            object identificadorBloqueo,
            ISessionStateItemCollection datos,
            int segundosEsperaSesion,
            out string[] llaves,
            out object[] valores)
        {
            llaves = null;
            valores = null;
            if (datos != null)
            {
                List<object> valoresEstadoSesion = new List<object>();
                int cantidadElementosEliminados = 0;
                int cantidadElementosActualizados = 1;
                byte[] coleccionElementosEstadoSesionSerializada = this.SerializarColeccionElementosEstadoSesion(datos);
                valoresEstadoSesion.Add("EstadoSesion");
                valoresEstadoSesion.Add(coleccionElementosEstadoSesionSerializada);

                llaves = new string[] { this.generadorLlaves.LlaveSesion, this.generadorLlaves.LlaveBloqueo };
                valores = new object[valoresEstadoSesion.Count + 10];
                valores[0] = GeneradorLlaves.CampoExpiracionSesion;
                valores[1] = GeneradorLlaves.CampoDatos;
                valores[2] = identificadorBloqueo ?? string.Empty;
                valores[3] = segundosEsperaSesion;
                valores[4] = cantidadElementosEliminados;
                valores[5] = 9;
                valores[6] = cantidadElementosEliminados + 8;
                valores[7] = cantidadElementosActualizados;
                valores[8] = cantidadElementosEliminados + 9;
                valores[9] = valoresEstadoSesion.Count + 8;

                valoresEstadoSesion.CopyTo(valores, 10);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serializa la colección de elementos de estado de sesión para que su valor pueda ser actualizado en Redis.
        /// </summary>
        /// <param name="coleccionElementosEstadoSesion">La colección de elementos de estado de sesión a serializar.</param>
        /// <returns>La colección de elementos de estado de sesión serializados como un vector de bytes.</returns>
        private byte[] SerializarColeccionElementosEstadoSesion(ISessionStateItemCollection coleccionElementosEstadoSesion)
        {
            if (coleccionElementosEstadoSesion == null)
            {
                return null;
            }

            using (MemoryStream flujoMemoria = new MemoryStream())
            {
                using (BinaryWriter escritorBinario = new BinaryWriter(flujoMemoria))
                {
                    ((ColeccionElementosEstadoSesion)coleccionElementosEstadoSesion).Serializar(escritorBinario);
                    escritorBinario.Close();
                    return flujoMemoria.ToArray();
                }
            }
        }
    }
}
