// <copyright file="ExtensionesBinaryWriter.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.IO;

    /// <summary>
    /// Métodos de extensión para la clase <see cref="BinaryWriter"/> que facilitan la tarea de serializar la colección de elementos de
    /// estado de sesión con el formato de .NET.
    /// </summary>
    internal static class ExtensionesBinaryWriter
    {
        /// <summary>
        /// Serializa un valor de tipo <see cref="int"/> en dos bytes.
        /// </summary>
        /// <param name="escritor">El escritor que procesa los datos para serializarlos.</param>
        /// <param name="numero">El número que será serializado en dos bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Si el valor es negativo o muy grande para ser serializado en dos bytes.
        /// </exception>
        public static void SerializarA2Bytes(this BinaryWriter escritor, int numero)
        {
            if (numero < 0 || numero > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(numero), "El valor no puede ser serializado en dos bytes.");
            }

            escritor.Write((byte)(numero >> 8));
            escritor.Write((byte)(0xFF & numero));
        }

        /// <summary>
        /// Serializa un valor de tipo <see cref="int"/> en tres bytes.
        /// </summary>
        /// <param name="escritor">El escritor que procesa los datos para serializarlos.</param>
        /// <param name="numero">El número que será serializado en tres bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Si el valor es negativo o muy grande para ser serializado en tres bytes.
        /// </exception>
        public static void SerializarA3Bytes(this BinaryWriter escritor, int numero)
        {
            if (numero < 0 || numero > 0xFFFFFF)
            {
                throw new ArgumentOutOfRangeException(nameof(numero), "El valor no puede ser serializado en tres bytes.");
            }

            escritor.Write((byte)(numero >> 16));
            escritor.Write((byte)(0xFF & (numero >> 8)));
            escritor.Write((byte)(0xFF & numero));
        }

        /// <summary>
        /// Serializa un valor de tipo <see cref="int"/> en cuatro bytes.
        /// </summary>
        /// <param name="escritor">El escritor que procesa los datos para serializarlos.</param>
        /// <param name="numero">El número que será serializado en cuatro bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException">Si el valor es negativo y no será serializado en cuatro bytes.
        /// </exception>
        public static void SerializarA4Bytes(this BinaryWriter escritor, int numero)
        {
            if (numero < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numero), "El valor no puede ser negativo.");
            }

            escritor.Write((byte)(numero >> 24));
            escritor.Write((byte)(0xFF & (numero >> 16)));
            escritor.Write((byte)(0xFF & (numero >> 8)));
            escritor.Write((byte)(0xFF & numero));
        }
    }
}
