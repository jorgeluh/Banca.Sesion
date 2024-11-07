// <copyright file="ClienteHttp.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis.ApiEnlace
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// Cliente HTTP usado para consumir la API de sesión de .NET.
    /// </summary>
    internal sealed class ClienteHttp : HttpClient
    {
        // TODO: ¿conviene que esto sea configurable?

        /// <summary>
        /// Define la URL base de la API de sincronización de variables de sesión.
        /// </summary>
        private const string UrlBaseApiSincronizacion = "http://variablessesion/api/enlaceSesion/";

        /// <summary>
        /// La instancia única del cliente HTTP para todo el proceso.
        /// </summary>
        private static readonly Lazy<ClienteHttp> InstanciaUnica = new Lazy<ClienteHttp>(() => new ClienteHttp());

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ClienteHttp"/>. Es privado por tratarse de una implementación del patrón
        /// de diseño <i>singleton</i>.
        /// </summary>
        private ClienteHttp()
            : base(new HttpClientHandler() { UseCookies = false }, false)
        {
            this.BaseAddress = new Uri(UrlBaseApiSincronizacion);
        }

        /// <summary>
        /// Obtiene una instancia de la clase. Siempre será la misma para todo el proceso.
        /// </summary>
        public static ClienteHttp Instancia => InstanciaUnica.Value;
    }
}