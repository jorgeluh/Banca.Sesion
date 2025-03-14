// <copyright file="GeneradorLlaves.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    /// <summary>
    /// Genera los nombres de las llaves y mantiene los nombres de los campos que se usarán en los comandos de Redis y que son compatibles
    /// con las llaves de .NET.
    /// </summary>
    /// <remarks>
    /// Esta clase también se encarga de ejecutar el enlace del identificador de sesión de .NET Framework con la llave de sesión de Redis
    /// para que los comandos funcionen correctamente.
    /// </remarks>
    internal class GeneradorLlaves
    {
        /// <summary>
        /// El nombre del campo que emplea .NET para guardar los datos serializados de los elementos de estado de sesión.
        /// </summary>
        public const string CampoDatos = "data";

        /// <summary>
        /// El nombre del campo que emplea .NET para guadar el tiempo que se extiende la llave de sesión cada vez que es accedida.
        /// </summary>
        public const string CampoExpiracionSesion = "sldexp";

        /// <summary>
        /// El identificador de sesión con el que se realizó el enlace con la sesión de .NET. Notar que para la llave en Redis se emplea la
        /// propiedad <see cref="LlaveSesion"/> y que es distinta.
        /// </summary>
        private string identificadorSesion;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="GeneradorLlaves"/>.
        /// </summary>
        /// <param name="identificadorSesion">El identificador de la sesión de .NET Framework.</param>
        public GeneradorLlaves(string identificadorSesion)
        {
            this.GenerarLlaves(identificadorSesion);
        }

        /// <summary>
        /// Obtiene la llave de la sesión de .NET Framework con la que se pueden encontrar los datos del estado de sesión en Redis.
        /// </summary>
        public string LlaveSesion { get; private set; }

        /// <summary>
        /// Obtiene la llave que emplea .NET Framework en Redis para el registro de bloqueo de estado de sesión.
        /// </summary>
        public string LlaveBloqueo { get; private set; }

        /// <summary>
        /// Regenera las llaves <see cref="LlaveSesion"/> y <see cref="LlaveBloqueo"/> si el identificador de sesión de .NET Framework
        /// cambió.
        /// </summary>
        /// <remarks>
        /// Esto ejecuta también un nuevo enlace con la llave de Redis de .NET para el nuevo identificador de sesión.
        /// </remarks>
        /// <param name="identificadorSesion">El nuevo identificador de sesión de .NET Framework.</param>
        public void RegenerarCadenaLlaveSiIdentificadorModificado(string identificadorSesion)
        {
            if (!identificadorSesion.Equals(this.identificadorSesion))
            {
                this.GenerarLlaves(identificadorSesion);
            }
        }

        /// <summary>
        /// Genera los valores de las llaves en Redis basado en el identificador de sesión de .NET Framework.
        /// </summary>
        /// <param name="identificadorSesion">El identificador de sesión de .NET Framework.</param>
        private void GenerarLlaves(string identificadorSesion)
        {
            this.identificadorSesion = identificadorSesion;
            this.LlaveSesion = identificadorSesion;
            this.LlaveBloqueo = $"{identificadorSesion}_BloqueoEscritura";
        }
    }
}
