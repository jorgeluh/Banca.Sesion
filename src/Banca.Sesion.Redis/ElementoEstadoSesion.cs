// <copyright file="ElementoEstadoSesion.cs" company="Canella S.A.">
// Desarrollado por Canella S.A. Todos los derechos reservados.
// </copyright>

namespace Banca.Sesion.Redis
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Web;
    using System.Web.SessionState;

    /// <summary>
    /// Clase auxiliar que permite deserializar los valores de los elementos de estado de sesión sólo cuando es necesario.
    /// </summary>
    internal class ElementoEstadoSesion
    {
        /// <summary>
        /// Lista que indica los tipos de dato de .NET Framework que son inmutables. Esto permite identificar si es necesario marcar la
        /// colección como "sucia" (modificada) también con la lectura de elementos y no sólo con la escritura.
        /// </summary>
        private static readonly HashSet<Type> TiposInmutables = new HashSet<Type>(19)
            {
                typeof(string),
                typeof(int),
                typeof(bool),
                typeof(DateTime),
                typeof(decimal),
                typeof(byte),
                typeof(char),
                typeof(float),
                typeof(double),
                typeof(sbyte),
                typeof(short),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(TimeSpan),
                typeof(Guid),
                typeof(IntPtr),
                typeof(UIntPtr),
            };

        /// <summary>
        /// El valor del elemento serializado como un vector de bytes.
        /// </summary>
        private byte[] bytesValor;

        /// <summary>
        /// El valor deserializado del elemento de estado de sesión.
        /// </summary>
        private object valor;

        /// <summary>
        /// Bandera que indica si el valor se encuentra deserializado. Principalmente sirve para diferenciar entre un valor <c>null</c>
        /// porque no ha sido deserializado o si en realidad el elemento tiene <c>null</c> como valor.
        /// </summary>
        private bool deserializado;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ElementoEstadoSesion"/>.
        /// </summary>
        /// <param name="llave">La llave (nombre) que identifica al elemento de estado de sesión. Como estas también se serializan al
        /// persistirlas en Redis, se serializa de inmediato para hacer que la serialización final sea más rápida.</param>
        public ElementoEstadoSesion(string llave)
            : this(Encoding.UTF8.GetBytes(llave))
        {
        }

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ElementoEstadoSesion"/>.
        /// </summary>
        /// <param name="bytesLlave">El nombre serializado del elemento de estado de sesión. Se mantiene de esa manera para que la
        /// serialización de la colección sea más rápida al evitar serializar de nuevo los nombres de los elementos.</param>
        public ElementoEstadoSesion(byte[] bytesLlave)
        {
            this.BytesLlave = bytesLlave;
            this.deserializado = false;
        }

        /// <summary>
        /// Obtiene la llave que identifica al elemento de estado de sesión ya serializada.
        /// </summary>
        public byte[] BytesLlave { get; }

        /// <summary>
        /// Obtiene un valor que indica si el tipo de dato del elemento de estado de sesión es inmutable (<c>true</c>) o no.
        /// </summary>
        public bool EsInmutable => this.valor != null && TiposInmutables.Contains(this.valor.GetType());

        /// <summary>
        /// Obtiene o establece el valor serializado del elemento de estado de sesión. Si el valor es nuevo y no se encontraba serializado,
        /// se serializa. También se serializa si estaba deserializado y su tipo de dato no es inmutable pues pudo haber sufrido alguna
        /// modificación y se serializa de nuevo para no perder esos cambios.
        /// </summary>
        public byte[] BytesValor
        {
            get
            {
                if (this.bytesValor == null || (this.deserializado && !this.EsInmutable))
                {
                    this.Serializar();
                }

                return this.bytesValor;
            }

            set
            {
                this.bytesValor = value;
            }
        }

        /// <summary>
        /// Obtiene o establece el valor deserializado del elemento de estado de sesión. Si se encontraba serializado, se serializa en este
        /// momento y se mantiene de esa manera para no repetir el trabajo.
        /// </summary>
        public object Valor
        {
            get
            {
                if (!this.deserializado && this.valor == null && this.bytesValor != null)
                {
                    this.Deserializar();
                    this.deserializado = true;
                }

                return this.valor;
            }

            set
            {
                this.valor = value;
                this.deserializado = true;
            }
        }

        /// <summary>
        /// Serializa el valor del elemento de estado de sesión y lo asigna a la propiedad <see cref="BytesValor"/>.
        /// </summary>
        /// <exception cref="HttpException">Si el valor es un objeto que no puede ser serializado como <c>byte[]</c>.</exception>
        private void Serializar()
        {
            using (MemoryStream flujoMemoria = new MemoryStream())
            {
                using (BinaryWriter escritorBinario = new BinaryWriter(flujoMemoria))
                {
                    if (this.valor == null)
                    {
                        escritorBinario.Write((byte)TipoDato.Null);
                    }
                    else if (this.valor is string valorString)
                    {
                        escritorBinario.Write((byte)TipoDato.String);
                        escritorBinario.Write(Encoding.UTF8.GetBytes(valorString));
                    }
                    else if (this.valor is int valorInt)
                    {
                        escritorBinario.Write((byte)TipoDato.Int32);
                        byte[] bytes =
                        {
                            (byte)(valorInt >> 24),
                            (byte)(0xFF & (valorInt >> 16)),
                            (byte)(0xFF & (valorInt >> 8)),
                            (byte)(0xFF & valorInt),
                        };
                        escritorBinario.Write(bytes);
                    }
                    else if (this.valor is bool valorBool)
                    {
                        escritorBinario.Write((byte)TipoDato.Boolean);
                        escritorBinario.Write(valorBool);
                    }
                    else if (this.valor is DateTime valorDateTime)
                    {
                        escritorBinario.Write((byte)TipoDato.DateTime);
                        escritorBinario.Write(valorDateTime.Ticks);
                    }
                    else if (this.valor is decimal valorDecimal)
                    {
                        escritorBinario.Write((byte)TipoDato.Decimal);
                        int[] bits = decimal.GetBits(valorDecimal);
                        for (int i = 0; i < 4; i++)
                        {
                            escritorBinario.Write(bits[i]);
                        }
                    }
                    else if (this.valor is byte valorByte)
                    {
                        escritorBinario.Write((byte)TipoDato.Byte);
                        escritorBinario.Write(valorByte);
                    }
                    else if (this.valor is char valorChar)
                    {
                        escritorBinario.Write((byte)TipoDato.Char);
                        escritorBinario.Write(valorChar);
                    }
                    else if (this.valor is float valorFloat)
                    {
                        escritorBinario.Write((byte)TipoDato.Single);
                        escritorBinario.Write(valorFloat);
                    }
                    else if (this.valor is double valorDouble)
                    {
                        escritorBinario.Write((byte)TipoDato.Double);
                        escritorBinario.Write(valorDouble);
                    }
                    else if (this.valor is sbyte valorSByte)
                    {
                        escritorBinario.Write((byte)TipoDato.SByte);
                        escritorBinario.Write(valorSByte);
                    }
                    else if (this.valor is short valorShort)
                    {
                        escritorBinario.Write((byte)TipoDato.Int16);
                        escritorBinario.Write(valorShort);
                    }
                    else if (this.valor is long valorLong)
                    {
                        escritorBinario.Write((byte)TipoDato.Int64);
                        escritorBinario.Write(valorLong);
                    }
                    else if (this.valor is ushort valorUInt16)
                    {
                        escritorBinario.Write((byte)TipoDato.UInt16);
                        escritorBinario.Write(valorUInt16);
                    }
                    else if (this.valor is uint valorUInt32)
                    {
                        escritorBinario.Write((byte)TipoDato.UInt32);
                        escritorBinario.Write(valorUInt32);
                    }
                    else if (this.valor is ulong valorUInt64)
                    {
                        escritorBinario.Write((byte)TipoDato.UInt64);
                        escritorBinario.Write(valorUInt64);
                    }
                    else if (this.valor is TimeSpan valorTimeSpan)
                    {
                        escritorBinario.Write((byte)TipoDato.TimeSpan);
                        escritorBinario.Write(valorTimeSpan.Ticks);
                    }
                    else if (this.valor is Guid valorGuid)
                    {
                        escritorBinario.Write((byte)TipoDato.Guid);
                        escritorBinario.Write(valorGuid.ToByteArray());
                    }
                    else if (this.valor is IntPtr valorIntPtr)
                    {
                        escritorBinario.Write((byte)TipoDato.IntPtr);
                        if (IntPtr.Size == 4)
                        {
                            escritorBinario.Write(valorIntPtr.ToInt32());
                        }
                        else
                        {
                            escritorBinario.Write(valorIntPtr.ToInt64());
                        }
                    }
                    else if (this.valor is UIntPtr valorUIntPtr)
                    {
                        escritorBinario.Write((byte)TipoDato.UIntPtr);
                        if (UIntPtr.Size == 4)
                        {
                            escritorBinario.Write(valorUIntPtr.ToUInt32());
                        }
                        else
                        {
                            escritorBinario.Write(valorUIntPtr.ToUInt64());
                        }
                    }
                    else
                    {
                        escritorBinario.Write((byte)TipoDato.Object);
                        BinaryFormatter formateadorBinario = new BinaryFormatter();
                        if (SessionStateUtility.SerializationSurrogateSelector != null)
                        {
                            formateadorBinario.SurrogateSelector = SessionStateUtility.SerializationSurrogateSelector;
                        }

                        try
                        {
                            formateadorBinario.Serialize(escritorBinario.BaseStream, this.valor);
                        }
                        catch (Exception ex)
                        {
                            throw new HttpException(
                                $"No se pudo serializar el valor de la variable {Encoding.UTF8.GetString(this.BytesLlave)}.", ex);
                        }
                    }
                }

                this.BytesValor = flujoMemoria.ToArray();
            }
        }

        /// <summary>
        /// Deserializa el valor del elemento de estado sesión y lo asigna a la propiedad <see cref="Valor"/>.
        /// </summary>
        /// <remarks>
        /// La forma como se serializan los valores de tipo <see cref="string"/> e <see cref="int"/> es la misma que la que usa .NET y son
        /// los únicos tipos de dato aparte de <c>byte[]</c> que soporta de manera predeterminada. También parecía una manera más eficiente
        /// de hacerlo que usar la serialización del proveedor de sesión de .NET Framework para Redis.
        /// </remarks>
        private void Deserializar()
        {
            // Los tipos de dato más frecuentes se manejan de manera más directa como se hace en .NET.
            TipoDato tipoDato = (TipoDato)this.bytesValor[0];
            switch (tipoDato)
            {
                case TipoDato.String:
                    this.Valor = Encoding.UTF8.GetString(this.bytesValor.Skip(1).ToArray());
                    return;
                case TipoDato.Int32:
                    this.Valor = this.bytesValor[1] << 24 | this.bytesValor[2] << 16 | this.bytesValor[3] << 8 | this.bytesValor[4];
                    return;
                case TipoDato.Null:
                    this.Valor = null;
                    return;
            }

            using (MemoryStream flujoMemoria = new MemoryStream(this.bytesValor.Skip(1).ToArray()))
            {
                using (BinaryReader lectorBinario = new BinaryReader(flujoMemoria))
                {
                    switch (tipoDato)
                    {
                        case TipoDato.Boolean:
                            this.Valor = lectorBinario.ReadBoolean();
                            return;
                        case TipoDato.DateTime:
                            this.Valor = new DateTime(lectorBinario.ReadInt64(), DateTimeKind.Unspecified);
                            return;
                        case TipoDato.Decimal:
                            int[] bits = new int[4];
                            for (int i = 0; i < 4; i++)
                            {
                                bits[i] = lectorBinario.ReadInt32();
                            }

                            this.Valor = new decimal(bits);
                            return;
                        case TipoDato.Byte:
                            this.Valor = lectorBinario.ReadByte();
                            return;
                        case TipoDato.Char:
                            this.Valor = lectorBinario.ReadChar();
                            return;
                        case TipoDato.Single:
                            this.Valor = lectorBinario.ReadSingle();
                            return;
                        case TipoDato.Double:
                            this.Valor = lectorBinario.ReadDouble();
                            return;
                        case TipoDato.SByte:
                            this.Valor = lectorBinario.ReadSByte();
                            return;
                        case TipoDato.Int16:
                            this.Valor = lectorBinario.ReadInt16();
                            return;
                        case TipoDato.Int64:
                            this.Valor = lectorBinario.ReadInt64();
                            return;
                        case TipoDato.UInt16:
                            this.Valor = lectorBinario.ReadUInt16();
                            return;
                        case TipoDato.UInt32:
                            this.Valor = lectorBinario.ReadUInt32();
                            return;
                        case TipoDato.UInt64:
                            this.Valor = lectorBinario.ReadUInt64();
                            return;
                        case TipoDato.TimeSpan:
                            this.Valor = new TimeSpan(lectorBinario.ReadInt64());
                            return;
                        case TipoDato.Guid:
                            this.Valor = new Guid(lectorBinario.ReadBytes(16));
                            return;
                        case TipoDato.IntPtr:
                            if (IntPtr.Size == 4)
                            {
                                this.Valor = new IntPtr(lectorBinario.ReadInt32());
                            }
                            else
                            {
                                this.Valor = new IntPtr(lectorBinario.ReadInt64());
                            }

                            return;
                        case TipoDato.UIntPtr:
                            if (UIntPtr.Size == 4)
                            {
                                this.Valor = new UIntPtr(lectorBinario.ReadUInt32());
                            }
                            else
                            {
                                this.Valor = new UIntPtr(lectorBinario.ReadUInt64());
                            }

                            return;

                        case TipoDato.Object:
                            BinaryFormatter formateadorBinario = new BinaryFormatter();
                            if (SessionStateUtility.SerializationSurrogateSelector != null)
                            {
                                formateadorBinario.SurrogateSelector = SessionStateUtility.SerializationSurrogateSelector;
                            }

                            this.Valor = formateadorBinario.Deserialize(lectorBinario.BaseStream);
                            return;
                    }
                }
            }
        }
    }
}
