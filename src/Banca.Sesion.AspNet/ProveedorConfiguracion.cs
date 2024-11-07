// <copyright file="ProveedorConfiguracion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.AspNet
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Reflection;
    using System.Web;
    using System.Web.Configuration;
    using Banca.Sesion.Redis;

    /// <summary>
    /// Obtiene los parámetros necesarios para el funcionamiento del proveedor de estado de sesión en Redis. Estos se obtienen desde el
    /// archivo <c>web.config</c>.
    /// </summary>
    internal class ProveedorConfiguracion : IProveedorConfiguracion
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProveedorConfiguracion"/>.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        private ProveedorConfiguracion(NameValueCollection configuraciones)
        {
            this.CadenaConexion = ObtenerCadenaConexion(configuraciones);
            this.Huesped = ObtenerConfiguracionString(configuraciones, "huesped", "127.0.0.1");
            this.Puerto = ObtenerConfiguracionInt(configuraciones, "puerto", 0);
            this.ClaveAcceso = ObtenerConfiguracionString(configuraciones, "claveAcceso", null);
            this.UsarSsl = ObtenerConfiguracionBool(configuraciones, "ssl", true);
            this.IdentificadorBaseDatos = ObtenerConfiguracionInt(configuraciones, "identificadorBaseDatos", 0);
            this.NombreCookieSesionNet = ObtenerConfiguracionString(configuraciones, "nombreCookieSesionNet", ".AspNetCore.Session");
            this.MilisegundosTiempoEsperaConexion = ObtenerConfiguracionInt(configuraciones, "milisegundosTiempoEsperaConexion", 0);
            this.MilisegundosTiempoEsperaOperacion = ObtenerConfiguracionInt(configuraciones, "milisegundosTiempoEsperaOperacion", 0);
            int milisegundosTiempoEsperaReintentos = ObtenerConfiguracionInt(configuraciones, "milisegundosTiempoEsperaReintentos", 5000);
            this.TiempoEsperaReintentos = new TimeSpan(0, 0, 0, 0, milisegundosTiempoEsperaReintentos);
            HttpRuntimeSection seccionHttpRuntime = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            this.TiempoEsperaPeticion = seccionHttpRuntime.ExecutionTimeout;
            this.ArrojarConError = ObtenerConfiguracionBool(configuraciones, "arrojarConError", true);
            SessionStateSection seccionEstadoSesion = (SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");
            this.TiempoEsperaSesion = seccionEstadoSesion.Timeout;
            this.CookieEnlaceSegura = ObtenerConfiguracionBool(configuraciones, "cookieEnlaceSegura", true);
            this.CookieEnlaceMismoSitio = seccionEstadoSesion.CookieSameSite;
        }

        /// <summary>
        /// Obtiene los parámetros para la conexión con la instancia de Redis. En la cadena de conexión se pueden recibir los parámetros
        /// <see cref="Huesped"/>, <see cref="Puerto"/>, <see cref="ClaveAcceso"/>, <see cref="UsarSsl"/>,
        /// <see cref="IdentificadorBaseDatos"/>, <see cref="MilisegundosTiempoEsperaConexion"/> y
        /// <see cref="MilisegundosTiempoEsperaOperacion"/> por lo que es innecesario especificarlos si se proporciona la cadena de
        /// conexión.
        /// </summary>
        public string CadenaConexion { get; }

        /// <summary>
        /// Obtiene el nombre o IP del servidor del clúster donde se encuentra Redis.
        /// </summary>
        public string Huesped { get; }

        /// <summary>
        /// Obtiene el puerto en el que se encuentra disponible el servicio de Redis.
        /// </summary>
        public int Puerto { get; }

        /// <summary>
        /// Obtiene la contraseña para conectarse al servicio de Redis.
        /// </summary>
        public string ClaveAcceso { get; }

        /// <summary>
        /// Obtiene un valor que indica si se debe usar SSL para establecer la conexión con Redis (<c>true</c>) o no.
        /// </summary>
        public bool UsarSsl { get; }

        /// <summary>
        /// Obtiene el índice de la base de datos predeterminada de la conexión con Redis.
        /// </summary>
        public int IdentificadorBaseDatos { get; }

        /// <summary>
        /// Obtiene la cantidad de milisegundos que se espera que se establezca la conexión con Redis antes de generar un error de tiempo de
        /// espera.
        /// </summary>
        public int MilisegundosTiempoEsperaConexion { get; }

        /// <summary>
        /// Obtiene la cantidad de milisegundos que se espera la ejecución de los comandos síncronos en Redis antes de generar un error de
        /// tiempo de espera.
        /// </summary>
        public int MilisegundosTiempoEsperaOperacion { get; }

        /// <summary>
        /// Obtiene el tiempo máximo para ejecutar reintentos de las operaciones de conexión y enlace de sesiones de .NET y .NET Framework
        /// antes de lanzar la excepción que provoca los reintentos.
        /// </summary>
        public TimeSpan TiempoEsperaReintentos { get; }

        /// <summary>
        /// Obtiene el tiempo que la sesión puede transcurrir sin ser accedida antes que sea descartada.
        /// </summary>
        public TimeSpan TiempoEsperaSesion { get; }

        /// <summary>
        /// Obtiene el nombre de la cookie de sesión configurado para las aplicaciones de .NET.
        /// </summary>
        public string NombreCookieSesionNet { get; }

        /// <summary>
        /// Obtiene un valor que indica si las excepciones se relanzan (<c>true</c>) o si sólo se capturan para que no se sigan propagando.
        /// </summary>
        public bool ArrojarConError { get; }

        /// <summary>
        /// Obtiene el tiempo de espera máximo que se permite ejecutar esta petición.
        /// </summary>
        public TimeSpan TiempoEsperaPeticion { get; private set; }

        /// <summary>
        /// Obtiene un valor que indica si se debe agregar la propiedad <c>httponly</c> a la cookie de enlace (<c>true</c>) o no.
        /// </summary>
        public bool CookieEnlaceSoloHttp => true;

        /// <summary>
        /// Obtiene el valor para la propiedad <c>path</c> de la cookie de enlace.
        /// </summary>
        public string CookieEnlaceRuta => "/";

        /// <summary>
        /// Obtiene un valor que indica si se debe agregar la propiedad <c>secure</c> a la cookie de enlace (<c>true</c>) o no.
        /// </summary>
        public bool CookieEnlaceSegura { get; }

        /// <summary>
        /// Obtiene el modo para la propiedad <c>samesite</c> de la cookie de enlace.
        /// </summary>
        public SameSiteMode CookieEnlaceMismoSitio { get; }

        /// <summary>
        /// Crea una nueva instancia de la clase <see cref="ProveedorConfiguracion"/>.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        /// <returns>Una nueva instancia del proveedor de configuraciones con los valores asignados a sus propiedades.</returns>
        internal static ProveedorConfiguracion ProveedorConfiguracionParaEstadoSesion(NameValueCollection configuraciones)
        {
            return new ProveedorConfiguracion(configuraciones);
        }

        /// <summary>
        /// Construye la cadena de conexión hacia Redis a partir de los valores de las configuraciones.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        /// <returns>La cadena completa a usar como cadena de conexión de Redis.</returns>
        /// <exception cref="ConfigurationErrorsException">Si se especifica tanto la cadena de conexión como la clase y su método para
        /// obtener la cadena de conexión en las configuraciones de la aplicación.</exception>
        /// <exception cref="TypeLoadException">Si no se encontró la clase de configuración de cadena de conexión.</exception>
        /// <exception cref="MissingMemberException">Si la clase de configuración no tiene la función indicada para obtener la cadena de
        /// conexión.</exception>
        /// <exception cref="MissingMethodException">Si la función indicada para obtener la cadena de conexión no es estática o su tipo de
        /// dato de retorno no es <see cref="string"/>.</exception>
        private static string ObtenerCadenaConexion(NameValueCollection configuraciones)
        {
            string nombreClaseConfiguraciones = ObtenerConfiguracionString(configuraciones, "nombreClaseConfiguracion", null);
            string nombreMetodoConfiguraciones = ObtenerConfiguracionString(configuraciones, "nombreMetodoConfiguracion", null);
            string cadenaConexion = ObtenerConfiguracionString(configuraciones, "cadenaConexion", null);

            if (!string.IsNullOrWhiteSpace(cadenaConexion) &&
                (!string.IsNullOrEmpty(nombreClaseConfiguraciones) || !string.IsNullOrEmpty(nombreMetodoConfiguraciones)))
            {
                throw new ConfigurationErrorsException(
                    $"Use ya sea la configuración de parámetros \"{nameof(nombreClaseConfiguraciones)}\" y \"{nameof(nombreMetodoConfiguraciones)}\" o use el parámetro \"{nameof(cadenaConexion)}\" pero no ambos.");
            }

            if (!string.IsNullOrEmpty(nombreClaseConfiguraciones) && !string.IsNullOrEmpty(nombreMetodoConfiguraciones))
            {
                Type claseConfiguraciones = (Type.GetType(nombreClaseConfiguraciones, false, true) ??
                    ObtenerClaseDesdeEnsamblados(nombreClaseConfiguraciones)) ??
                    throw new TypeLoadException(
                        $"La clase especificada '{nombreClaseConfiguraciones}' no pudo ser cargada. Por favor asegúrese que el valor especificada es una clase calificada de ensamblado.");

                MethodInfo metodoConfiguraciones = claseConfiguraciones.GetMethod(nombreMetodoConfiguraciones, new Type[] { }) ??
                    throw new MissingMemberException(
                        $"El método especificado '{nombreMetodoConfiguraciones}' en la clase '{nombreClaseConfiguraciones}' no pudo ser encontrado o no satisface la firma de método requerida. Por favor asegúrese que exista, que sea público y que no reciba parámetros.");

                if ((metodoConfiguraciones.Attributes & MethodAttributes.Static) == 0)
                {
                    throw new MissingMethodException(
                        $"El método especificado '{nombreMetodoConfiguraciones}' en la clase '{nombreClaseConfiguraciones}' no coincide con la firma de método requerida. El método debe ser definido como \"static\".");
                }

                if (!typeof(string).IsAssignableFrom(metodoConfiguraciones.ReturnType))
                {
                    throw new MissingMethodException(
                        $"El método especificado '{nombreMetodoConfiguraciones}' en la clase '{nombreClaseConfiguraciones}' no coincide con la firma de método requerida. El método debe tener un tipo de retorno \"String\".");
                }

                cadenaConexion = (string)metodoConfiguraciones.Invoke(null, new object[] { });
            }

            return cadenaConexion;
        }

        /// <summary>
        /// Lee un valor de tipo <see cref="string"/> desde las configuraciones de la aplicación.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        /// <param name="nombreAtributo">El nombre del atributo que se busca entre los valores de configuración.</param>
        /// <param name="valorPredeterminado">El valor predeterminado de la configuración si no fue proporcionado.</param>
        /// <returns>El valor de configuración con el nombre indicado o el valor predeterminado.</returns>
        private static string ObtenerConfiguracionString(
            NameValueCollection configuraciones, string nombreAtributo, string valorPredeterminado)
        {
            string valor = ObtenerDesdeConfiguracion(configuraciones, nombreAtributo);
            if (string.IsNullOrEmpty(valor))
            {
                return valorPredeterminado;
            }

            string valorConfiguracionesAplicacion = ObtenerDesdeConfiguracionesAplicacion(valor);
            if (!string.IsNullOrEmpty(valorConfiguracionesAplicacion))
            {
                return valorConfiguracionesAplicacion;
            }

            return valor;
        }

        /// <summary>
        /// Lee un valor de tipo <see cref="int"/> desde las configuraciones de la aplicación.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        /// <param name="nombreAtributo">El nombre del atributo que se busca entre los valores de configuración.</param>
        /// <param name="valorPredeterminado">El valor predeterminado de la configuración si no fue proporcionado.</param>
        /// <returns>El valor de configuración con el nombre indicado o el valor predeterminado.</returns>
        private static int ObtenerConfiguracionInt(NameValueCollection configuraciones, string nombreAtributo, int valorPredeterminado)
        {
            string valor = ObtenerDesdeConfiguracion(configuraciones, nombreAtributo);
            if (valor == null)
            {
                return valorPredeterminado;
            }

            if (int.TryParse(valor, out int resultado))
            {
                return resultado;
            }

            string valorConfiguracionesAplicacion = ObtenerDesdeConfiguracionesAplicacion(valor);
            if (valorConfiguracionesAplicacion == null)
            {
                return int.Parse(valor);
            }

            return int.Parse(valorConfiguracionesAplicacion);
        }

        /// <summary>
        /// Lee un valor de tipo <see cref="bool"/> desde las configuraciones de la aplicación.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        /// <param name="nombreAtributo">El nombre del atributo que se busca entre los valores de configuración.</param>
        /// <param name="valorPredeterminado">El valor predeterminado de la configuración si no fue proporcionado.</param>
        /// <returns>El valor de configuración con el nombre indicado o el valor predeterminado.</returns>
        private static bool ObtenerConfiguracionBool(NameValueCollection configuraciones, string nombreAtributo, bool valorPredeterminado)
        {
            string valor = ObtenerDesdeConfiguracion(configuraciones, nombreAtributo);
            if (valor == null)
            {
                return valorPredeterminado;
            }

            if (bool.TryParse(valor, out bool resultado))
            {
                return resultado;
            }

            string valorConfiguracionesAplicacion = ObtenerDesdeConfiguracionesAplicacion(valor);
            if (valorConfiguracionesAplicacion == null)
            {
                return bool.Parse(valor);
            }

            return bool.Parse(valorConfiguracionesAplicacion);
        }

        /// <summary>
        /// Lee un valor de tipo <see cref="string"/> desde las configuraciones de la aplicación. Si el valor no existe se retorna
        /// <c>null</c>.
        /// </summary>
        /// <param name="configuraciones">Representa una colección de llaves y valores de tipo <see cref="string"/> que pueden ser accedidos
        /// por llave o índice.</param>
        /// <param name="nombreAtributo">El nombre del atributo que se busca entre los valores de configuración.</param>
        /// <returns>El valor de configuración con el nombre indicado o <c>null</c> si no existe.</returns>
        private static string ObtenerDesdeConfiguracion(NameValueCollection configuraciones, string nombreAtributo)
        {
            string[] valoresAtributo = configuraciones.GetValues(nombreAtributo);
            if (valoresAtributo != null && valoresAtributo.Length > 0 && !string.IsNullOrEmpty(valoresAtributo[0]))
            {
                return valoresAtributo[0];
            }

            return null;
        }

        /// <summary>
        /// Lee una configuración desde la sección <c>appSettings</c> del archivo <c>web.config</c>.
        /// </summary>
        /// <param name="nombreAtributo">El nombre del atributo que se busca.</param>
        /// <returns>El valor del atributo solicitado o <c>null</c> si no se encuentra.</returns>
        private static string ObtenerDesdeConfiguracionesAplicacion(string nombreAtributo)
        {
            if (!string.IsNullOrEmpty(nombreAtributo))
            {
                string parametroDesdeConfiguracion = ConfigurationManager.AppSettings[nombreAtributo];
                if (!string.IsNullOrEmpty(parametroDesdeConfiguracion))
                {
                    return parametroDesdeConfiguracion;
                }
            }

            return null;
        }

        /// <summary>
        /// Busca una clase por nombre entre todas las clases cargadas desde los ensamblados cargados al ejecutar la aplicación.
        /// </summary>
        /// <param name="nombreClase">El nombre de la clase que se busca entre los ensamblados.</param>
        /// <returns>Los metadatos de la clase solicitada.</returns>
        private static Type ObtenerClaseDesdeEnsamblados(string nombreClase)
        {
            Type tipoClase;
            foreach (Assembly ensamblado in AppDomain.CurrentDomain.GetAssemblies())
            {
                tipoClase = ensamblado.GetType(nombreClase, false, true);
                if (tipoClase == null)
                {
                    tipoClase = ensamblado.GetType($"{ensamblado.GetName().Name}.{nombreClase}", false, true);
                }

                if (tipoClase != null)
                {
                    return tipoClase;
                }
            }

            return null;
        }
    }
}
