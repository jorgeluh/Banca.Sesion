// <copyright file="EnlazadorSesion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
#if !NET461
    using System.Threading.Tasks;
#endif
    using System.Web;
    using Banca.Sesion.Redis.ApiEnlace;

    /// <summary>
    /// Clase auxiliar que llama a la API de sesión de .NET cuando se considera necesario para crear la llave en Redis que enlaza al
    /// identificador de sesión de .NET Framework con la llave de Redis que emplea .NET.
    /// </summary>
    /// <remarks>
    /// Es necesario que esta operación se realice antes de ejecutar cualquier comando en Redis pues están diseñados para depender de ese
    /// mapeo. La alternativa hubiese sido consultar la llave de Redis que emplea .NET y usarla de este lado para crear el mapeo. Aparte de
    /// ser prácticamente lo mismo es menos seguro permitir que esa llave esté viajando en peticiones HTTP porque la llave de .NET Framework
    /// de cualquier manera está expuesta al usuario.
    /// </remarks>
    public class EnlazadorSesion
    {
        /// <summary>
        /// El nombre que tendrá la cookie empleada como bandera para indicar que la sesión ya ha sido enlazada y por lo tanto que esta
        /// operación es innecesaria.
        /// </summary>
        public const string NombreCookieEnlace = "Enlace";

        /// <summary>
        /// El valor que tendrá la cookie de enlace. Este no importa pero sí debe tener una longitud mayor a <c>0</c> para que se pueda
        /// validar de manera confiable en algunos contextos.
        /// </summary>
        private const string ValorCookieEnlace = "true";

        /// <summary>
        /// El identificador de sesión obtenido de la cookie de sesión de .NET.
        /// </summary>
        private readonly string identificadorSesionNet;

        /// <summary>
        /// Proveedor de configuración con los valores de las propiedades de la cookie de enlace y tiempo de espera para reintentos.
        /// </summary>
        private readonly IProveedorConfiguracion configuracion;

        /// <summary>
        /// Bandera que indica si es necesario enlazar la sesión o no.
        /// </summary>
        private bool enlazarSesion;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="EnlazadorSesion"/>.
        /// </summary>
        /// <param name="identificadorSesionNet">El identificador de sesión obtenido de la cookie de sesión de .NET. Este no debe tener
        /// ninguna codificación para evitar problemas al enviarlo a la API de enlace de sesión.</param>
        /// <param name="configuracion">Proveedor de configuración con los valores de las propiedades de la cookie de enlace y tiempo de
        /// espera para reintentos.</param>
        /// <param name="existeCookieEnlace">Indica si ya existe la cookie de enlace en la petición que se recibió (<c>true</c>) o no.
        /// </param>
        internal EnlazadorSesion(string identificadorSesionNet, IProveedorConfiguracion configuracion, bool existeCookieEnlace)
        {
            if (string.IsNullOrEmpty(identificadorSesionNet))
            {
                throw new ArgumentException("No se encontró la cookie de sesión de .NET.", nameof(identificadorSesionNet));
            }

            this.identificadorSesionNet = identificadorSesionNet;
            this.configuracion = configuracion;
            this.enlazarSesion = !existeCookieEnlace;
        }

        /// <summary>
        /// Ejecuta la operación de enlace del identificador de sesión de .NET Framework y la llave de Redis de .NET.
        /// </summary>
        /// <remarks>
        /// <para>
        /// La operación sólo se ejecuta cuando es necesario. Es decir, cuando no se encuentra la cookie bandera de sesión enlazada en la
        /// petición y si la operación es exitosa se hace a lo sumo una vez durante la petición, sin importar cuántas veces se ejecute este
        /// método.
        /// </para>
        /// <para>
        /// Si la operación es exitosa, se recibe como resultado de la API la cantidad de segundos de vida de la llave en Redis. Este valor
        /// se usa para definir la expiración de la cookie de enlace. Esta vigencia no se va extendiendo pero no es problema que la cookie
        /// expire, en ese caso sólo se volverá a enlazar la sesión pero habrá cumplido su propósito de no llamar a la API innecesariamente.
        /// </para>
        /// </remarks>
        /// <param name="identificadorSesion">El identificador de sesión de .NET Framework.</param>
        /// <param name="forzarEnlace">Sirve para anular la validación que omite las operaciones de enlace innecesarias. Es útil por ejemplo
        /// si el identificador de sesión ha sido modificado.</param>
#if !NET461
        /// <returns>Una tarea cuyo resultado es la cookie de enlace que se emplea como bandera para no volver a realizar el enlace con cada
        /// petición.</returns>
        internal async Task<HttpCookie> EnlazarAsync(string identificadorSesion, bool forzarEnlace = false)
#else
        /// <returns>La cookie de enlace que se emplea como bandera para no volver a realizar el enlace con cada petición.</returns>
        internal HttpCookie Enlazar(string identificadorSesion, bool forzarEnlace = false)
#endif
        {
            if (!this.enlazarSesion && !forzarEnlace)
            {
                return null;
            }

#if !NET461
            int segundosExpiracionSesion = await LogicaReintentos.EjecutarFuncionAsync(
                () => ClienteApi.EnlazarSesionAsync(identificadorSesion, this.identificadorSesionNet),
                this.configuracion.TiempoEsperaReintentos);
#else
            int segundosExpiracionSesion = LogicaReintentos.EjecutarFuncion(
                () => ClienteApi.EnlazarSesion(identificadorSesion, this.identificadorSesionNet),
                this.configuracion.TiempoEsperaReintentos);
#endif
            if (segundosExpiracionSesion > 0)
            {
                this.enlazarSesion = false;
                return new HttpCookie(NombreCookieEnlace, ValorCookieEnlace)
                    {
                        Expires = DateTime.Now.AddSeconds(segundosExpiracionSesion),
                        HttpOnly = this.configuracion.CookieEnlaceSoloHttp,
                        Path = this.configuracion.CookieEnlaceRuta,
                        Secure = this.configuracion.CookieEnlaceSegura,
#if !NET461
                    SameSite = this.configuracion.CookieEnlaceMismoSitio,
#endif
                    };
            }

            return null;
        }
    }
}
