// <copyright file="IContenido.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Asp
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interfaz para la propiedad <c>Contents</c> del objeto <c>Session</c> original de ASP.
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IContenido
    {
        /// <summary>
        /// Elimina una variable de sesión individual por su nombre.
        /// </summary>
        /// <param name="nombreVariable">El nombre de la variable de sesión por eliminar.</param>
        void Remove(string nombreVariable);
    }
}