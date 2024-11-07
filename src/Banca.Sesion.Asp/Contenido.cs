// <copyright file="Contenido.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Asp
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Simula la propiedad <c>Contents</c> del objeto <c>Session</c> original para poder eliminar variables de sesión.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Contenido : IContenido
    {
        /// <summary>
        /// La sesión de la cual se estará eliminando la variable y por medio de la cual se eliminará la variable en .NET Core.
        /// </summary>
        private readonly Sesion sesion;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="Contenido"/>.
        /// </summary>
        /// <param name="sesion">La sesión de la cual se estará eliminando la variable y por medio de la cual se eliminará la variable en .NET Core.</param>
        internal Contenido(Sesion sesion)
        {
            this.sesion = sesion;
        }

        /// <summary>
        /// Elimina una variable de sesión individual por su nombre.
        /// </summary>
        /// <param name="nombreVariable">El nombre de la variable de sesión por eliminar.</param>
        public void Remove(string nombreVariable)
        {
            this.sesion.EliminarVariable(nombreVariable);
        }
    }
}
