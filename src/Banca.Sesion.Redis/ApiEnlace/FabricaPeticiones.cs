// <copyright file="FabricaPeticiones.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis.ApiEnlace
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;

    /// <summary>
    /// Fábrica de peticiones que las configura para llamar a la API de sesión de .NET.
    /// </summary>
    internal static class FabricaPeticiones
    {
        /// <summary>
        /// Crea una petición HTTP que contiene toda la información necesaria, incluyendo la cookie de sesión, para poder ser enviada por
        /// medio del método <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> (a través de la clase <see cref="ClienteHttp"/>).
        /// </summary>
        /// <param name="metodo">El método HTTP de la petición.</param>
        /// <param name="uri">El URI de la operación (más allá de la dirección base).</param>
        /// <param name="contenido">El contenido de la petición que será serializado como JSON.</param>
        /// <returns>El <see cref="HttpRequestMessage"/> con la información necesaria para ejecutar la operación necesaria de la API de
        /// sincronización.</returns>
        /// <exception cref="InvalidOperationException">Si no se encuentra la cookie de sesión de .NET Core en la petición.</exception>
        public static HttpRequestMessage CrearPeticion(HttpMethod metodo, string uri, object contenido = null)
        {
            HttpRequestMessage peticion = new HttpRequestMessage(metodo, uri);
            if (contenido != null)
            {
                peticion.Content = new StringContent(JsonSerializer.Serialize(contenido), Encoding.UTF8, "application/json");
            }

            return peticion;
        }
    }
}
