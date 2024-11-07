// <copyright file="ExtensionesBinaryReader.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System.IO;

    /// <summary>
    /// Métodos de extensión para la clase <see cref="BinaryReader"/> que facilitan la tarea de deserializar la colección de elementos de
    /// estado de sesión con el formato de .NET.
    /// </summary>
    internal static class ExtensionesBinaryReader
    {
        /// <summary>
        /// Deserializa un valor de tipo <see cref="int"/> serializado en dos bytes.
        /// </summary>
        /// <param name="lector">El lector que procesa los datos serializados obtenidos desde Redis.</param>
        /// <returns>El valor entero deserializado desde dos bytes.</returns>
        public static int DeserializarDesde2Bytes(this BinaryReader lector)
        {
            return lector.ReadByte() << 8 | lector.ReadByte();
        }

        /// <summary>
        /// Deserializa un valor de tipo <see cref="int"/> serializado en tres bytes.
        /// </summary>
        /// <param name="lector">El lector que procesa los datos serializados obtenidos desde Redis.</param>
        /// <returns>El valor entero deserializado desde tres bytes.</returns>
        public static int DeserializarDesde3Bytes(this BinaryReader lector)
        {
            return lector.ReadByte() << 16 | lector.ReadByte() << 8 | lector.ReadByte();
        }

        /// <summary>
        /// Deserializa un valor de tipo <see cref="int"/> serializado en cuatro bytes.
        /// </summary>
        /// <param name="lector">El lector que procesa los datos serializados obtenidos desde Redis.</param>
        /// <returns>El valor entero deserializado desde cuatro bytes.</returns>
        public static int DeserializarDesde4Bytes(this BinaryReader lector)
        {
            return lector.ReadByte() << 24 | lector.ReadByte() << 16 | lector.ReadByte() << 8 | lector.ReadByte();
        }

        /// <summary>
        /// Lee una cantidad determinada de bytes y los retorna como un vector.
        /// </summary>
        /// <param name="lector">El lector que procesa los datos serializados obtenidos desde Redis.</param>
        /// <param name="cantidad">La cantidad de bytes a leer.</param>
        /// <returns>El vector de bytes de la longitud solicitada.</returns>
        /// <exception cref="EndOfStreamException">Si se llegó al final del lector.</exception>
        public static byte[] LeerBytes(this BinaryReader lector, int cantidad)
        {
            byte[] resultado = new byte[cantidad];
            int total = 0;
            int leidos;
            while (total < cantidad)
            {
                leidos = lector.Read(resultado, total, cantidad - total);
                if (leidos == 0)
                {
                    throw new EndOfStreamException();
                }

                total += leidos;
            }

            return resultado;
        }
    }
}
