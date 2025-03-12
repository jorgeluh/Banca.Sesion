// <copyright file="ProveedorConfiguracion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Asp
{
    using System;
    using Banca.Sesion.Asp.Properties;
    using Banca.Sesion.Redis;

    /// <summary>
    /// Obtiene los parámetros necesarios para el funcionamiento del proveedor de estado de sesión en Redis. Estos se obtienen desde el
    /// archivo <c>application.config</c>.
    /// </summary>
    internal class ProveedorConfiguracion : IProveedorConfiguracion
    {
        /// <summary>
        /// Obtiene los parámetros para la conexión con la instancia de Redis. En la cadena de conexión se pueden recibir los parámetros
        /// <see cref="Huesped"/>, <see cref="Puerto"/>, <see cref="ClaveAcceso"/>, <see cref="UsarSsl"/>,
        /// <see cref="IdentificadorBaseDatos"/>, <see cref="MilisegundosTiempoEsperaConexion"/> y
        /// <see cref="MilisegundosTiempoEsperaOperacion"/> por lo que es innecesario especificarlos si se proporciona la cadena de
        /// conexión.
        /// </summary>
        public string CadenaConexion => Settings.Default.CadenaConexion;

        /// <summary>
        /// Obtiene el nombre o IP del servidor del clúster donde se encuentra Redis. No implementada.
        /// </summary>
        public string Huesped => throw new NotImplementedException();

        /// <summary>
        /// Obtiene el puerto en el que se encuentra disponible el servicio de Redis. No implemenatada.
        /// </summary>
        public int Puerto => throw new NotImplementedException();

        /// <summary>
        /// Obtiene la contraseña para conectarse al servicio de Redis. No implementada.
        /// </summary>
        public string ClaveAcceso => throw new NotImplementedException();

        /// <summary>
        /// Obtiene un valor que indica si se debe usar SSL para establecer la conexión con Redis (<c>true</c>) o no. No implementada.
        /// </summary>
        public bool UsarSsl => throw new NotImplementedException();

        /// <summary>
        /// Obtiene el índice de la base de datos predeterminada de la conexión con Redis.
        /// </summary>
        public int IdentificadorBaseDatos => 0;

        /// <summary>
        /// Obtiene la cantidad de milisegundos que se espera que se establezca la conexión con Redis antes de generar un error de tiempo de
        /// espera.
        /// </summary>
        public int MilisegundosTiempoEsperaConexion => 0;

        /// <summary>
        /// Obtiene la cantidad de milisegundos que se espera la ejecución de los comandos síncronos en Redis antes de generar un error de
        /// tiempo de espera.
        /// </summary>.
        public int MilisegundosTiempoEsperaOperacion => 0;

        /// <summary>
        /// Obtiene el tiempo máximo para ejecutar reintentos de las operaciones de conexión y enlace de sesiones de .NET y .NET Framework
        /// antes de lanzar la excepción que provoca los reintentos.
        /// </summary>
        public TimeSpan TiempoEsperaReintentos => Settings.Default.TiempoEsperaReintentos;

        /// <summary>
        /// Obtiene el tiempo que la sesión puede transcurrir sin ser accedida antes que sea descartada.
        /// </summary>
        public TimeSpan TiempoEsperaSesion => Settings.Default.TiempoEsperaSesion;

        /// <summary>
        /// Obtiene el nombre de la cookie de sesión configurado para las aplicaciones de .NET.
        /// </summary>
        public string NombreCookieSesionNet => Settings.Default.NombreCookieSesionNet;

        /// <summary>
        /// Obtiene el tiempo de espera máximo que se permite ejecutar esta petición.
        /// </summary>
        public TimeSpan TiempoEsperaPeticion => Settings.Default.TiempoEsperaPeticion;
    }
}
