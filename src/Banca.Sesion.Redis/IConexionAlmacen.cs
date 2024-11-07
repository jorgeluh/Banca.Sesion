// <copyright file="IConexionAlmacen.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.SessionState;

    /// <summary>
    /// Define las operaciones necesarias para la implementación de un proveedor de estado de sesión de .NET Framework basado en Redis.
    /// </summary>
    public interface IConexionAlmacen
    {
        /// <summary>
        /// Actualiza el tiempo de expiración de las llaves en Redis para extenderlo.
        /// </summary>
        /// <param name="segundosParaExpirar">El tiempo de vida de la sesión en segundos.</param>
        /// <returns>Una tarea que permite esperar a que se actualice el tiempo de vida de la sesión.</returns>
        Task ActualizarTiempoExpiracionAsync(int segundosParaExpirar);

        /// <summary>
        /// Actualiza el valor de los elementos de estado de sesión en Redis.
        /// </summary>
        /// <param name="datos">La colección de elementos de estado de sesión cuyos valores se van a actualizar en Redis.</param>
        /// <param name="segundosEsperaSesion">El tiempo de vida de la sesión en segundos.</param>
        /// <returns>Una tarea que permite esperar que se establezcan los datos del estado de la sesión.</returns>
        Task FijarAsync(ISessionStateItemCollection datos, int segundosEsperaSesion);

        /// <summary>
        /// Actualiza los valores de los elementos de estado de sesión en Redis y elimina la llave del bloqueo.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo que se va a eliminar.</param>
        /// <param name="datos">La colección de elementos de estado de sesión que se va a actualizar en Redis.</param>
        /// <param name="segundosEsperaSesion">Los segundos de tiempo de espera de la sesión sin peticiones antes que sea
        /// descartada.</param>
        /// <returns>Una tarea que permite esperar que se actualicen los valores y se elimine el bloqueo.</returns>
        Task IntentarActualizarYLiberarBloqueoAsync(
            object identificadorBloqueo, ISessionStateItemCollection datos, int segundosEsperaSesion);

        /// <summary>
        /// Elimina todas las llaves pertenecientes a la sesión en Redis.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo a eliminar.</param>
        /// <returns>Una tarea que permite esperar que se eliminen las llaves de sesión y sus bloqueos.</returns>
        Task IntentarEliminarYLiberarBloqueoAsync(object identificadorBloqueo);

        /// <summary>
        /// Elimina el bloqueo si su identificador coincide con el proporcionado. Luego extiende la expiración de las llaves de la sesión.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo a eliminar.</param>
        /// <param name="segundosEsperaSesion">La duración en segundos que se extenderá la validez de las llaves de la sesión.</param>
        /// <returns>Una tarea que permite esperar a que se termine la operación.</returns>
        /// <returns>Una tarea que permite esperar que se libere el bloqueo.</returns>
        Task IntentarLiberarBloqueoSiIdentificadorBloqueoCoincideAsync(object identificadorBloqueo, int segundosEsperaSesion);

        /// <summary>
        /// Crea una llave de bloqueo para la sesión y consulta los datos de los elementos de estado de sesión.
        /// </summary>
        /// <param name="horaBloqueo">La hora en la que inicia el bloqueo.</param>
        /// <param name="segundosEsperaBloqueo">La duración del bloqueo en segundos.</param>
        /// <returns>Una tarea cuyo resultado es un objeto que contiene datos de la sesión y su bloqueo si existe.</returns>
        Task<DatosSesion> IntentarTomarBloqueoEscrituraYObtenerDatosAsync(DateTime horaBloqueo, int segundosEsperaBloqueo);

        /// <summary>
        /// Comprueba si el identificador de bloqueo recibido es el mismo en Redis y consulta los valores de los elementos de estado de
        /// sesión.
        /// </summary>
        /// <returns>Una tarea cuyo resultado es un objeto que contiene los elementos de estado de sesión y del bloqueo en Redis.</returns>
        Task<DatosSesion> IntentarVerificarBloqueoEscrituraYObtenerDatosAsync();

        /// <summary>
        /// Obtiene el tiempo transcurrido desde que se estableció el bloqueo con el identificador dado.
        /// </summary>
        /// <param name="identificadorBloqueo">El identificador del bloqueo cuya antigüedad se consulta.</param>
        /// <returns>El tiempo transcurrido desde que se estableció el bloqueo indicado.</returns>
        TimeSpan ObtenerTiempoBloqueo(object identificadorBloqueo);

        /// <summary>
        /// Vuelve a generar los identificadores de las llaves de bloqueo y de sesión si el identificador de sesión ha sido modificado. Esto
        /// obliga a enlazar la sesión de nuevo.
        /// </summary>
        /// <param name="identificadorSesion">El nuevo identificador de sesión.</param>
        /// <returns>La cookie bandera de enlace de sesión.</returns>
        Task<HttpCookie> RegenerarCadenaLlaveSiIdentificadorModificadoAsync(string identificadorSesion);
    }
}
