// <copyright file="ConexionCompartidaRedis.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.Reflection;
    using System.Security.Authentication;
    using StackExchange.Redis;

    /// <summary>
    /// Provee una conexión hacia la instancia de Redis que fue configurada.
    /// </summary>
    internal class ConexionCompartidaRedis
    {
        /// <summary>
        /// Objeto usado para bloquear intentos de conexión concurrentes.
        /// </summary>
        private static readonly object BloqueoReconexion = new object();

        /// <summary>
        /// La hora de la última reconexión con Redis.
        /// </summary>
        private static DateTimeOffset horaUltimaReconexion = DateTimeOffset.MinValue;

        /// <summary>
        /// La hora del primer error de conexión con Redis.
        /// </summary>
        private static DateTimeOffset horaPrimerError = DateTimeOffset.MinValue;

        /// <summary>
        /// La hora del último error de conexión con Redis.
        /// </summary>
        private static DateTimeOffset horaErrorPrevio = DateTimeOffset.MinValue;

        /// <summary>
        /// El período usado para establecer conexiones con Redis.
        /// </summary>
        private static TimeSpan frecuenciaReconexion = TimeSpan.FromSeconds(60);

        /// <summary>
        /// El período que debe transcurrir para intentar una reconexión si ocurrió un error.
        /// </summary>
        private static TimeSpan umbralErrorReconexion = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Proveedor de configuración con los valores necesarios para establecer la conexión.
        /// </summary>
        private readonly IProveedorConfiguracion configuracion;

        /// <summary>
        /// Opciones de configuración específicas de Redis.
        /// </summary>
        private readonly ConfigurationOptions opcionesConfiguracion;

        /// <summary>
        /// El multiplexor empleado para obtener conexiones hacia Redis. Sólo se instancia hasta cuando debe ser utilizado.
        /// </summary>
        private Lazy<ConnectionMultiplexer> multiplexorRedis;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ConexionCompartidaRedis"/>.
        /// </summary>
        /// <param name="configuracion">El proveedor de configuración del proceso desde el que se obtienen los parámetros de conexión hacia
        /// Redis.</param>
        public ConexionCompartidaRedis(IProveedorConfiguracion configuracion)
        {
            this.configuracion = configuracion;
            this.opcionesConfiguracion = new ConfigurationOptions();

            if (!string.IsNullOrEmpty(this.configuracion.CadenaConexion))
            {
                this.opcionesConfiguracion = ConfigurationOptions.Parse(this.configuracion.CadenaConexion);
                this.opcionesConfiguracion.AbortOnConnectFail = false;
            }
            else
            {
                if (this.configuracion.Puerto == 0)
                {
                    this.opcionesConfiguracion.EndPoints.Add(this.configuracion.Huesped);
                }
                else
                {
                    this.opcionesConfiguracion.EndPoints.Add($"{this.configuracion.Huesped}:{this.configuracion.Puerto}");
                }

                this.opcionesConfiguracion.Password = this.configuracion.ClaveAcceso;
                this.opcionesConfiguracion.Ssl = this.configuracion.UsarSsl;
                this.opcionesConfiguracion.SslProtocols = SslProtocols.None;
                this.opcionesConfiguracion.AbortOnConnectFail = false;

                if (this.configuracion.MilisegundosTiempoEsperaConexion != 0)
                {
                    this.opcionesConfiguracion.ConnectTimeout = this.configuracion.MilisegundosTiempoEsperaConexion;
                }

                if (this.configuracion.MilisegundosTiempoEsperaOperacion != 0)
                {
                    this.opcionesConfiguracion.SyncTimeout = this.configuracion.MilisegundosTiempoEsperaOperacion;
                }
            }

            if (string.IsNullOrWhiteSpace(this.opcionesConfiguracion.ClientName))
            {
                AssemblyName proveedor = Assembly.GetExecutingAssembly().GetName();
                this.opcionesConfiguracion.ClientName =
                    $"{this.opcionesConfiguracion.Defaults.ClientName}({proveedor.Name}-v{proveedor.Version})";
            }

            this.CrearMultiplexor();
        }

        /// <summary>
        /// Obtiene el objeto por medio del cual se ejecutan las operaciones en Redis.
        /// </summary>
        public IDatabase Conexion => this.multiplexorRedis.Value.GetDatabase(
            this.opcionesConfiguracion.DefaultDatabase ?? this.configuracion.IdentificadorBaseDatos);

        /// <summary>
        /// Intenta establecer una conexión con la base de datos de Redis.
        /// </summary>
        public void ForzarReconexion()
        {
            DateTimeOffset horaReconexionPrevia = horaUltimaReconexion;
            TimeSpan periodoDesdeUltimaReconexion = DateTimeOffset.UtcNow - horaReconexionPrevia;

            if (periodoDesdeUltimaReconexion > frecuenciaReconexion)
            {
                lock (BloqueoReconexion)
                {
                    DateTimeOffset horaUtcActual = DateTimeOffset.UtcNow;
                    periodoDesdeUltimaReconexion = horaUtcActual - horaUltimaReconexion;

                    if (periodoDesdeUltimaReconexion < frecuenciaReconexion)
                    {
                        return;
                    }

                    if (horaPrimerError == DateTimeOffset.MinValue)
                    {
                        horaPrimerError = horaUtcActual;
                        horaErrorPrevio = horaUtcActual;
                        return;
                    }

                    TimeSpan periodoDesdePrimerError = horaUtcActual - horaPrimerError;
                    TimeSpan periodoDesdeErrorMasReciente = horaUtcActual - horaErrorPrevio;
                    horaErrorPrevio = horaUtcActual;

                    if (periodoDesdePrimerError >= umbralErrorReconexion && periodoDesdeErrorMasReciente <= umbralErrorReconexion)
                    {
                        horaPrimerError = DateTimeOffset.MinValue;
                        horaErrorPrevio = DateTimeOffset.MinValue;

                        Lazy<ConnectionMultiplexer> multiplexorAnterior = this.multiplexorRedis;
                        this.CerrarMultiplexor(multiplexorAnterior);
                        this.CrearMultiplexor();
                    }
                }
            }
        }

        /// <summary>
        /// Crea el multiplexor por medio del cual se obtienen las conexiones hasta el momento cuando es necesario.
        /// </summary>
        private void CrearMultiplexor()
        {
            this.multiplexorRedis = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(this.opcionesConfiguracion));
            horaUltimaReconexion = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Cierra el multiplexor anterior si ocurrió un error.
        /// </summary>
        /// <param name="multiplexorAnterior">La instancia del multiplexor que se está cerrando.</param>
        private void CerrarMultiplexor(Lazy<ConnectionMultiplexer> multiplexorAnterior)
        {
            if (multiplexorAnterior != null)
            {
                try
                {
                    multiplexorAnterior.Value.Close();
                }
                catch
                {
                    // El multiplexor se está desechando porque ya ocurrió un error.
                }
            }
        }
    }
}