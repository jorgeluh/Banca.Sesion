// <copyright file="TipoDato.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    /// <summary>
    /// Diferencia tipos de dato de los elementos de estado de sesión para serializarlos y deserializarlos de manera acorde.
    /// </summary>
    internal enum TipoDato : byte
    {
        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="string"/>.
        /// </summary>
        String = 1,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="int"/>.
        /// </summary>
        Int32,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="bool"/>.
        /// </summary>
        Boolean,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="DateTime"/>.
        /// </summary>
        DateTime,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="decimal"/>.
        /// </summary>
        Decimal,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="byte"/>.
        /// </summary>
        Byte,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="char"/>.
        /// </summary>
        Char,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="float"/>.
        /// </summary>
        Single,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="double"/>.
        /// </summary>
        Double,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="sbyte"/>.
        /// </summary>
        SByte,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="short"/>.
        /// </summary>
        Int16,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="long"/>.
        /// </summary>
        Int64,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="ushort"/>.
        /// </summary>
        UInt16,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="uint"/>.
        /// </summary>
        UInt32,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="ulong"/>.
        /// </summary>
        UInt64,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="TimeSpan"/>.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="System.Guid"/>.
        /// </summary>
        Guid,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="System.IntPtr"/>.
        /// </summary>
        IntPtr,

        /// <summary>
        /// El elemento de estado de sesión es de tipo <see cref="System.UIntPtr"/>.
        /// </summary>
        UIntPtr,

        /// <summary>
        /// El elemento de estado de sesión es un objeto u otro tipo de dato no contemplado.
        /// </summary>
        Object,

        /// <summary>
        /// El valor del elemento de sesión es nulo y no se puede determinar un tipo de dato por ello.
        /// </summary>
        Null,
    }
}
