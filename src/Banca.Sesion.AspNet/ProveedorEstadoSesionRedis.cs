// <copyright file="ProveedorEstadoSesionRedis.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.AspNet
{
    using System;
    using System.Collections.Specialized;
#if !NET461
    using System.Threading;
    using System.Threading.Tasks;
#endif
    using System.Web;
    using System.Web.SessionState;
    using Banca.Sesion.Redis;
#if !NET461
    using Microsoft.AspNet.SessionState;
#endif

/// <summary>
/// Implementación de <see cref="SessionStateStoreProviderAsyncBase"/> que usa Redis como almacén de datos de estado de sesión y es
/// compatible con la sesión de .NET. Esto significa que permite leer y escribir variables de sesión que se comparten con otras
/// aplicaciones web de .NET Framework y .NET.
/// </summary>
#if !NET461
    public class ProveedorEstadoSesionRedis : SessionStateStoreProviderAsyncBase
#else
    public class ProveedorEstadoSesionRedis : SessionStateStoreProviderBase
#endif
    {
        /// <summary>
        /// Nombre de la variable de estado de sesión donde se almacena la acción aplicada.
        /// </summary>
        private const string VariableSesionAcciones = "AccionesEstadoSesion";

        /// <summary>
        /// Objeto usado para bloquear la inicialización de la configuración del proveedor de sesión.
        /// </summary>
        private static readonly object BloqueoCreacionConfiguracion = new object();

        /// <summary>
        /// Proveedor de configuraciones para el proveedor de sesión.
        /// </summary>
        private static ProveedorConfiguracion configuracion;

        /// <summary>
        /// La conexión hacia Redis que expone operaciones básicas para persistir los elementos de estado de sesión entre peticiones.
        /// </summary>
        private IConexionAlmacen almacen;

        /// <summary>
        /// El identificador de sesión de .NET Framework.
        /// </summary>
        private string identificadorSesion;

        /// <summary>
        /// El identificador del bloqueo de sesión.
        /// </summary>
        private object identificadorBloqueoSesion;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ProveedorEstadoSesionRedis"/>.
        /// </summary>
        public ProveedorEstadoSesionRedis()
        {
        }

        /// <summary>
        /// Inicializa el constructor de configuración.
        /// </summary>
        /// <param name="name">El nombre del proveedor.</param>
        /// <param name="config">Una colección de pares nombre/valor representando los atributos específicos del proveedor especificados en
        /// la configuración para este proveedor.</param>
        /// <exception cref="ArgumentNullException">Si la colección de configuraciones es <c>null</c>.</exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(name))
            {
                name = "MiAlmacen";
            }

            if (string.IsNullOrEmpty(config["descripcion"]))
            {
                config.Remove("descripcion");
                config.Add("descripcion", "Redis como un almacén de datos de sesión");
            }

            base.Initialize(name, config);

            if (configuracion == null)
            {
                lock (BloqueoCreacionConfiguracion)
                {
                    if (configuracion == null)
                    {
                        configuracion = ProveedorConfiguracion.ProveedorConfiguracionParaEstadoSesion(config);
                    }
                }
            }
        }

        /// <summary>
        /// Crea un nuevo objeto <see cref="SessionStateStoreData"/> para ser usado para la petición actual.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición actual.</param>
        /// <param name="timeout">El valor del tiempo de espera del estado de sesión para el nuevo <see cref="SessionStateStoreData"/>.
        /// </param>
        /// <returns>El nuevo almacén de datos de estado de sesión para la petición.</returns>
#if !NET461
        public override SessionStateStoreData CreateNewStoreData(HttpContextBase context, int timeout)
#else
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
#endif
        {
            return new SessionStateStoreData(new ColeccionElementosEstadoSesion(), new HttpStaticObjectsCollection(), timeout);
        }

        /// <summary>
        /// Crea un elemento de sesión no inicializado.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de sesión.</param>
        /// <param name="timeout">El valor del tiempo de espera de la sesión.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>El elemento de sesión no inicializado.</returns>
        public override async Task CreateUninitializedItemAsync(
            HttpContextBase context, string id, int timeout, CancellationToken cancellationToken)
#else
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
#endif
        {
            try
            {
                ISessionStateItemCollection datosSesion = new ColeccionElementosEstadoSesion();
                datosSesion[VariableSesionAcciones] = SessionStateActions.InitializeItem;
                this.ObtenerAccesoAlmacen(id);
#if !NET461
                await this.almacen.FijarAsync(datosSesion, timeout * 60);
#else
                this.almacen.Fijar(datosSesion, timeout * 60);
#endif
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Libera los recursos.
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Llamada asíncrona para el evento <c>EndRequest</c>.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición actual.</param>
#if !NET461
        /// <returns>Una tarea que permite esperar la finalización de la ejecución del evento.</returns>
        public override async Task EndRequestAsync(HttpContextBase context)
#else
        public override void EndRequest(HttpContext context)
#endif
        {
            try
            {
                int segundosTiempoEsperaSesion;
                if (context != null && context.Session != null)
                {
                    segundosTiempoEsperaSesion = context.Session.Timeout * 60;
                }
                else
                {
                    segundosTiempoEsperaSesion = (int)configuracion.TiempoEsperaSesion.TotalSeconds;
                }

                if (this.identificadorSesion != null && this.identificadorBloqueoSesion != null)
                {
                    this.ObtenerAccesoAlmacen(this.identificadorSesion);
#if !NET461
                    await this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincideAsync(
                        this.identificadorBloqueoSesion, segundosTiempoEsperaSesion);
#else
                    this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincide(
                        this.identificadorBloqueoSesion, segundosTiempoEsperaSesion);
#endif
                    this.identificadorSesion = null;
                    this.identificadorBloqueoSesion = null;
                }

                this.almacen = null;
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Recupera un elemento de sesión sin bloqueo.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que recupera el elemento de sesión sin bloqueo.</returns>
        public override async Task<GetItemResult> GetItemAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
#else
        public override SessionStateStoreData GetItem(
            HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
#endif
        {
#if !NET461
            return await this.ObtenerElementoDesdeAlmacenSesionAsync(false, context, id, cancellationToken);
#else
            return this.ObtenerElementoDesdeAlmacenSesion(false, context, id, out locked, out lockAge, out lockId, out actions);
#endif
        }

        /// <summary>
        /// Recupera un elemento de sesión con bloqueo.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que recupera el elemento de sesión con bloqueo.</returns>
        public override Task<GetItemResult> GetItemExclusiveAsync(
            HttpContextBase context, string id, CancellationToken cancellationToken)
#else
        public override SessionStateStoreData GetItemExclusive(
            HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
#endif
        {
#if !NET461
            return this.ObtenerElementoDesdeAlmacenSesionAsync(true, context, id, cancellationToken);
#else
            return this.ObtenerElementoDesdeAlmacenSesion(true, context, id, out locked, out lockAge, out lockId, out actions);
#endif
        }

        /// <summary>
        /// Llamado al inicio del evento <c>AcquireRequestState</c>.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
#if !NET461
        public override void InitializeRequest(HttpContextBase context)
#else
        public override void InitializeRequest(HttpContext context)
#endif
        {
        }

        /// <summary>
        /// Libera un elemento bloqueado por <c>GetExclusive</c>
        /// (<see cref="GetItemExclusiveAsync(HttpContextBase, string, CancellationToken)"/>).
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task ReleaseItemExclusiveAsync(
            HttpContextBase context, string id, object lockId, CancellationToken cancellationToken)
#else
        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
#endif
        {
            try
            {
                int segundosTiempoEsperaSesion;
                if (context != null && context.Session != null)
                {
                    segundosTiempoEsperaSesion = context.Session.Timeout * 60;
                }
                else
                {
                    segundosTiempoEsperaSesion = (int)configuracion.TiempoEsperaSesion.TotalSeconds;
                }

                if (lockId != null)
                {
                    this.ObtenerAccesoAlmacen(id);
#if !NET461
                    await this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincideAsync(lockId, segundosTiempoEsperaSesion);
#else
                    this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincide(lockId, segundosTiempoEsperaSesion);
#endif
                    this.identificadorSesion = null;
                    this.identificadorBloqueoSesion = null;
                }
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Elimina el elemento de sesión del almacén.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="item">El elemento de sesión a eliminar.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task RemoveItemAsync(
            HttpContextBase context, string id, object lockId, SessionStateStoreData item, CancellationToken cancellationToken)
#else
        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
#endif
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
#if !NET461
                await this.almacen.IntentarEliminarYLiberarBloqueoAsync(lockId);
#else
                this.almacen.IntentarEliminarYLiberarBloqueo(lockId);
#endif
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Reinicia el tiempo de expiración para un elemento basado en su tiempo de espera.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task ResetItemTimeoutAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
#else
        public override void ResetItemTimeout(HttpContext context, string id)
#endif
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
#if !NET461
                await this.almacen.ActualizarTiempoExpiracionAsync((int)configuracion.TiempoEsperaSesion.TotalSeconds);
#else
                this.almacen.ActualizarTiempoExpiracion((int)configuracion.TiempoEsperaSesion.TotalSeconds);
#endif
                this.almacen = null;
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Actualiza la información del elemento de sesión en el almacén de datos del estado de sesión con valores de la petición actual, y
        /// libera el bloqueo de los datos.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="item">Los datos de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="newItem">Si se trata de un nuevo elemento de sesión.</param>
#if !NET461
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task SetAndReleaseItemExclusiveAsync(
            HttpContextBase context,
            string id,
            SessionStateStoreData item,
            object lockId,
            bool newItem,
            CancellationToken cancellationToken)
#else
        public override void SetAndReleaseItemExclusive(
            HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
#endif
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
                if (newItem)
                {
                    ISessionStateItemCollection elementosSesion = null;
                    if (item != null && item.Items != null)
                    {
                        elementosSesion = item.Items;
                    }
                    else
                    {
                        elementosSesion = new ColeccionElementosEstadoSesion();
                    }

                    if (elementosSesion[VariableSesionAcciones] != null)
                    {
                        elementosSesion.Remove(VariableSesionAcciones);
                    }

#if !NET461
                    await this.almacen.FijarAsync(elementosSesion, item.Timeout * 60);
#else
                    this.almacen.Fijar(elementosSesion, item.Timeout * 60);
#endif
                }
                else
                {
                    if (item != null && item.Items != null)
                    {
                        if (item.Items[VariableSesionAcciones] != null)
                        {
                            item.Items.Remove(VariableSesionAcciones);
                        }

#if !NET461
                        await this.almacen.IntentarActualizarYLiberarBloqueoAsync(lockId, item.Items, item.Timeout * 60);
#else
                        this.almacen.IntentarActualizarYLiberarBloqueo(lockId, item.Items, item.Timeout * 60);
#endif
                    }
                }
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Fija una referencia al delegado <see cref="SessionStateItemExpireCallback"/> para el evento <c>Session_OnEnd</c>.
        /// </summary>
        /// <param name="expireCallback">El método que maneja al evento <see cref="SessionStateModule.End"/> del módulo de estado de sesión.
        /// </param>
        /// <returns><c>true</c> si el proveedor de estado de almacén de sesión soporta llamar al evento <c>Session_OnEnd</c>; <c>false</c>
        /// en caso contrario.</returns>
        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        /// <summary>
        /// Recupera los datos de un elemento de estado de sesión desde el almacén.
        /// </summary>
        /// <param name="seRequiereBloqueoEscritura">Indica si se requiere crear un bloqueo de escritura para la sesión.</param>
        /// <param name="contexto">El <see cref="HttpContext"/> de la petición actual.</param>
        /// <param name="identificadorSesion">El identificador de la sesión.</param>
#if !NET461
        /// <param name="tokenCancelacion">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea cuyo resultado es el estado de sesión recuperado desde Redis.</returns>
        private async Task<GetItemResult> ObtenerElementoDesdeAlmacenSesionAsync(
            bool seRequiereBloqueoEscritura, HttpContextBase contexto, string identificadorSesion, CancellationToken tokenCancelacion)
#else
        /// <param name="bloqueado"><c>true</c> indica que sí se pudo establecer el bloqueo de la sesión.</param>
        /// <param name="tiempoBloqueo">El tiempo establecido para el bloqueo de la sesión.</param>
        /// <param name="identificadorBloqueo">El identificador del bloqueo establecido para la sesión.</param>
        /// <param name="acciones">Indica si la sesión fue inicializada o si no se tomó ninguna acción.</param>
        /// <returns>El almacén de datos de estado de sesión.</returns>
        private SessionStateStoreData ObtenerElementoDesdeAlmacenSesion(
            bool seRequiereBloqueoEscritura,
            HttpContext contexto,
            string identificadorSesion,
            out bool bloqueado,
            out TimeSpan tiempoBloqueo,
            out object identificadorBloqueo,
            out SessionStateActions acciones)
#endif
        {
#if !NET461
            TimeSpan tiempoBloqueo = TimeSpan.Zero;
            SessionStateActions acciones = SessionStateActions.None;
#else
            bloqueado = false;
            tiempoBloqueo = TimeSpan.Zero;
            identificadorBloqueo = 0;
            acciones = SessionStateActions.None;
#endif

            try
            {
                if (identificadorSesion == null)
                {
#if !NET461
                    return new GetItemResult(null, false, tiempoBloqueo, 0, acciones);
#else
                    return null;
#endif
                }

                this.ObtenerAccesoAlmacen(identificadorSesion);
                DatosSesion datosSesion;
                if (seRequiereBloqueoEscritura)
                {
#if !NET461
                    datosSesion = await this.almacen.IntentarTomarBloqueoEscrituraYObtenerDatosAsync(
                        DateTime.Now, (int)configuracion.TiempoEsperaPeticion.TotalSeconds);
#else
                    datosSesion = this.almacen.IntentarTomarBloqueoEscrituraYObtenerDatos(
                        DateTime.Now, (int)configuracion.TiempoEsperaPeticion.TotalSeconds);
#endif
                    this.identificadorSesion = identificadorSesion;
                    this.identificadorBloqueoSesion = datosSesion.IdentificadorBloqueo;
                }
                else
                {
#if !NET461
                    datosSesion = await this.almacen.IntentarVerificarBloqueoEscrituraYObtenerDatosAsync();
#else
                    datosSesion = this.almacen.IntentarVerificarBloqueoEscrituraYObtenerDatos();
#endif
                }

                if (!datosSesion.SeTomoBloqueo)
                {
                    this.identificadorSesion = null;
                    this.identificadorBloqueoSesion = null;
#if !NET461
                    return new GetItemResult(
                        null,
                        true,
                        this.almacen.ObtenerTiempoBloqueo(datosSesion.IdentificadorBloqueo),
                        datosSesion.IdentificadorBloqueo,
                        acciones);
#else
                    bloqueado = true;
                    tiempoBloqueo = this.almacen.ObtenerTiempoBloqueo(identificadorBloqueo);
                    identificadorBloqueo = datosSesion.IdentificadorBloqueo;
                    return null;
#endif
                }

                if (datosSesion.ElementosEstadoSesion == null)
                {
#if !NET461
                    await this.ReleaseItemExclusiveAsync(contexto, identificadorSesion, datosSesion.IdentificadorBloqueo, tokenCancelacion);
                    return new GetItemResult(null, false, tiempoBloqueo, datosSesion.IdentificadorBloqueo, acciones);
#else
                    this.ReleaseItemExclusive(contexto, identificadorSesion, datosSesion.IdentificadorBloqueo);
                    return null;
#endif
                }

                if (datosSesion.ElementosEstadoSesion[VariableSesionAcciones] != null)
                {
                    acciones = (SessionStateActions)datosSesion.ElementosEstadoSesion[VariableSesionAcciones];
                }

                datosSesion.ElementosEstadoSesion.Dirty = false;
                SessionStateStoreData datosAlmacenEstadoSesion = new SessionStateStoreData(
                    datosSesion.ElementosEstadoSesion, new HttpStaticObjectsCollection(), datosSesion.SegundosEsperaSesion);
#if !NET461
                return new GetItemResult(datosAlmacenEstadoSesion, false, tiempoBloqueo, datosSesion.IdentificadorBloqueo, acciones);
#else
                identificadorBloqueo = datosSesion.IdentificadorBloqueo;
                return datosAlmacenEstadoSesion;
#endif
            }
            catch
            {
                if (configuracion.ArrojarConError)
                {
                    throw;
                }

#if !NET461
                return new GetItemResult(null, false, tiempoBloqueo, null, SessionStateActions.None);
#else
                bloqueado = false;
                identificadorBloqueo = null;
                tiempoBloqueo = TimeSpan.Zero;
                acciones = SessionStateActions.None;
                return null;
#endif
            }
        }

        /// <summary>
        /// Obtiene el almacén de datos de sesión en Redis para ejecutar las operaciones necesarias.
        /// </summary>
        /// <param name="identificadorSesionNetFramework">El identificador de la sesión de .NET Framework.</param>
        private void ObtenerAccesoAlmacen(string identificadorSesionNetFramework)
        {
            HttpCookie cookieEnlace;
            if (this.almacen == null)
            {
                string identificadorSesionNet =
                    HttpUtility.UrlDecode(HttpContext.Current.Request.Cookies[configuracion.NombreCookieSesionNet].Value);
                bool existeCokieEnlace = HttpContext.Current.Request.Cookies[EnlazadorSesion.NombreCookieEnlace] != null;
                this.almacen = new EnvoltorioConexionRedis(
                    configuracion,
                    identificadorSesionNet,
                    identificadorSesionNetFramework,
                    existeCokieEnlace,
                    out cookieEnlace);
            }
            else
            {
#if !NET461
                cookieEnlace =
                    Task.Run(() => this.almacen.RegenerarCadenaLlaveSiIdentificadorModificadoAsync(identificadorSesionNetFramework)).Result;
#else
                cookieEnlace = this.almacen.RegenerarCadenaLlaveSiIdentificadorModificado(identificadorSesionNetFramework);
#endif
            }

            if (cookieEnlace != null)
            {
                HttpContext.Current.Response.Cookies.Add(cookieEnlace);
            }
        }
    }
}
