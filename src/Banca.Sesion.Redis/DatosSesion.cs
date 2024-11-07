// <copyright file="DatosSesion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System.Web.SessionState;

    /// <summary>
    /// Reúne los valores de la sesión y su bloqueo cuando se consultan desde el almacén de sesiones.
    /// </summary>
    public class DatosSesion
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="DatosSesion"/>.
        /// </summary>
        /// <param name="seTomoBloqueo">Un valor que indica si se estableció un bloqueo al ejecutar la operación en el almacén de
        /// sesiones.</param>
        /// <param name="estaBloqueada">Un valor que indica si la sesión se encontraba bloqueada en el almacén de sesiones.</param>
        /// <param name="identificadorBloqueo">El identificador del bloqueo en el almacén de sesiones.</param>
        /// <param name="segundosEsperaSesion">El tiempo de espera para expirar la sesión en el almacén medido en segundos.</param>
        /// <param name="elementosEstadoSesion">La colección de elementos de estado de sesión en el almacén.</param>
        public DatosSesion(
            bool seTomoBloqueo,
            bool estaBloqueada,
            object identificadorBloqueo,
            int segundosEsperaSesion,
            ISessionStateItemCollection elementosEstadoSesion)
        {
            this.SeTomoBloqueo = seTomoBloqueo;
            this.EstaBloqueada = estaBloqueada;
            this.IdentificadorBloqueo = identificadorBloqueo;
            this.SegundosEsperaSesion = segundosEsperaSesion;
            this.ElementosEstadoSesion = elementosEstadoSesion;
        }

        /// <summary>
        /// Obtiene un valor que indica si se estableció un bloqueo al ejecutar la operación en el almacén de sesiones.
        /// </summary>
        public bool SeTomoBloqueo { get; private set; }

        /// <summary>
        /// Obtiene un valor que indica si la sesión se encontraba bloqueada en el almacén de sesiones.
        /// </summary>
        public bool EstaBloqueada { get; private set; }

        /// <summary>
        /// Obtiene el identificador del bloqueo en el almacén de sesiones.
        /// </summary>
        public object IdentificadorBloqueo { get; private set; }

        /// <summary>
        /// Obtiene el tiempo de espera para expirar la sesión en el almacén medido en segundos.
        /// </summary>
        public int SegundosEsperaSesion { get; private set; }

        /// <summary>
        /// Obtiene la colección de elementos de estado de sesión en el almacén.
        /// </summary>
        public ISessionStateItemCollection ElementosEstadoSesion { get; private set; }
    }
}
