// <copyright file="ISesion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Asp
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Define las funciones, métodos y propiedades necesarios para reemplazar al objeto <c>Session</c> de ASP.
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("D07D8ACD-4E65-40B5-B28B-04698F24CE66")]
    public interface ISesion
    {
        /// <summary>
        /// Obtiene el nombre de la cookie de enlace de sesión para poder comprobar si existe en la petición desde ASP.
        /// </summary>
        string NombreCookieEnlace { get; }

        /// <summary>
        /// Obtiene el identificador de sesión de ASP. Este es un número entero. Se representa como <see cref="string"/> si se elimina la
        /// dependencia de ASP y se puede usar un identificador distinto.
        /// </summary>
        string SessionID { get; }

        /// <summary>
        /// Obtiene una propiedad que representa a <c>Contents</c> del objeto <c>Session</c> original para poder acceder a su método
        /// <c>Remove</c> y así eliminar variables.
        /// </summary>
        Contenido Contents { get; }

        /// <summary>
        /// Obtiene o establece el identificador de localidad (LCID) de la sesión actual. No tiene una implementación real, se agregó sólo
        /// por compatibilidad.
        /// </summary>
        int LCID { get; set; }

        /// <summary>
        /// Obtiene o establece el tiempo de espera que se puede mantener la sesión antes de ser descartada. No tiene una implementación
        /// real pues no es algo que se pueda configurar en tiempo de ejecución en .NET.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Obtiene o establece un valor de sesión por nombre.
        /// </summary>
        /// <param name="nombre">El nombre llave del valor de sesión.</param>
        /// <returns>El valor de estado de sesión con el nombre especificado.</returns>
        object this[string nombre] { get; set; }

        /// <summary>
        /// Inicializa el objeto para poder obtener el identificador de sesión de .NET Framework y enlazarlo con el identificador de sesión
        /// de .NET si aún no se ha hecho desde una aplicación de ASP.NET Framework.
        /// </summary>
        /// <param name="identificadorSesionNetFramework">El identificador de sesión de .NET Framework.</param>
        /// <param name="identificadorSesionNet">El identificador de sesión de .NET.</param>
        /// <param name="existeCookieEnlace">Un valor que indica si se encontró la cookie de enlace (cuyo nombre lo da la propiedad
        /// <see cref="NombreCookieEnlace"/>) en la petición o no.</param>
        /// <returns>El valor para el encabezado <c>Set-Cookie</c> si se debe crear la cookie de enlace.</returns>
        string Inicializar(string identificadorSesionNetFramework, string identificadorSesionNet, bool existeCookieEnlace);

        /// <summary>
        /// Abandona la sesión actual. Su único efecto es eliminar todas las variables de sesión pues no existe una funcionalidad
        /// equivalente en .NET Core.
        /// </summary>
        void Abandon();
    }
}
