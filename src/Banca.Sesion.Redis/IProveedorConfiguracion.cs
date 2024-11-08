// <copyright file="IProveedorConfiguracion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
#if !NET461
    using System.Web;
#endif

    /// <summary>
    /// Define los valores de configuración necesarios para almacenar el estado de sesión en Redis.
    /// </summary>
    /// <remarks>
    /// Esta interfaz existe sólo para poder leer la configuración desde archivos de configuración de diferentes contextos dependiendo del
    /// cliente de esta biblioteca. Se podrá reemplazar por consultas en la base de datos de clonación si se continúa usando.
    /// </remarks>
    public interface IProveedorConfiguracion
    {
        /// <summary>
        /// Obtiene los parámetros para la conexión con la instancia de Redis. En la cadena de conexión se pueden recibir los parámetros
        /// <see cref="Huesped"/>, <see cref="Puerto"/>, <see cref="ClaveAcceso"/>, <see cref="UsarSsl"/>,
        /// <see cref="IdentificadorBaseDatos"/>, <see cref="MilisegundosTiempoEsperaConexion"/> y
        /// <see cref="MilisegundosTiempoEsperaOperacion"/> por lo que es innecesario especificarlos si se proporciona la cadena de
        /// conexión.
        /// </summary>
        string CadenaConexion { get; }

        /// <summary>
        /// Obtiene el nombre o IP del servidor del clúster donde se encuentra Redis.
        /// </summary>
        string Huesped { get; }

        /// <summary>
        /// Obtiene el puerto en el que se encuentra disponible el servicio de Redis.
        /// </summary>
        int Puerto { get; }

        /// <summary>
        /// Obtiene la contraseña para conectarse al servicio de Redis.
        /// </summary>
        string ClaveAcceso { get; }

        /// <summary>
        /// Obtiene un valor que indica si se debe usar SSL para establecer la conexión con Redis (<c>true</c>) o no.
        /// </summary>
        bool UsarSsl { get; }

        /// <summary>
        /// Obtiene el índice de la base de datos predeterminada de la conexión con Redis.
        /// </summary>
        int IdentificadorBaseDatos { get; }

        /// <summary>
        /// Obtiene la cantidad de milisegundos que se espera que se establezca la conexión con Redis antes de generar un error de tiempo de
        /// espera.
        /// </summary>
        int MilisegundosTiempoEsperaConexion { get; }

        /// <summary>
        /// Obtiene la cantidad de milisegundos que se espera la ejecución de los comandos síncronos en Redis antes de generar un error de
        /// tiempo de espera.
        /// </summary>
        int MilisegundosTiempoEsperaOperacion { get; }

        /// <summary>
        /// Obtiene el tiempo máximo para ejecutar reintentos de las operaciones de conexión y enlace de sesiones de .NET y .NET Framework
        /// antes de lanzar la excepción que provoca los reintentos.
        /// </summary>
        TimeSpan TiempoEsperaReintentos { get; }

        /// <summary>
        /// Obtiene el tiempo que la sesión puede transcurrir sin ser accedida antes que sea descartada.
        /// </summary>
        TimeSpan TiempoEsperaSesion { get; }

        /// <summary>
        /// Obtiene un valor que indica si se debe agregar la propiedad <c>httponly</c> a la cookie de enlace (<c>true</c>) o no.
        /// </summary>
        bool CookieEnlaceSoloHttp { get; }

        /// <summary>
        /// Obtiene el valor para la propiedad <c>path</c> de la cookie de enlace.
        /// </summary>
        string CookieEnlaceRuta { get; }

        /// <summary>
        /// Obtiene un valor que indica si se debe agregar la propiedad <c>secure</c> a la cookie de enlace (<c>true</c>) o no.
        /// </summary>
        bool CookieEnlaceSegura { get; }

#if !NET461
        /// <summary>
        /// Obtiene el modo para la propiedad <c>samesite</c> de la cookie de enlace.
        /// </summary>
        SameSiteMode CookieEnlaceMismoSitio { get; }
#endif
    }
}
