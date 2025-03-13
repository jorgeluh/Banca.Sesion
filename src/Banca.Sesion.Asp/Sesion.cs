// <copyright file="Sesion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Asp
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
#if !NET461
    using System.Threading.Tasks;
#endif
    using System.Web.SessionState;
    using Banca.Sesion.Redis;

    /// <summary>
    /// Implementa las funciones, métodos y propiedades necesarios para reemplazar al objeto <c>Session</c> de ASP.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Sesion.Sincronizada")]
    [Guid("AC2BF14A-AFCC-4142-BE05-186724FB22D1")]
    public class Sesion : ISesion
    {
        /// <summary>
        /// El nombre de la variable de sesión en la que se almacena el identificador de sesión (<see cref="SessionID"/>) una vez generado.
        /// </summary>
        private const string NombreIdentificadorSesionAsp = "!dentificador$esionA$P";

        /// <summary>
        /// El proveedor de configuraciones con los parámetros para establecer la conexión con Redis y crear la cookie de enlace de sesión.
        /// </summary>
        private static readonly ProveedorConfiguracion Configuraciones = new ProveedorConfiguracion();

        /// <summary>
        /// La conexión hacia Redis que expone operaciones básicas para persistir los elementos de estado de sesión entre peticiones.
        /// </summary>
        private IConexionAlmacen almacen;

        /// <summary>
        /// La colección de elementos de estado de sesión cargada desde Redis.
        /// </summary>
        private ISessionStateItemCollection datosSesion;

        /// <summary>
        /// El identificador del bloqueo de sesión si se solicitó uno para actualizar el estado de la sesión.
        /// </summary>
        private object identificadorBloqueo;

        /// <summary>
        /// El tiempo de espera sin peticiones antes de eliminar la sesión medido en minutos.
        /// </summary>
        private int minutosTiempoEsperaSesion;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="Sesion"/>.
        /// </summary>
        public Sesion()
        {
            this.Contents = new Contenido(this);
            this.almacen = null;
            this.datosSesion = null;
            this.identificadorBloqueo = string.Empty;
            this.minutosTiempoEsperaSesion = (int)Configuraciones.TiempoEsperaSesion.TotalMinutes;
        }

        /// <summary>
        /// Obtiene el identificador de sesión de ASP. Este es un número entero. Se representa como <see cref="string"/> si se elimina la
        /// dependencia de ASP y se puede usar un identificador distinto.
        /// </summary>
        public string SessionID { get; private set; }

        /// <summary>
        /// Obtiene una propiedad que representa a <c>Contents</c> del objeto <c>Session</c> original para poder acceder a su método
        /// <c>Remove</c> y así eliminar variables.
        /// </summary>
        public Contenido Contents { get; private set; }

        /// <summary>
        /// Obtiene o establece el identificador de localidad (LCID) de la sesión actual. La asignación no tiene una implementación real, se
        /// agregó sólo por compatibilidad.
        /// </summary>
        public int LCID
        {
            get => Thread.CurrentThread.CurrentCulture.LCID;
            set
            {
                // Este valor no se puede modificar, se dejó la propiedad como de escritura por compatibilidad.
            }
        }

        /// <summary>
        /// Obtiene o establece el tiempo de espera que se puede mantener la sesión antes de ser descartada. No tiene una implementación
        /// real pues no es algo que se pueda configurar en tiempo de ejecución en .NET.
        /// </summary>
        public int Timeout
        {
            get => this.minutosTiempoEsperaSesion;
            set
            {
                // Este valor no se puede modificar, se dejó la propiedad como de escritura por compatibilidad.
            }
        }

        /// <summary>
        /// Obtiene o establece un valor de sesión por nombre.
        /// </summary>
        /// <param name="nombre">El nombre llave del valor de sesión.</param>
        /// <returns>El valor de estado de sesión con el nombre especificado.</returns>
        public object this[string nombre]
        {
            get
            {
                this.ObtenerDatoSesion(false);
                return this.datosSesion?[nombre];
            }

            set
            {
                this.ObtenerDatoSesion(true);
                if (this.datosSesion != null)
                {
                    this.datosSesion[nombre] = value;
#if !NET461
                    this.almacen.IntentarActualizarYLiberarBloqueoAsync(
                        this.identificadorBloqueo, this.datosSesion, (int)Configuraciones.TiempoEsperaSesion.TotalSeconds).Wait();
#else
                    this.almacen.IntentarActualizarYLiberarBloqueo(
                        this.identificadorBloqueo, this.datosSesion, (int)Configuraciones.TiempoEsperaSesion.TotalSeconds);
#endif
                }
            }
        }

        /// <summary>
        /// Inicializa el objeto para poder obtener el identificador de sesión de .NET Framework y enlazarlo con el identificador de sesión
        /// de .NET si aún no se ha hecho desde una aplicación de ASP.NET Framework.
        /// </summary>
        /// <param name="identificadorSesion">El identificador de sesión de .NET Framework.</param>
        public void Inicializar(string identificadorSesion)
        {
            this.almacen = new EnvoltorioConexionRedis(Configuraciones, identificadorSesion);
            this.GenerarIdentificadorSesionAsp();
        }

        /// <summary>
        /// Abandona la sesión actual. Su único efecto es eliminar todas las variables de sesión pues no existe una funcionalidad
        /// equivalente en .NET Core.
        /// </summary>
        public void Abandon()
        {
#if !NET461
            this.almacen.IntentarEliminarYLiberarBloqueoAsync(this.identificadorBloqueo).Wait();
#else
            this.almacen.IntentarEliminarYLiberarBloqueo(this.identificadorBloqueo);
#endif
        }

        /// <summary>
        /// Elimina el elemento indicado de la colección de elementos de estado de sesión y del almacén de estado de sesión.
        /// </summary>
        /// <param name="nombre">El nombre del elemento de estado de sesión a eliminar de la colección.</param>
        internal void EliminarVariable(string nombre)
        {
            this.ObtenerDatoSesion(true);
            if (this.datosSesion != null)
            {
                this.datosSesion.Remove(nombre);
#if !NET461
                this.almacen.IntentarActualizarYLiberarBloqueoAsync(
                    this.identificadorBloqueo, this.datosSesion, (int)Configuraciones.TiempoEsperaSesion.TotalSeconds).Wait();
#else
                this.almacen.IntentarActualizarYLiberarBloqueo(
                    this.identificadorBloqueo, this.datosSesion, (int)Configuraciones.TiempoEsperaSesion.TotalSeconds);
#endif
            }
        }

        /// <summary>
        /// Establece el identificador de sesión de la cookie de sesión de .NET Core. Con este valor es posible identificar la sesión del
        /// cliente en la API de sincronización.
        /// </summary>
        /// <remarks>
        /// El contar con este valor permite acceder a las variables de sesión de .NET Core y por lo tanto ya permite recuperar el
        /// identificador de sesión (<see cref="SessionID"/>) creado o generar uno nuevo si aún no existe.
        /// </remarks>
        private void GenerarIdentificadorSesionAsp()
        {
            this.ObtenerDatoSesion(false);
            this.SessionID = (string)this.datosSesion[NombreIdentificadorSesionAsp];
            if (string.IsNullOrEmpty(this.SessionID))
            {
                this.SessionID = new Random(unchecked((int)DateTime.Now.Ticks)).Next(100000000, 999999999).ToString();
                this[NombreIdentificadorSesionAsp] = this.SessionID;
            }
        }

        /// <summary>
        /// Carga la colección de elementos de estado de sesión desde el almacén.
        /// </summary>
        /// <param name="bloquearSesion">Un valor que indica si se solicita un bloqueo sobre el estado de sesión en el almacén porque será
        /// actualizado (<c>true</c>).</param>
        private void ObtenerDatoSesion(bool bloquearSesion)
        {
            if (this.datosSesion != null)
            {
                return;
            }

            DatosSesion datosSesion;
            if (bloquearSesion)
            {
#if !NET461
                datosSesion = Task.Run(() => this.almacen.IntentarTomarBloqueoEscrituraYObtenerDatosAsync(
                    DateTime.Now, (int)Configuraciones.TiempoEsperaPeticion.TotalSeconds)).Result;
#else
                datosSesion = this.almacen.IntentarTomarBloqueoEscrituraYObtenerDatos(
                    DateTime.Now, (int)Configuraciones.TiempoEsperaPeticion.TotalSeconds);
#endif
            }
            else
            {
#if !NET461
                datosSesion = Task.Run(() => this.almacen.IntentarVerificarBloqueoEscrituraYObtenerDatosAsync()).Result;
#else
                datosSesion = this.almacen.IntentarVerificarBloqueoEscrituraYObtenerDatos();
#endif
            }

            this.datosSesion = datosSesion.ElementosEstadoSesion;
            this.identificadorBloqueo = datosSesion.IdentificadorBloqueo;
            this.minutosTiempoEsperaSesion = datosSesion.SegundosEsperaSesion / 60;
        }
    }
}
