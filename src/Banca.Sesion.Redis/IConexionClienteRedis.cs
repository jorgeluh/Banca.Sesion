// <copyright file="IConexionClienteRedis.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
#if !NET461
    using System.Threading.Tasks;
#endif
    using System.Web.SessionState;
    using StackExchange.Redis;

    /// <summary>
    /// Define las funcionalidades básicas en Redis para poder usarlo como un almacén de estado de sesión.
    /// </summary>
    internal interface IConexionClienteRedis
    {
        /// <summary>
        /// Ejecuta un script de comandos en Redis con las llaves y argumentos indicados.
        /// </summary>
        /// <remarks>
        /// Los "comandos" en Redis son equivalentes a procedimientos almacenados en una base de datos relacional y están definidos como
        /// scripts de <see href="https://www.lua.org/">Lua</see>. Dentro de los scripts se puede emplear la función <c>redis.call</c> para
        /// ejecutar las <see href="https://redis.io/commands/">instrucciones</see> propias de Redis.
        /// </remarks>
        /// <param name="comando">El comando de Redis.</param>
        /// <param name="llaves">Las llaves a emplear dentro del script del comando de Redis. Se acceden por medio del vector <c>KEYS</c>.
        /// </param>
        /// <param name="valores">Los argumentos a emplear dentro del script del comando de Redis. Se acceden por medio del vector
        /// <c>ARGV</c>.</param>
#if !NET461
        /// <returns>Una tarea cuyo resultado es un objeto de tipo <see cref="RedisResult"/> que luego debe ser casteado según el tipo de
        /// resultado que se espera del script.</returns>
        Task<RedisResult> EvaluarAsync(string comando, string[] llaves, object[] valores);
#else
        /// <returns>Un objeto de tipo <see cref="RedisResult"/> que luego debe ser casteado según el tipo de resultado que se espera del
        /// script.</returns>
        RedisResult Evaluar(string comando, string[] llaves, object[] valores);
#endif

        /// <summary>
        /// Lee el identificador del bloqueo a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>El identificador del bloqueo de sesión.</returns>
        string ObtenerIdentificadorBloqueo(RedisResult datosDesdeRedis);

        /// <summary>
        /// Lee el tiempo de espera de la sesión a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>El tiempo de espera de la sesión en segundos.</returns>
        int ObtenerSegundosEsperaSesion(RedisResult datosDesdeRedis);

        /// <summary>
        /// Obtiene la bandera que indica si la sesión se encuentra bloqueada a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>Un valor que indica si la sesión se encuentra bloqueada (<c>true</c>) o no.</returns>
        bool EstaBloqueada(object datosDesdeRedis);

        /// <summary>
        /// Recrea la colección de elementos de estado de sesión a partir del resultado del comando de Redis.
        /// </summary>
        /// <param name="datosDesdeRedis">El resultado de evaluar el comando en Redis.</param>
        /// <returns>La colección de elementos de estado de sesión que se encontraba almacenada en Redis.</returns>
        ISessionStateItemCollection ObtenerDatosSesion(object datosDesdeRedis);
    }
}
