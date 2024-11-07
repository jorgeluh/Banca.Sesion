// <copyright file="ColeccionElementosEstadoSesion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web.SessionState;

    /// <summary>
    /// Presenta la colección de elementos de estado de sesión desde donde se pueden recuperar sus valores o agregar nuevos. Es compatible
    /// con implementaciones del proveedor de estado de sesión de .NET Framework y usa Redis como medio de almacenamiento.
    /// </summary>
    /// <remarks>
    /// La forma como esta colección serializa y deserializa los elementos de estado de sesión es compatible con la forma como funciona el
    /// caché de sesión de .NET.
    /// </remarks>
    public class ColeccionElementosEstadoSesion : NameObjectCollectionBase, ISessionStateItemCollection
    {
        /// <summary>
        /// Este es un "prefijo" de serialización que emplea la implementación de caché de sesión de .NET. Es necesario para mantener la
        /// compatibilidad.
        /// </summary>
        private const byte RevisionSerializacion = 2;

        /// <summary>
        /// Longitud de un vector de llaves que .NET emplea en la serialización de los elementos de estado de sesión.
        /// </summary>
        private const int CantidadBytesIdentificadorSesion = 16;

        /// <summary>
        /// Mantiene los bytes de identificador de sesión que .NET incluye en los datos serializados de los elementos de estado de sesión.
        /// </summary>
        private byte[] bytesIdentificadorSesion;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ColeccionElementosEstadoSesion"/>. La colección no contiene elementos de
        /// estado de sesión preexistentes.
        /// </summary>
        public ColeccionElementosEstadoSesion()
            : base()
        {
        }

        /// <summary>
        /// Obtiene o establece un valor que indica si los datos de la sesión han sido modificados durante la petición, lo que implica que
        /// deben ser actualizados en el medio de almacenamiento.
        /// </summary>
        public bool Dirty { get; set; }

        /// <summary>
        /// Obtiene o establece un elemento de estado de sesión asociado a un nombre (llave).
        /// </summary>
        /// <remarks>
        /// Los valores se mantienen serializados hasta el momento en que se solicitan para evitar deserializarlos innecesariamente si no
        /// son requeridos.
        /// </remarks>
        /// <remarks>
        /// La colección se marca como "sucia" si se crea o actualiza una variable de sesión o si se solicita un elemento de estado de
        /// sesión que no sea inmutable. Esto significa que puede ser modificado sin tener que volver a usar la operación de actualización
        /// de esta colección.
        /// </remarks>
        /// <param name="name">El nombre con el que se podrá recuperar el valor del elemento de estado de sesión.</param>
        /// <returns>El valor del elemento de estado de sesión asociado al nombre indicado. <c>null</c> si el nombre no se encuentra en la
        /// colección.</returns>
        public object this[string name]
        {
            get
            {
                object valor = null;
                if (this.BaseGet(name) is ElementoEstadoSesion elemento)
                {
                    valor = elemento.Valor;

                    // La propiedad EsInmutable se establece después de deserializar el valor y se usa porque el valor puede cambiar sin
                    // asignarlo a la variable de sesión de nuevo según la implementación de la clase SessionStateItemCollection.
                    this.Dirty = this.Dirty || !elemento.EsInmutable;
                }

                return valor;
            }

            set
            {
                this.BaseSet(name, new ElementoEstadoSesion(name) { Valor = value });
                this.Dirty = true;
            }
        }

        /// <summary>
        /// Obtiene o establece un elemento de estado de sesión no basado en un nombre sino en un índice.
        /// </summary>
        /// <remarks>
        /// La colección se marca como "sucia" si se crea o actualiza una variable de sesión o si se solicita un elemento de estado de
        /// sesión que no sea inmutable. Esto significa que puede ser modificado sin tener que volver a usar la operación de actualización
        /// de esta colección.
        /// </remarks>
        /// <param name="index">El índice del elemento de estado de sesión a obtener o establecer.</param>
        /// <returns>El valor del elemento de estado de sesión que se encuentra en la posición solicitada.</returns>
        public object this[int index]
        {
            get
            {
                object valor = null;
                if (this.BaseGet(index) is ElementoEstadoSesion elemento)
                {
                    valor = elemento.Valor;

                    // La propiedad EsInmutable se establece después de deserializar el valor y se usa porque el valor puede cambiar sin
                    // asignarlo a la variable de sesión de nuevo según la implementación de la clase SessionStateItemCollection.
                    this.Dirty = this.Dirty || !elemento.EsInmutable;
                }

                return valor;
            }

            set
            {
                ElementoEstadoSesion elemento = (ElementoEstadoSesion)this.BaseGet(index);
                elemento.Valor = value;
                this.Dirty = true;
            }
        }

        /// <summary>
        /// Elimina todos los elementos de estado de sesión de la colección.
        /// </summary>
        public void Clear()
        {
            this.BaseClear();
            this.Dirty = true;
        }

        /// <summary>
        /// Elimina el elemento de estado de sesión identificado por el nombre indicado.
        /// </summary>
        /// <param name="name">El nombre que identifica al elemento de estado de sesión que será eliminado.</param>
        public void Remove(string name)
        {
            this.BaseRemove(name);
            this.Dirty = true;
        }

        /// <summary>
        /// Elimina el elemento de estado de sesión que se encuentra en el índice especificado.
        /// </summary>
        /// <param name="index">El índice del elemento de estado de sesión a eliminar.</param>
        public void RemoveAt(int index)
        {
            this.BaseRemoveAt(index);
            this.Dirty = true;
        }

        /// <summary>
        /// Serializa la colección de elementos de estado de sesión. Emplea el mismo formato que .NET para que los elementos de estado de
        /// sesión se puedan leer y escribir también por su proveedor de caché.
        /// </summary>
        /// <param name="escritor">El escritor donde se serializan los datos.</param>
        internal void Serializar(BinaryWriter escritor)
        {
            escritor.Write(RevisionSerializacion);
            escritor.SerializarA3Bytes(this.Count);
            escritor.Write(this.bytesIdentificadorSesion, 0, CantidadBytesIdentificadorSesion);

            foreach (ElementoEstadoSesion elemento in this.BaseGetAllValues().Cast<ElementoEstadoSesion>())
            {
                escritor.SerializarA2Bytes(elemento.BytesLlave.Length);
                escritor.Write(elemento.BytesLlave, 0, elemento.BytesLlave.Length);
                escritor.SerializarA4Bytes(elemento.BytesValor.Length);
                escritor.Write(elemento.BytesValor);
            }
        }

        /// <summary>
        /// Deserializa los valores recuperados del almacén de estado de sesión para cargar la colección de elementos.
        /// </summary>
        /// <param name="lector">El lector binario desde donde se deserializa la colección de elementos de estado de sesión.</param>
        internal void Deserializar(BinaryReader lector)
        {
            if (lector == null || lector.ReadByte() != RevisionSerializacion)
            {
                this.Dirty = true;
                return;
            }

            int cantidadElementos = lector.DeserializarDesde3Bytes();
            this.bytesIdentificadorSesion = lector.LeerBytes(CantidadBytesIdentificadorSesion);
            int longitudLlave;
            byte[] bytesLlave;
            int longitudDatos;
            for (int i = 0; i < cantidadElementos; i++)
            {
                longitudLlave = lector.DeserializarDesde2Bytes();
                bytesLlave = lector.LeerBytes(longitudLlave);
                longitudDatos = lector.DeserializarDesde4Bytes();

                this.BaseAdd(
                    Encoding.UTF8.GetString(bytesLlave),
                    new ElementoEstadoSesion(bytesLlave) { BytesValor = lector.LeerBytes(longitudDatos) });
            }

            this.Dirty = false;
        }
    }
}
