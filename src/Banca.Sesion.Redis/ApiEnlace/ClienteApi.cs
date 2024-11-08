// <copyright file="ClienteApi.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis.ApiEnlace
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsula las operaciones disponibles en la API de sesión de .NET.
    /// </summary>
    internal static class ClienteApi
    {
        /// <summary>
        /// Crea una llave en Redis que permite mapear el identificador de sesión de .NET Framework con la llave de sesión de .NET, creando
        /// así un enlace entre las dos sesiones que se puede usar directamente dentro de Redis. Es necesario llamar a esta operación antes
        /// de ejecutar cualquier comando de Redis pues dependen de la existencia de esta llave de enlace para funcionar correctamente.
        /// </summary>
        /// <remarks>
        /// Se optó por enviar el identificador de sesión de .NET Framework para que se grabe desde la API web en lugar de consultar
        /// la llave de sesión de .NET porque esta última se mantiene más segura. De cualquier manera el identificador de sesión de .NET
        /// Framework va expuesto en la cookie sin modificación alguna.
        /// </remarks>
        /// <param name="identificadorSesion">El identificador de sesión de .NET Framework.</param>
        /// <param name="identificadorCookieNet">El identificador presente en la cookie de sesión de .NET. Cabe mencionar que este es muy
        /// distinto a la llave de sesión que emplea .NET en Redis.</param>
#if !NET461
        /// <returns>Una tarea cuyo resultado es el tiempo restante de la llave de sesión de .NET en Redis medido en segundos. <c>0</c> si
        /// se obtuvo una respuesta de error de la API o un valor negativo si la llave de sesión de .NET ya había expirado o no se encontró
        /// en Redis.</returns>
        public static async Task<int> EnlazarSesionAsync(string identificadorSesion, string identificadorCookieNet)
#else
        /// <returns>El tiempo restante de la llave de sesión de .NET en Redis medido en segundos. <c>0</c> si se obtuvo una respuesta de
        /// error de la API o un valor negativo si la llave de sesión de .NET ya había expirado o no se encontró en Redis.</returns>
        public static int EnlazarSesion(string identificadorSesion, string identificadorCookieNet)
#endif
        {
            HttpRequestMessage peticion = FabricaPeticiones.CrearPeticion(HttpMethod.Put, identificadorSesion, identificadorCookieNet);
#if !NET461
            HttpResponseMessage respuesta = await ClienteHttp.Instancia.SendAsync(peticion);
#else
            HttpResponseMessage respuesta = Task.Run(() => ClienteHttp.Instancia.SendAsync(peticion)).Result;
#endif
            if (respuesta.StatusCode == HttpStatusCode.OK)
            {
#if !NET461
                return int.Parse(await respuesta.Content.ReadAsStringAsync());
#else
                return int.Parse(Task.Run(() => respuesta.Content.ReadAsStringAsync()).Result);
#endif
            }
            else
            {
                return 0;
            }
        }
    }
}
