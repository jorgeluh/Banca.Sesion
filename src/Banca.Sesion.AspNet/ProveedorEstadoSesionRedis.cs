// <copyright file="ProveedorEstadoSesionRedis.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.AspNet
{
#if !NET461
    using System;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.SessionState;
    using Banca.Sesion.Redis;
    using Microsoft.AspNet.SessionState;
#else
    using System;
    using System.Collections.Specialized;
    using System.Web;
    using System.Web.SessionState;
    using Banca.Sesion.Redis;
#endif

#if !NET461
    /// <summary>
    /// Implementación de <see cref="SessionStateStoreProviderAsyncBase"/> que usa Redis como almacén de datos de estado de sesión y es
    /// compatible con la sesión de .NET. Esto significa que permite leer y escribir variables de sesión que se comparten con otras
    /// aplicaciones web de .NET Framework y .NET.
    /// </summary>
    public class ProveedorEstadoSesionRedis : SessionStateStoreProviderAsyncBase
#else
    /// <summary>
    /// Implementación de <see cref="SessionStateStoreProviderAsync"/> que usa Redis como almacén de datos de estado de sesión y es
    /// compatible con la sesión de .NET. Esto significa que permite leer y escribir variables de sesión que se comparten con otras
    /// aplicaciones web de .NET Framework y .NET.
    /// </summary>
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
        /// Libera los recursos.
        /// </summary>
        public override void Dispose()
        {
        }

#if !NET461
        /// <summary>
        /// Llamado al inicio del evento <c>AcquireRequestState</c>.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición.</param>
        public override void InitializeRequest(HttpContextBase context)
        {
        }

        /// <summary>
        /// Llamada asíncrona para el evento <c>EndRequestAsync</c>.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición actual.</param>
        /// <returns>Una tarea que permite esperar la finalización de la ejecución del evento.</returns>
        public override async Task EndRequestAsync(HttpContextBase context)
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
                    await this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincideAsync(
                        this.identificadorBloqueoSesion, segundosTiempoEsperaSesion);
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
        /// Crea un nuevo objeto <see cref="SessionStateStoreData"/> para ser usado para la petición actual.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición actual.</param>
        /// <param name="timeout">El valor del tiempo de espera del estado de sesión para el nuevo <see cref="SessionStateStoreData"/>.
        /// </param>
        /// <returns>El nuevo almacén de datos de estado de sesión para la petición.</returns>
        public override SessionStateStoreData CreateNewStoreData(HttpContextBase context, int timeout)
        {
            return new SessionStateStoreData(new ColeccionElementosEstadoSesion(), new HttpStaticObjectsCollection(), timeout);
        }

        /// <summary>
        /// Crea un elemento de sesión no inicializado.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de sesión.</param>
        /// <param name="timeout">El valor del tiempo de espera de la sesión.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>El elemento de sesión no inicializado.</returns>
        public override async Task CreateUninitializedItemAsync(
            HttpContextBase context, string id, int timeout, CancellationToken cancellationToken)
        {
            try
            {
                ISessionStateItemCollection datosSesion = new ColeccionElementosEstadoSesion();
                datosSesion[VariableSesionAcciones] = SessionStateActions.InitializeItem;
                this.ObtenerAccesoAlmacen(id);
                await this.almacen.FijarAsync(datosSesion, timeout * 60);
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
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que recupera el elemento de sesión sin bloqueo.</returns>
        public override async Task<GetItemResult> GetItemAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            return await this.ObtenerElementoDesdeAlmacenSesionAsync(false, context, id, cancellationToken);
        }

        /// <summary>
        /// Recupera un elemento de sesión con bloqueo.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que recupera el elemento de sesión con bloqueo.</returns>
        public override async Task<GetItemResult> GetItemExclusiveAsync(
            HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            return await this.ObtenerElementoDesdeAlmacenSesionAsync(true, context, id, cancellationToken);
        }

        /// <summary>
        /// Reinicia el tiempo de expiración para un elemento basado en su tiempo de espera.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task ResetItemTimeoutAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
                await this.almacen.ActualizarTiempoExpiracionAsync((int)configuracion.TiempoEsperaSesion.TotalSeconds);
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
        /// Elimina el elemento de sesión del almacén.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="item">El elemento de sesión a eliminar.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task RemoveItemAsync(
            HttpContextBase context, string id, object lockId, SessionStateStoreData item, CancellationToken cancellationToken)
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
                await this.almacen.IntentarEliminarYLiberarBloqueoAsync(lockId);
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
        /// Libera un elemento bloqueado por <c>GetExclusive</c>
        /// (<see cref="GetItemExclusiveAsync(HttpContextBase, string, CancellationToken)"/>).
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task ReleaseItemExclusiveAsync(
            HttpContextBase context, string id, object lockId, CancellationToken cancellationToken)
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
                    await this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincideAsync(lockId, segundosTiempoEsperaSesion);
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
        /// Actualiza la información del elemento de sesión en el almacén de datos del estado de sesión con valores de la petición actual, y
        /// libera el bloqueo de los datos.
        /// </summary>
        /// <param name="context">El <see cref="HttpContextBase"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="item">Los datos de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="newItem">Si se trata de un nuevo elemento de sesión.</param>
        /// <param name="cancellationToken">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea que permite esperar la finalización del proceso.</returns>
        public override async Task SetAndReleaseItemExclusiveAsync(
            HttpContextBase context,
            string id,
            SessionStateStoreData item,
            object lockId,
            bool newItem,
            CancellationToken cancellationToken)
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

                    await this.almacen.FijarAsync(elementosSesion, item.Timeout * 60);
                }
                else
                {
                    if (item != null && item.Items != null)
                    {
                        if (item.Items[VariableSesionAcciones] != null)
                        {
                            item.Items.Remove(VariableSesionAcciones);
                        }

                        await this.almacen.IntentarActualizarYLiberarBloqueoAsync(lockId, item.Items, item.Timeout * 60);
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
        /// Recupera los datos de un elemento de estado de sesión desde el almacén.
        /// </summary>
        /// <param name="seRequiereBloqueoEscritura">Indica si se requiere crear un bloqueo de escritura para la sesión.</param>
        /// <param name="contexto">El <see cref="HttpContext"/> de la petición actual.</param>
        /// <param name="identificadorSesion">El identificador de la sesión.</param>
        /// <param name="tokenCancelacion">El token de cancelación para la tarea asíncrona.</param>
        /// <returns>Una tarea cuyo resultado es el estado de sesión recuperado desde Redis.</returns>
        private async Task<GetItemResult> ObtenerElementoDesdeAlmacenSesionAsync(
            bool seRequiereBloqueoEscritura, HttpContextBase contexto, string identificadorSesion, CancellationToken tokenCancelacion)
        {
            bool estaBloqueada = false;
            TimeSpan tiempoBloqueo = TimeSpan.Zero;
            object identificadorBloqueo = 0;
            SessionStateActions acciones = SessionStateActions.None;
            try
            {
                if (identificadorSesion == null)
                {
                    return new GetItemResult(null, estaBloqueada, tiempoBloqueo, identificadorBloqueo, acciones);
                }

                this.ObtenerAccesoAlmacen(identificadorSesion);
                ISessionStateItemCollection elementosEstadoSesion = null;

                int segundosEsperaSesion;
                bool seTomoBloqueo = false;
                DatosSesion datosSesion;
                if (seRequiereBloqueoEscritura)
                {
                    datosSesion = await this.almacen.IntentarTomarBloqueoEscrituraYObtenerDatosAsync(
                        DateTime.Now, (int)configuracion.TiempoEsperaPeticion.TotalSeconds);
                    this.identificadorSesion = identificadorSesion;
                    this.identificadorBloqueoSesion = datosSesion.IdentificadorBloqueo;
                }
                else
                {
                    datosSesion = await this.almacen.IntentarVerificarBloqueoEscrituraYObtenerDatosAsync();
                }

                segundosEsperaSesion = datosSesion.SegundosEsperaSesion;
                seTomoBloqueo = datosSesion.SeTomoBloqueo;
                elementosEstadoSesion = datosSesion.ElementosEstadoSesion;
                if (seTomoBloqueo)
                {
                    estaBloqueada = false;
                }
                else
                {
                    this.identificadorSesion = null;
                    this.identificadorBloqueoSesion = null;
                    estaBloqueada = true;
                }

                if (estaBloqueada)
                {
                    tiempoBloqueo = this.almacen.ObtenerTiempoBloqueo(datosSesion.IdentificadorBloqueo);
                    return new GetItemResult(null, estaBloqueada, tiempoBloqueo, identificadorBloqueo, acciones);
                }

                if (elementosEstadoSesion == null)
                {
                    await this.ReleaseItemExclusiveAsync(contexto, identificadorSesion, identificadorBloqueo, tokenCancelacion);
                    return new GetItemResult(null, estaBloqueada, tiempoBloqueo, identificadorBloqueo, acciones);
                }

                if (elementosEstadoSesion[VariableSesionAcciones] != null)
                {
                    acciones = (SessionStateActions)elementosEstadoSesion[VariableSesionAcciones];
                }

                elementosEstadoSesion.Dirty = false;
                SessionStateStoreData datosAlmacenEstadoSesion =
                    new SessionStateStoreData(elementosEstadoSesion, new HttpStaticObjectsCollection(), segundosEsperaSesion);
                return new GetItemResult(datosAlmacenEstadoSesion, estaBloqueada, tiempoBloqueo, identificadorBloqueo, acciones);
            }
            catch
            {
                estaBloqueada = false;
                identificadorBloqueo = null;
                tiempoBloqueo = TimeSpan.Zero;
                acciones = SessionStateActions.None;

                if (configuracion.ArrojarConError)
                {
                    throw;
                }

                return new GetItemResult(null, estaBloqueada, tiempoBloqueo, identificadorBloqueo, acciones);
            }
        }
#else
        /// <summary>
        /// Llamado al inicio del evento <c>AcquireRequestState</c>.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        public override void InitializeRequest(HttpContext context)
        {
        }

        /// <summary>
        /// Llamada asíncrona para el evento <c>EndRequest</c>.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición actual.</param>
        public override void EndRequest(HttpContext context)
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
                    this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincide(
                        this.identificadorBloqueoSesion, segundosTiempoEsperaSesion);
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
        /// Crea un nuevo objeto <see cref="SessionStateStoreData"/> para ser usado para la petición actual.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición actual.</param>
        /// <param name="timeout">El valor del tiempo de espera del estado de sesión para el nuevo <see cref="SessionStateStoreData"/>.
        /// </param>
        /// <returns>El nuevo almacén de datos de estado de sesión para la petición.</returns>
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new ColeccionElementosEstadoSesion(), new HttpStaticObjectsCollection(), timeout);
        }

        /// <summary>
        /// Crea un elemento de sesión no inicializado.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de sesión.</param>
        /// <param name="timeout">El valor del tiempo de espera de la sesión.</param>
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            try
            {
                ISessionStateItemCollection datosSesion = new ColeccionElementosEstadoSesion();
                datosSesion[VariableSesionAcciones] = SessionStateActions.InitializeItem;
                this.ObtenerAccesoAlmacen(id);
                this.almacen.Fijar(datosSesion, timeout * 60);
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
        public override SessionStateStoreData GetItem(
            HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return this.ObtenerElementoDesdeAlmacenSesion(false, context, id, out locked, out lockAge, out lockId, out actions);
        }

        /// <summary>
        /// Recupera un elemento de sesión con bloqueo.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        public override SessionStateStoreData GetItemExclusive(
            HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return this.ObtenerElementoDesdeAlmacenSesion(true, context, id, out locked, out lockAge, out lockId, out actions);
        }
        
        /// <summary>
        /// Reinicia el tiempo de expiración para un elemento basado en su tiempo de espera.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        public override void ResetItemTimeout(HttpContext context, string id)
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
                this.almacen.ActualizarTiempoExpiracion((int)configuracion.TiempoEsperaSesion.TotalSeconds);
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
        /// Elimina el elemento de sesión del almacén.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="item">El elemento de sesión a eliminar.</param>
        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            try
            {
                this.ObtenerAccesoAlmacen(id);
                this.almacen.IntentarEliminarYLiberarBloqueo(lockId);
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
        /// Libera un elemento bloqueado por <c>GetExclusive</c>
        /// (<see cref="GetItemExclusiveAsync(HttpContext, string, CancellationToken)"/>).
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
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
                    this.almacen.IntentarLiberarBloqueoSiIdentificadorBloqueoCoincide(lockId, segundosTiempoEsperaSesion);
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
        /// Actualiza la información del elemento de sesión en el almacén de datos del estado de sesión con valores de la petición actual, y
        /// libera el bloqueo de los datos.
        /// </summary>
        /// <param name="context">El <see cref="HttpContext"/> de la petición.</param>
        /// <param name="id">El identificador de la sesión.</param>
        /// <param name="item">Los datos de la sesión.</param>
        /// <param name="lockId">El identificador del bloqueo de sesión.</param>
        /// <param name="newItem">Si se trata de un nuevo elemento de sesión.</param>
        public override void SetAndReleaseItemExclusive(
            HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
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

                    this.almacen.Fijar(elementosSesion, item.Timeout * 60);
                }
                else
                {
                    if (item != null && item.Items != null)
                    {
                        if (item.Items[VariableSesionAcciones] != null)
                        {
                            item.Items.Remove(VariableSesionAcciones);
                        }

                        this.almacen.IntentarActualizarYLiberarBloqueo(lockId, item.Items, item.Timeout * 60);
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
        /// Recupera los datos de un elemento de estado de sesión desde el almacén.
        /// </summary>
        /// <param name="seRequiereBloqueoEscritura">Indica si se requiere crear un bloqueo de escritura para la sesión.</param>
        /// <param name="contexto">El <see cref="HttpContext"/> de la petición actual.</param>
        /// <param name="identificadorSesion">El identificador de la sesión.</param>
        /// <param name="estaBloqueada"><c>true</c> indica que sí se pudo establecer el bloqueo de la sesión.</param>
        /// <param name="tiempoBloqueo">El tiempo establecido para el bloqueo de la sesión.</param>
        /// <param name="identificadorBloqueo">El identificador del bloqueo establecido para la sesión.</param>
        /// <param name="acciones">Indica si la sesión fue inicializada o si no se tomó ninguna acción.</param>
        /// <returns>El almacén de datos de estado de sesión.</returns>
        private SessionStateStoreData ObtenerElementoDesdeAlmacenSesion(
            bool seRequiereBloqueoEscritura,
            HttpContextBase contexto,
            string identificadorSesion,
            out bool estaBloqueada,
            out TimeSpan tiempoBloqueo,
            out object identificadorBloqueo,
            out SessionStateActions acciones)
        {
            try
            {
                estaBloqueada = false;
                tiempoBloqueo = TimeSpan.Zero;
                identificadorBloqueo = 0;
                acciones = SessionStateActions.None;

                if (identificadorSesion == null)
                {
                    return null;
                }

                this.ObtenerAccesoAlmacen(identificadorSesion);
                ISessionStateItemCollection elementosEstadoSesion = null;

                int segundosEsperaSesion;
                bool seTomoBloqueo = false;
                DatosSesion datosSesion;
                if (seRequiereBloqueoEscritura)
                {
                    datosSesion = this.almacen.IntentarTomarBloqueoEscrituraYObtenerDatos(
                        DateTime.Now, (int)configuracion.TiempoEsperaPeticion.TotalSeconds);
                    this.identificadorSesion = identificadorSesion;
                    this.identificadorBloqueoSesion = datosSesion.IdentificadorBloqueo;
                }
                else
                {
                    datosSesion = this.almacen.IntentarVerificarBloqueoEscrituraYObtenerDatos();
                }

                segundosEsperaSesion = datosSesion.SegundosEsperaSesion;
                seTomoBloqueo = datosSesion.SeTomoBloqueo;
                elementosEstadoSesion = datosSesion.ElementosEstadoSesion;
                if (seTomoBloqueo)
                {
                    estaBloqueada = false;
                }
                else
                {
                    this.identificadorSesion = null;
                    this.identificadorBloqueoSesion = null;
                    estaBloqueada = true;
                }

                if (estaBloqueada)
                {
                    tiempoBloqueo = this.almacen.ObtenerTiempoBloqueo(datosSesion.IdentificadorBloqueo);
                    return null;
                }

                if (elementosEstadoSesion == null)
                {
                    this.ReleaseItemExclusive(contexto, identificadorSesion, identificadorBloqueo);
                    return null;
                }

                if (elementosEstadoSesion[VariableSesionAcciones] != null)
                {
                    acciones = (SessionStateActions)elementosEstadoSesion[VariableSesionAcciones];
                }

                elementosEstadoSesion.Dirty = false;
                return new SessionStateStoreData(elementosEstadoSesion, new HttpStaticObjectsCollection(), segundosEsperaSesion);
            }
            catch
            {
                estaBloqueada = false;
                identificadorBloqueo = null;
                tiempoBloqueo = TimeSpan.Zero;
                acciones = SessionStateActions.None;

                if (configuracion.ArrojarConError)
                {
                    throw;
                }

                return null;
            }
        }
#endif

        /// <summary>
        /// Obtiene el almacén de datos de sesión en Redis para ejecutar las operaciones necesarias.
        /// </summary>
        /// <param name="identificadorSesion">El identificador de la sesión de .NET Framework.</param>
        private void ObtenerAccesoAlmacen(string identificadorSesion)
        {
            if (this.almacen == null)
            {
                this.almacen = new EnvoltorioConexionRedis(configuracion, identificadorSesion);
            }
            else
            {
                this.almacen.Llaves.RegenerarCadenaLlaveSiIdentificadorModificado(identificadorSesion);
            }
        }
    }
}
