using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Text;

namespace Service.Contracts
{
	/* 
	 * The objective of this class is providing a foundation on top of which it is easy to createto a binary
	 * communication protocol.
	 */

	/// <summary>
	/// This enumeration contains all the different data types supported by the serialization buffer.
	/// </summary>
	public enum DataTypes
	{
		dtNull = 0x00,
		dtBool = 0xD0,
		dtByte = 0xD1,
		dtChar = 0xD2,
		dtInt32 = 0xD3,
		dtInt64 = 0xD4,
		dtSingle = 0xD5,
		dtDouble = 0xD6,
		dtDecimal = 0xD7,
		dtString = 0xD8,
		dtUTFString = 0xD9,
		dtDateTime = 0xDA,
		dtTimeSpan = 0xDB,
		dtGuid = 0xDC,
		dtObject = 0xDE,
		dtArray = 0xDF,
		dtList = 0xE0,
		dtDictionary = 0xE1,
		dtStream = 0xE2,
		dtEOM = 0xFF
	}

	/// <summary>
	/// An interface that objects need to implement in order for them to be sent as parameters to remote methods using the messaging protocol.
	/// </summary>
	public interface ICanSerialize
	{
		void Serialize(SerializationBuffer buffer);
		void Deserialize(SerializationBuffer buffer);
	}


	/// <summary>
	/// Object used to serialize and deserialize data sent using the messaging protocol.
	/// </summary>
	public class SerializationBuffer
	{
		internal const int INITIAL_BUFFER_SIZE = 16384;     //16KB	<--- Amount of memory allocated initially for the buffer (this buffer is used to exchange messages, also notice the buffer can grow as much as needed to accomodate large messages)
		internal const int MAX_STREAM_SIZE = 1073741824;    //1GB	<--- Maximum file size for stream based transfers. Notice however that, while receiving a stream your code is responsible of determinig where to put the data, the recomendation is to link streams to physical files to avoid using excessive amounts of memory, but the code you write can as easily place all the received data in a memory stream.
		internal const int STREAM_BLOCK_SIZE = 4096;        //4KB	<--- When sending / receiving a stream, we send data in small blocks of up to this size. It makes no sense to increase this to a much larger size, as TCP packets will start fragmenting anyway if the size is too large.

		internal byte[] buffer;
		internal int availableData;
		internal int position;

		internal IScope scope;
		internal DateTime lastResize;


		public SerializationBuffer(IScope scope)
		{
			this.scope = scope;
			lastResize = DateTime.Now;
			InitBuffer(INITIAL_BUFFER_SIZE);
		}


		internal SerializationBuffer(IScope scope, int size)
		{
			this.scope = scope;
			lastResize = DateTime.Now;
			InitBuffer(size);
		}


		// Initializes a new instance pointing to a given "section" of an already allocated buffer
		internal SerializationBuffer(IScope scope, byte[] buffer, int position, int availableData)
		{
			this.scope = scope;
			lastResize = DateTime.Now;
			this.buffer = buffer;
			this.position = position;
			this.availableData = availableData;
		}


		private void InitBuffer(int size)
		{
			buffer = new byte[size];
			availableData = 0;
			position = 0;
		}


		/// <summary>
		/// Verifies that the buffer is capable of storing the specified amount of bytes. Enlarging the buffer if necesary.
		/// </summary>
		/// <param name="requiredBufferSize">Specifies the total amount of bytes that the underlaying buffer should be able to store.</param>
		/// <remarks>
		/// IMPORTANT: Calling EnsureCapacity might update the buffer internal members like segment, segmentLength and segmentOffset.
		/// It is important to re-read those values after calling EnsureCapacity.
		/// </remarks>
		public virtual void EnsureCapacity(int requiredBufferSize)
		{
			if (requiredBufferSize > buffer.Length)
			{
				byte[] tmp = new byte[requiredBufferSize * 2];
				Buffer.BlockCopy(buffer, 0, tmp, 0, availableData);
				buffer = tmp;
				lastResize = DateTime.Now;
			}
		}


		public int AvailableData { get { return availableData; } }


		public int Position
		{
			get { return position; }
			set { position = value; }
		}


		public bool EOM
		{
			get
			{
				if (position >= availableData || PeekByte(position) == (byte)DataTypes.dtEOM)
					return true;
				else
					return false;
			}
		}


		public unsafe byte PeekByte(int offset)
		{
			byte value;
			fixed (byte* p = &buffer[offset])
			{
				value = *p;
			}
			return value;
		}


		public unsafe void SetByte(int offset, byte value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*p = value;
			}
		}


		public unsafe Guid PeekGuid(int offset)
		{
			byte[] bytes = new byte[16];
			fixed (byte* p1 = &buffer[offset], p2 = &bytes[0])
			{
				var src = (long*)p1;
				var dst = (long*)p2;
				dst[0] = src[0];
				dst[1] = src[1];
			}
			return new Guid(bytes);
		}


		public unsafe void SetGuid(int offset, Guid value)
		{
			byte[] bytes = value.ToByteArray();
			fixed (byte* p1 = &bytes[0], p2 = &buffer[offset])
			{
				var src = (long*)p1;
				var dst = (long*)p2;
				dst[0] = src[0];
				dst[1] = src[1];
			}
		}


		public unsafe char PeekChar(int offset)
		{
			char value;
			fixed (byte* p = &buffer[offset])
			{
				value = *((char*)p);
			}
			return value;
		}


		public unsafe void SetChar(int offset, char value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*((char*)p) = value;
			}
		}


		public unsafe int PeekInt32(int offset)
		{
			int value;
			fixed (byte* p = &buffer[offset])
			{
				value = *((int*)p);
			}
			return value;
		}


		public unsafe void SetInt32(int offset, int value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*((int*)p) = value;
			}
		}


		public unsafe long PeekInt64(int offset)
		{
			long value;
			fixed (byte* p = &buffer[offset])
			{
				value = *((long*)p);
			}
			return value;
		}


		public unsafe void SetInt64(int offset, long value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*((long*)p) = value;
			}
		}


		public unsafe float PeekSingle(int offset)
		{
			float value;
			fixed (byte* p = &buffer[offset])
			{
				value = *((float*)p);
			}
			return value;
		}


		public unsafe void SetSingle(int offset, float value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*((float*)p) = value;
			}
		}


		public unsafe double PeekDouble(int offset)
		{
			double value;
			fixed (byte* p = &buffer[offset])
			{
				value = *((double*)p);
			}
			return value;
		}


		public unsafe void SetDouble(int offset, double value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*((double*)p) = value;
			}
		}


		public unsafe decimal PeekDecimal(int offset)
		{
			decimal value;
			fixed (byte* p = &buffer[offset])
			{
				value = *((decimal*)p);
			}
			return value;
		}


		public unsafe void SetDecimal(int offset, decimal value)
		{
			fixed (byte* p = &buffer[offset])
			{
				*((decimal*)p) = value;
			}
		}


		public void Reset()
		{
			position = 0;
			availableData = 0;
		}


		public int IndexOf(byte[] search)
		{
			if (buffer == null || buffer.Length == 0 || availableData == 0)
				return -1;

			if (search == null || search.Length == 0)
				return -1;

			if (search.Length > availableData)
				return -1;

			int leftIndex, rightIndex;
			byte firstByte = search[0];
			int maxIndex = availableData - search.Length + 1;
			for (leftIndex = 0; leftIndex < maxIndex; leftIndex++)
			{
				if (buffer[leftIndex] == firstByte)
				{
					rightIndex = search.Length - 1;
					while (rightIndex > 0 && buffer[leftIndex + rightIndex] == search[rightIndex])
					{
						rightIndex--;
					}
					if (rightIndex == 0) return leftIndex;
				}
			}
			return -1;
		}


		public void AddNext(object value)
		{
			if (value == null)
				AddNullReference();
			else
			{
				Type type = value.GetType();
				if (type == typeof(bool))
				{
					AddBoolean((bool)value);
				}
				else if (type == typeof(byte))
				{
					AddByte((byte)value);
				}
				else if (type == typeof(char))
				{
					AddChar((char)value);
				}
				else if (type == typeof(int) || type.IsEnum)
				{
					AddInt32((int)value);
				}
				else if (type == typeof(long))
				{
					AddInt64((long)value);
				}
				else if (type == typeof(float))
				{
					AddSingle((float)value);
				}
				else if (type == typeof(double))
				{
					AddDouble((double)value);
				}
				else if (type == typeof(decimal))
				{
					AddDecimal((decimal)value);
				}
				else if (type == typeof(string))
				{
					AddString((string)value);
				}
				else if (type == typeof(DateTime))
				{
					AddDateTime((DateTime)value);
				}
				else if (type == typeof(TimeSpan))
				{
					AddTimeSpan((TimeSpan)value);
				}
				else if (type == typeof(Guid))
				{
					AddGuid((Guid)value);
				}
				else if (type.IsArray)
				{
					AddArray(value);
				}
				else if (typeof(IList).IsAssignableFrom(type))
				{
					AddList(value);
				}
				else if (typeof(Stream).IsAssignableFrom(type))
				{
					AddStream((Stream)value);
				}
				else if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass)
				{
					AddObject(value);
				}
				else
					throw new Exception("Cannot add a value of type " + type.FullName);
			}
		}


		internal void Skip()
		{
			int len;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			else
			{
				DataTypes dt = (DataTypes)PeekByte(position);
				switch (dt)
				{
					case DataTypes.dtNull:
						position++;
						break;
					case DataTypes.dtEOM:
						position++;
						break;
					case DataTypes.dtBool:
						GetBoolean();
						break;
					case DataTypes.dtByte:
						GetByte();
						break;
					case DataTypes.dtChar:
						GetChar();
						break;
					case DataTypes.dtInt32:
						GetInt32();
						break;
					case DataTypes.dtInt64:
						GetInt64();
						break;
					case DataTypes.dtSingle:
						GetSingle();
						break;
					case DataTypes.dtDouble:
						GetDouble();
						break;
					case DataTypes.dtDecimal:
						GetDecimal();
						break;
					case DataTypes.dtString:
						GetString();
						break;
					case DataTypes.dtDateTime:
						GetDateTime();
						break;
					case DataTypes.dtTimeSpan:
						GetTimeSpan();
						break;
					case DataTypes.dtGuid:
						GetGuid();
						break;
					case DataTypes.dtObject:
						len = PeekInt32(position + 1);
						position += len;
						break;
					case DataTypes.dtArray:
					case DataTypes.dtList:
					case DataTypes.dtStream:
						// TODO: Implement skip logic for these types (they can be skiped, but logic is involved) and currently I dont need this functionality.
						throw new Exception("Array & List Cannot be skiped.");
					default:
						throw new Exception($"Found an element that cannot be skiped: {dt}");
				}
			}
		}


		public object GetNext()
		{
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			else
			{
				DataTypes dt = (DataTypes)PeekByte(position);
				switch (dt)
				{
					case DataTypes.dtNull:
						position++;
						return null;
					case DataTypes.dtBool:
						return GetBoolean();
					case DataTypes.dtByte:
						return GetByte();
					case DataTypes.dtChar:
						return GetChar();
					case DataTypes.dtInt32:
						return GetInt32();
					case DataTypes.dtInt64:
						return GetInt64();
					case DataTypes.dtSingle:
						return GetSingle();
					case DataTypes.dtDouble:
						return GetDouble();
					case DataTypes.dtDecimal:
						return GetDecimal();
					case DataTypes.dtString:
						return GetString();
					case DataTypes.dtDateTime:
						return GetDateTime();
					case DataTypes.dtTimeSpan:
						return GetTimeSpan();
					case DataTypes.dtGuid:
						return GetGuid();
					case DataTypes.dtObject:
					case DataTypes.dtArray:
					case DataTypes.dtList:
						throw new Exception("Cannot retrieve element from buffer, try using GetNext<T>() instead.");
					case DataTypes.dtStream:
						return GetStream();
					case DataTypes.dtEOM:
						position++;
						return null;
					default:
						throw new Exception("Found an element that cannot be deserialized.");
				}
			}
		}


		public object GetNext(Type type)
		{
			foreach (MethodInfo mi in typeof(SerializationBuffer).GetMember("GetNext", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance))
			{
				if (mi.GetGenericArguments().Length > 0)
				{
					MethodInfo method = mi.MakeGenericMethod(type);
					return method.Invoke(this, null);
				}
			}
			throw new Exception("Unexpected code path");
		}


		public T GetNext<T>()
		{
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			else
			{
				DataTypes dt = (DataTypes)PeekByte(position);
				switch (dt)
				{
					case DataTypes.dtNull:
						position++;
						return default(T);
					case DataTypes.dtObject:
						return FastGetObject<T>();
					case DataTypes.dtArray:
						return (T)GetArray(typeof(T));
					case DataTypes.dtList:
						return (T)GetList(typeof(T));
					case DataTypes.dtEOM:
						position++;
						return default(T);
					default:
						return (T)GetNext();
				}
			}
		}


		public void AddBoolean(bool value)
		{
			EnsureCapacity(position + 2);
			SetByte(position, (byte)DataTypes.dtBool);
			SetByte(position + 1, (byte)(value ? 1 : 0));
			position += 2;
			availableData += 2;
		}


		public bool GetBoolean()
		{
			bool value;
			byte typeMark;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return false;
			}
			else if (typeMark == (byte)DataTypes.dtBool && position + 2 <= availableData)
			{
				value = (PeekByte(position + 1) == 1);
				position += 2;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddByte(byte value)
		{
			EnsureCapacity(position + 2);
			SetByte(position, (byte)DataTypes.dtByte);
			SetByte(position + 1, value);
			position += 2;
			availableData += 2;
		}


		public byte GetByte()
		{
			byte value;
			byte typeMark;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return 0;
			}
			else if (typeMark == (byte)DataTypes.dtByte && position + 2 <= availableData)
			{
				value = PeekByte(position + 1);
				position += 2;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddChar(char value)
		{
			const int requiredBytes = sizeof(char) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtChar);
			SetChar(position + 1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public char GetChar()
		{
			char value;
			byte typeMark;
			const int requiredBytes = sizeof(char) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return '\0';
			}
			else if (typeMark == (byte)DataTypes.dtChar && position + requiredBytes <= availableData)
			{
				value = PeekChar(position + 1);
				position += requiredBytes;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddInt32(int value)
		{
			const int requiredBytes = sizeof(int) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtInt32);
			SetInt32(position + 1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public int GetInt32()
		{
			int value;
			byte typeMark;
			const int requiredBytes = sizeof(int) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return 0;
			}
			else if (typeMark == (byte)DataTypes.dtInt32 && position + requiredBytes <= availableData)
			{
				value = PeekInt32(position + 1);
				position += requiredBytes;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public object GetEnum(Type enumType)
		{
			byte typeMark;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
				return 0;
			else if (typeMark == (byte)DataTypes.dtInt32)
				return GetInt32();
			else if (typeMark == (byte)DataTypes.dtString || typeMark == (byte)DataTypes.dtUTFString)
				return Enum.Parse(enumType, GetString());
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddInt64(long value)
		{
			const int requiredBytes = sizeof(long) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtInt64);
			SetInt64(position + 1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public long GetInt64()
		{
			long value;
			byte typeMark;
			const int requiredBytes = sizeof(long) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return 0L;
			}
			else if (typeMark == (byte)DataTypes.dtInt64 && position + requiredBytes <= availableData)
			{
				value = PeekInt64(position + 1);
				position += requiredBytes;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddSingle(float value)
		{
			const int requiredBytes = sizeof(float) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtSingle);
			SetSingle(position + 1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public float GetSingle()
		{
			byte typeMark;
			float value;
			const int requiredBytes = sizeof(float) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return 0f;
			}
			else if (typeMark == (byte)DataTypes.dtSingle && position + requiredBytes <= availableData)
			{
				value = PeekSingle(position + 1);
				position += requiredBytes;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddDouble(double value)
		{
			const int requiredBytes = sizeof(double) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtDouble);
			SetDouble(position + 1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public double GetDouble()
		{
			double value;
			byte typeMark;
			const int requiredBytes = sizeof(double) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (PeekByte(position) == (byte)DataTypes.dtEOM)
			{
				return 0d;
			}
			else if (PeekByte(position) == (byte)DataTypes.dtDouble && position + requiredBytes <= availableData)
			{
				value = PeekDouble(position + 1);
				position += requiredBytes;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddDecimal(decimal value)
		{
			const int requiredBytes = sizeof(decimal) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtDecimal);
			SetDecimal(position + 1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public decimal GetDecimal()
		{
			decimal value;
			byte typeMark;
			const int requiredBytes = sizeof(decimal) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return 0m;
			}
			else if (PeekByte(position) == (byte)DataTypes.dtDecimal && position + requiredBytes <= availableData)
			{
				value = PeekDecimal(position + 1);
				position += requiredBytes;
				return value;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddDateTime(DateTime value)
		{
			const int requiredBytes = sizeof(long) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtDateTime);
			SetInt64(position + 1, value.ToBinary());
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public DateTime GetDateTime()
		{
			long binary;
			byte typeMark;
			const int requiredBytes = sizeof(long) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return DateTime.MinValue;
			}
			else if (typeMark == (byte)DataTypes.dtDateTime && position + requiredBytes <= availableData)
			{
				binary = PeekInt64(position + 1);
				position += requiredBytes;
				return DateTime.FromBinary(binary);
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddTimeSpan(TimeSpan value)
		{
			const int requiredBytes = sizeof(long) + 1;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtTimeSpan);
			SetInt64(position + 1, value.Ticks);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public TimeSpan GetTimeSpan()
		{
			long ticks;
			byte typeMark;
			const int requiredBytes = sizeof(long) + 1;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return TimeSpan.FromTicks(0);
			}
			else if (typeMark == (byte)DataTypes.dtTimeSpan && position + requiredBytes <= availableData)
			{
				ticks = PeekInt64(position + 1);
				position += requiredBytes;
				return new TimeSpan(ticks);
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddGuid(Guid value)
		{
			const int requiredBytes = 17;
			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtGuid);
			SetGuid(position+1, value);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		public Guid GetGuid()
		{
			const int requiredBytes = 17;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			var typeMark = PeekByte(position);
			if (typeMark == (byte)DataTypes.dtEOM)
			{
				return Guid.Empty;
			}
			else if (typeMark == (byte)DataTypes.dtGuid && position + requiredBytes <= availableData)
			{
				var guid = PeekGuid(position + 1);
				position += requiredBytes;
				return guid;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddString(string value, bool utfEncoding = false)
		{
			if (value == null) AddNullReference();
			else
			{
				if (utfEncoding)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(value);
					int len = bytes.Length;
					int headerSize = 1 + sizeof(int);
					int requiredBytes = headerSize + len;
					EnsureCapacity(position + requiredBytes);
					SetByte(position, (byte)DataTypes.dtUTFString);
					SetInt32(position + 1, len);
					System.Buffer.BlockCopy(bytes, 0, buffer, position + headerSize, len);
					position += requiredBytes;
					availableData += requiredBytes;
				}
				else
				{
					int len = value.Length;
					int headerSize = 1 + sizeof(int);
					int requiredBytes = headerSize + len * sizeof(char);
					EnsureCapacity(position + requiredBytes);
					SetByte(position, (byte)DataTypes.dtString);
					SetInt32(position + 1, len);
					System.Buffer.BlockCopy(value.ToCharArray(), 0, buffer, position + headerSize, len * sizeof(char));
					position += requiredBytes;
					availableData += requiredBytes;
				}
			}
		}


		public string GetString()
		{
			int len;
			string result;
			if (position >= availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			DataTypes dt = (DataTypes)PeekByte(position);
			if (dt == DataTypes.dtNull)
			{
				position++;
				return null;
			}
			else if (PeekByte(position) == (byte)DataTypes.dtEOM)
			{
				return null;
			}
			else if (dt == DataTypes.dtString)
			{
				if (position + 1 + sizeof(int) > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				len = PeekInt32(position + 1);
				int requiredBytes = 1 + sizeof(int) + len * sizeof(char);
				if (position + requiredBytes > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				if (len == 0)
				{
					result = "";
				}
				else
				{
					unsafe
					{
						fixed (byte* p = &buffer[position + 1 + sizeof(int)])
						{
							result = new String((char*)p, 0, len);
						}
					}
				}
				position += requiredBytes;
				return result;
			}
			else if (dt == DataTypes.dtUTFString)
			{
				int headerSize = 1 + sizeof(int);
				if (position + headerSize > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				len = PeekInt32(position + 1);
				int requiredBytes = headerSize + len;
				if (position + requiredBytes > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				if (len == 0)
					result = "";
				else
					result = Encoding.UTF8.GetString(buffer, position + headerSize, len);
				position += requiredBytes;
				return result;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public string PeekString(int offset)
		{
			int len;
			string result;
			if (offset + 1 > availableData)
				throw new Exception("Serialization error. Reached the end of the buffer.");
			int headerSize = 1 + sizeof(int);
			DataTypes dt = (DataTypes)PeekByte(offset);
			if (dt == DataTypes.dtNull)
			{
				return null;
			}
			else if (dt == DataTypes.dtString)
			{
				if (offset + headerSize > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				len = PeekInt32(offset + 1);
				int requiredBytes = headerSize + (len * sizeof(char));
				if (offset + requiredBytes > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				unsafe
				{
					fixed (byte* p = &buffer[offset + headerSize])
					{
						result = new String((char*)p, 0, len);
					}
				}
				return result;
			}
			else if (dt == DataTypes.dtUTFString)
			{
				if (offset + headerSize >= availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				len = PeekInt32(offset + 1);
				int requiredBytes = headerSize + len;
				if (offset + requiredBytes > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				result = Encoding.UTF8.GetString(buffer, offset + headerSize, len);
				return result;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		public void AddArray<T>(T[] data)
		{
			if (data == null) AddNullReference();
			else
			{
				Type t = typeof(T);
				if (t == typeof(bool))
					AddNativeArray<T>(data, DataTypes.dtBool, sizeof(bool));
				else if (t == typeof(char))
					AddNativeArray<T>(data, DataTypes.dtChar, sizeof(char));
				else if (t == typeof(byte))
					AddNativeArray<T>(data, DataTypes.dtByte, sizeof(byte));
				else if (t == typeof(int))
					AddNativeArray<T>(data, DataTypes.dtInt32, sizeof(int));
				else if (t == typeof(long))
					AddNativeArray<T>(data, DataTypes.dtInt64, sizeof(long));
				else if (t == typeof(float))
					AddNativeArray<T>(data, DataTypes.dtSingle, sizeof(float));
				else if (t == typeof(double))
					AddNativeArray<T>(data, DataTypes.dtDouble, sizeof(double));
				else
					AddObjectArray<T>(data);
			}
		}


		private void AddNativeArray<T>(T[] data, DataTypes subType, int typeSize)
		{
			const int headerSize = 2 + sizeof(int);
			int count = data.Length;
			int requiredBytes = headerSize + count * typeSize;

			EnsureCapacity(position + requiredBytes);
			SetByte(position, (byte)DataTypes.dtArray);
			SetByte(position + 1, (byte)subType);
			SetInt32(position + 2, count);
			System.Buffer.BlockCopy(data, 0, buffer, position + headerSize, count * typeSize);
			position += requiredBytes;
			availableData += requiredBytes;
		}


		private void AddArray(object obj)
		{
			if (obj == null)
			{
				AddNullReference();
			}
			else
			{
				Type elmType = obj.GetType().GetElementType();
				MethodInfo mi = typeof(SerializationBuffer).GetMethod("AddArray", BindingFlags.Public | BindingFlags.Instance);
				MethodInfo method = mi.MakeGenericMethod(elmType);
				method.Invoke(this, new object[] { obj });
			}
		}


		private object GetArray(Type t)
		{
			Type elmType = t.GetElementType();
			MethodInfo mi = typeof(SerializationBuffer).GetMethod("GetArray", BindingFlags.Public | BindingFlags.Instance);
			MethodInfo method = mi.MakeGenericMethod(elmType);
			return method.Invoke(this, null);
		}


		private void AddObjectArray<T>(T[] data)
		{
			const int headerSize = 2 + sizeof(int);
			Type elmType = typeof(T);
			int count = data.Length;

			EnsureCapacity(position + headerSize);
			SetByte(position, (byte)DataTypes.dtArray);
			SetByte(position + 1, GetCollectionSubType(elmType));
			SetInt32(position + 2, count);
			position += headerSize;
			availableData += headerSize;
			if (elmType == typeof(string))
			{
				for (int i = 0; i < count; i++)
					AddString((string)(object)data[i]);
			}
			else if (elmType == typeof(decimal))
			{
				for (int i = 0; i < count; i++)
					AddDecimal((decimal)(object)data[i]);
			}
			else if (elmType == typeof(DateTime))
			{
				for (int i = 0; i < count; i++)
					AddDateTime((DateTime)(object)data[i]);
			}
			else if (elmType == typeof(TimeSpan))
			{
				for (int i = 0; i < count; i++)
					AddTimeSpan((TimeSpan)(object)data[i]);
			}
			else if (elmType == typeof(Guid))
			{
				for (int i = 0; i < count; i++)
					AddGuid((Guid)(object)data[i]);
			}
			else if (elmType.IsArray)
			{
				for (int i = 0; i < count; i++)
					AddArray((object)data[i]);
			}
			else if (typeof(IList).IsAssignableFrom(elmType))
			{
				for (int i = 0; i < count; i++)
					AddList((object)data[i]);
			}
			else if (typeof(Stream).IsAssignableFrom(elmType))
			{
				for (int i = 0; i < count; i++)
					AddStream((Stream)(object)data[i]);
			}
			else if (typeof(ICanSerialize).IsAssignableFrom(elmType) || elmType.IsClass)
			{
				for (int i = 0; i < count; i++)
					FastAddObject<T>(data[i]);
			}
			else
				throw new Exception("Cannot handle a value of type " + elmType.FullName);
		}


		private byte GetCollectionSubType(Type type)
		{
			if (type == typeof(bool)) return (byte)DataTypes.dtBool;
			if (type == typeof(byte)) return (byte)DataTypes.dtByte;
			if (type == typeof(char)) return (byte)DataTypes.dtChar;
			if (type == typeof(int)) return (byte)DataTypes.dtInt32;
			if (type == typeof(long)) return (byte)DataTypes.dtInt64;
			if (type == typeof(float)) return (byte)DataTypes.dtSingle;
			if (type == typeof(double)) return (byte)DataTypes.dtDouble;
			if (type == typeof(decimal)) return (byte)DataTypes.dtDecimal;
			if (type == typeof(string)) return (byte)DataTypes.dtString;
			if (type == typeof(DateTime)) return (byte)DataTypes.dtDateTime;
			if (type == typeof(TimeSpan)) return (byte)DataTypes.dtTimeSpan;
			if (type == typeof(Guid)) return (byte)DataTypes.dtGuid;
			if (type.IsArray) return (byte)DataTypes.dtArray;
			if (typeof(IList).IsAssignableFrom(type)) return (byte)DataTypes.dtList;
			if (typeof(Stream).IsAssignableFrom(type)) return (byte)DataTypes.dtStream;
			if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface) return (byte)DataTypes.dtObject;
			throw new Exception("Cannot handle a value of type " + type.FullName);
		}


		public T[] GetArray<T>()
		{
			const int headerSize = 2 + sizeof(int);
			if (position + 1 > availableData)
			{
				if (PeekByte(position) == (byte)DataTypes.dtEOM)
					return null;
				throw new Exception("Serialization error. Reached the end of the buffer.");
			}
			DataTypes dt = (DataTypes)PeekByte(position);
			if (dt == DataTypes.dtNull)
			{
				position++;
				return null;
			}
			else if (dt == DataTypes.dtEOM)
			{
				return null;
			}
			else if (dt == DataTypes.dtArray)
			{
				if (position + headerSize > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");

				int len;
				DataTypes subType = (DataTypes)PeekByte(position + 1);
				len = PeekInt32(position + 2);
				switch (subType)
				{
					case DataTypes.dtBool:
						return GetNativeArray<T>(new T[len], sizeof(bool));
					case DataTypes.dtByte:
						return GetNativeArray<T>(new T[len], sizeof(byte));
					case DataTypes.dtChar:
						return GetNativeArray<T>(new T[len], sizeof(char));
					case DataTypes.dtInt32:
						return GetNativeArray<T>(new T[len], sizeof(int));
					case DataTypes.dtInt64:
						return GetNativeArray<T>(new T[len], sizeof(long));
					case DataTypes.dtSingle:
						return GetNativeArray<T>(new T[len], sizeof(float));
					case DataTypes.dtDouble:
						return GetNativeArray<T>(new T[len], sizeof(double));
					default:
						return GetObjectArray<T>(subType, len);
				}
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		private T[] GetNativeArray<T>(T[] dest, int elementSize)
		{
			const int headerSize = 2 + sizeof(int);
			int requiredBytes = headerSize + dest.Length * elementSize;
			if (position + requiredBytes > availableData)
			{
				if (PeekByte(position) == (byte)DataTypes.dtEOM)
					return null;
				throw new Exception("Serialization error. Reached the end of the buffer.");
			}
			System.Buffer.BlockCopy(buffer, position + headerSize, dest, 0, dest.Length * elementSize);
			position += requiredBytes;
			return dest;
		}


		private T[] GetObjectArray<T>(DataTypes subType, int count)
		{
			const int headerSize = 2 + sizeof(int);
			position += headerSize;
			T[] array = new T[count];

			switch (subType)
			{
				case DataTypes.dtString:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetString();
					break;
				case DataTypes.dtDecimal:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetDecimal();
					break;
				case DataTypes.dtDateTime:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetDateTime();
					break;
				case DataTypes.dtTimeSpan:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetTimeSpan();
					break;
				case DataTypes.dtGuid:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetGuid();
					break;
				case DataTypes.dtArray:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetArray(typeof(T));
					break;
				case DataTypes.dtList:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetList(typeof(T));
					break;
				case DataTypes.dtStream:
					for (int i = 0; i < count; i++)
						array[i] = (T)(object)GetStream();
					break;
				case DataTypes.dtObject:
					for (int i = 0; i < count; i++)
						array[i] = FastGetObject<T>();
					break;
				default:
					throw new Exception("Cannot handle a value of type " + typeof(T).FullName);
			}
			return array;
		}


		private void AddList(object obj)
		{
			Type elmType = obj.GetType().GetGenericArguments()[0];
			MethodInfo mi = typeof(SerializationBuffer).GetMethod("AddList", BindingFlags.Public | BindingFlags.Instance);
			MethodInfo method = mi.MakeGenericMethod(elmType);
			method.Invoke(this, new object[] { obj });
		}


		private object GetList(Type t)
		{
			Type elmType = t.GetGenericArguments()[0];
			MethodInfo mi = typeof(SerializationBuffer).GetMethod("GetList", BindingFlags.Public | BindingFlags.Instance);
			MethodInfo method = mi.MakeGenericMethod(elmType);
			return method.Invoke(this, null);
		}


		public void AddList<T>(List<T> data)
		{
			if (data == null) AddNullReference();
			else
			{
				const int headerSise = 1 + sizeof(int);
				int count = data.Count;
				EnsureCapacity(position + headerSise);
				SetByte(position, (byte)DataTypes.dtList);
				SetInt32(position + 1, count);
				position += headerSise;
				availableData += headerSise;
				Type type = typeof(T);
				if (type == typeof(bool))
				{
					for (int i = 0; i < count; i++)
						AddBoolean((bool)(object)data[i]);
				}
				else if (type == typeof(byte))
				{
					for (int i = 0; i < count; i++)
						AddByte((byte)(object)data[i]);
				}
				else if (type == typeof(char))
				{
					for (int i = 0; i < count; i++)
						AddChar((char)(object)data[i]);
				}
				else if (type == typeof(int) || type.IsEnum)
				{
					for (int i = 0; i < count; i++)
						AddInt32((int)(object)data[i]);
				}
				else if (type == typeof(long))
				{
					for (int i = 0; i < count; i++)
						AddInt64((long)(object)data[i]);
				}
				else if (type == typeof(float))
				{
					for (int i = 0; i < count; i++)
						AddSingle((float)(object)data[i]);
				}
				else if (type == typeof(double))
				{
					for (int i = 0; i < count; i++)
						AddDouble((double)(object)data[i]);
				}
				else if (type == typeof(decimal))
				{
					for (int i = 0; i < count; i++)
						AddDecimal((decimal)(object)data[i]);
				}
				else if (type == typeof(string))
				{
					for (int i = 0; i < count; i++)
						AddString((string)(object)data[i]);
				}
				else if (type == typeof(DateTime))
				{
					for (int i = 0; i < count; i++)
						AddDateTime((DateTime)(object)data[i]);
				}
				else if (type == typeof(TimeSpan))
				{
					for (int i = 0; i < count; i++)
						AddTimeSpan((TimeSpan)(object)data[i]);
				}
				else if (type == typeof(Guid))
				{
					for (int i = 0; i < count; i++)
						AddGuid((Guid)(object)data[i]);
				}
				else if (type.IsArray)
				{
					for (int i = 0; i < count; i++)
						AddArray((object)data[i]);
				}
				else if (typeof(IList).IsAssignableFrom(type))
				{
					for (int i = 0; i < count; i++)
						AddList((object)data[i]);
				}
				else if (typeof(Stream).IsAssignableFrom(type))
				{
					for (int i = 0; i < count; i++)
						AddStream((Stream)(object)data[i]);
				}
				else if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface)
				{
					for (int i = 0; i < count; i++)
						FastAddObject<T>(data[i]);
				}
				else
					throw new Exception("Cannot handle a value of type " + type.FullName);
			}
		}


		public List<T> GetList<T>()
		{
			if (position + 1 > availableData)
			{
				if (PeekByte(position) == (byte)DataTypes.dtEOM)
					return null;
				throw new Exception("Serialization error. Reached the end of the buffer.");
			}
			DataTypes dt = (DataTypes)PeekByte(position);
			if (dt == DataTypes.dtNull)
			{
				position++;
				return null;
			}
			else if (dt == DataTypes.dtEOM)
			{
				return null;
			}
			else if (dt == DataTypes.dtList)
			{
				const int headerSize = 1 + sizeof(int);
				Type type = typeof(T);
				int count;
				if (position + headerSize > availableData)
					throw new Exception("Serialization error. Reached the end of the buffer.");
				count = PeekInt32(position + 1);
				if (count < 0)
					throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
				position += headerSize;
				List<T> list = new List<T>(count);
				if (type == typeof(bool))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetBoolean());
				}
				else if (type == typeof(byte))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetByte());
				}
				else if (type == typeof(char))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetChar());
				}
				else if (type == typeof(int) || type.IsEnum)
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetInt32());
				}
				else if (type == typeof(long))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetInt64());
				}
				else if (type == typeof(float))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetSingle());
				}
				else if (type == typeof(double))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetDouble());
				}
				else if (type == typeof(decimal))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetDecimal());
				}
				else if (type == typeof(string))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetString());
				}
				else if (type == typeof(DateTime))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetDateTime());
				}
				else if (type == typeof(TimeSpan))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetTimeSpan());
				}
				else if (type == typeof(Guid))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetGuid());
				}
				else if (type.IsArray)
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetArray(typeof(T)));
				}
				else if (typeof(IList).IsAssignableFrom(type))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetList(typeof(T)));
				}
				else if (typeof(Stream).IsAssignableFrom(type))
				{
					for (int i = 0; i < count; i++)
						list.Add((T)(object)GetStream());
				}
				else if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface)
				{
					for (int i = 0; i < count; i++)
						list.Add(FastGetObject<T>());
				}
				else throw new Exception("Cannot handle a value of type " + type.FullName);
				return list;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}


		private void AddObject(object obj)
		{
			Type type = obj.GetType();
			MethodInfo mi = typeof(SerializationBuffer).GetMethod("AddObject", BindingFlags.Public | BindingFlags.Instance);
			MethodInfo method = mi.MakeGenericMethod(type);
			method.Invoke(this, new object[] { obj });
		}


		public void AddObject<T>(T obj) where T : class
		{
			if (obj == null)
				AddNullReference();
			else if (obj is IList)
				AddList(obj);
			else if (obj.GetType().IsArray)
				AddArray(obj);
			else if (obj is Stream)
				AddStream(obj as Stream);
			else
			{
				FastAddObject<T>(obj);
			}
		}


		private void FastAddObject<T>(T obj)
		{
			if (obj == null)
			{
				AddNullReference();
				return;
			}
			Type t = obj.GetType();
			int startIdx = position;
			EnsureCapacity(position + 5);
			SetByte(position, (byte)DataTypes.dtObject);
			position += 5;
			availableData += 5;
			AddString(t.FullName);

			// Serialize the object values either by calling the Serialize method (if it implements ICanSerialize) or by using a dynamic serializer
			ICanSerialize serializableObject = obj as ICanSerialize;
			if (serializableObject != null)
			{
				serializableObject.Serialize(this);
			}
			else
			{
				DynamicSerializer serializer = SerializationHelper.GetDynamicSerializer(t);
				serializer(this, obj);
			}
			int objLen = position - startIdx + 1;
			SetInt32(startIdx + 1, objLen);
			EnsureCapacity(position + 1);
			SetByte(position, (byte)DataTypes.dtEOM);
			position++;
			availableData++;
		}


		public T GetObject<T>() where T : class
		{
			if (typeof(IList).IsAssignableFrom(typeof(T)) && typeof(T).IsGenericType)
				return (T)GetList(typeof(T));
			else if (typeof(T).IsArray)
				return (T)GetArray(typeof(T));
			else if (typeof(Stream).IsAssignableFrom(typeof(T)))
				return GetStream() as T;
			else
				return FastGetObject<T>();
		}


		private T FastGetObject<T>()
		{
			int len;
			int startIndex = position;

			if (position >= availableData)
				return default(T);

			DataTypes elemType = (DataTypes)buffer[position];
			if (elemType == DataTypes.dtNull)
			{
				position++;
				return default(T);
			}
			else if (elemType == DataTypes.dtEOM)
			{
				return default(T);
			}
			else if (elemType == DataTypes.dtObject)
			{
				if (position + 1 > availableData) throw new Exception("Serialization error. Reached the end of the buffer.");
				if (position + 1 + sizeof(int) > availableData) throw new Exception("Serialization error. Reached the end of the buffer.");
				len = PeekInt32(position + 1);
				if (position + len > availableData) throw new Exception("Serialization error. Reached the end of the buffer.");
				position += 5;

				var type = GetString();
				var t = typeof(T).Assembly.GetType(type);
				if (t == null)
					t = PerformExhaustiveTypeSearch(type);

				object obj = scope.GetInstance(t);
				if (obj is ICanSerialize)
					(obj as ICanSerialize).Deserialize(this);
				else
					SerializationHelper.GetDynamicDeserializer(t)(this, obj);

				position = startIndex + len;
				return (T)obj;
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type.");
		}

		private Type PerformExhaustiveTypeSearch(string type)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				var t = asm.GetType(type);
				if (t != null)
					return t;
			}
			throw new InvalidOperationException($"Cannot locate requested user data type: {type}");
		}

		public void AddStream(Stream stream)
		{
			if (stream == null) AddNullReference();
			else
			{
				if (scope == null)
					throw new InvalidOperationException("Streams are not supported in this context");

				// NOTE: Cannot read Length on some streams like DeflateStream, in which case len will be left as -1.
				// When this happens, it must be interpreted as: We should keep reading from this stream until Read returns 0 bytes.
				// Might need to update SupportsLengthProperty method to include other types of stream that do not support the Length property.
				long len = -1;
				if (stream.SupportsLengthProperty())
					len = stream.Length;

				if (len > MAX_STREAM_SIZE)
					throw new Exception($"Exceded maximum file size.");

				EnsureCapacity(position + 1);
				var streamService = scope.GetInstance<IMsgStreamService>();
				var guid = streamService.RegisterStream(stream);
				SetByte(position, (byte)DataTypes.dtStream);
				position++;
				availableData++;
				AddInt32((int)len);
				AddArray(guid.ToByteArray());
			}
		}


		public Stream GetStream()
		{
			if (position + 1 > availableData)
			{
				if (PeekByte(position) == (byte)DataTypes.dtEOM)
					return null;
				else
					throw new Exception("Serialization error. Reached the end of the buffer.");
			}
			DataTypes dt = (DataTypes)buffer[position];
			if (dt == DataTypes.dtNull)
			{
				position++;
				return null;
			}
			else if (dt == DataTypes.dtEOM)
			{
				return null;
			}
			else if (dt == DataTypes.dtStream)
			{
				if(scope == null)
					throw new InvalidOperationException("Streams are not supported in this context");

				position++;
				int len = GetInt32();
				byte[] id = GetArray<byte>();
				if (len > MAX_STREAM_SIZE)
					throw new Exception($"Exceded maximum file size.");

				return new RemoteStream(scope, new Guid(id), len);
			}
			else throw new Exception("Deserialization error. Current element cannot be converted to the specified type (Stream).");
		}


		private void AddNullReference()
		{
			EnsureCapacity(position + 1);
			buffer[position] = (byte)DataTypes.dtNull;
			position++;
			availableData++;
		}


		/// <summary>
		/// Creates a copy of the current content of the SerializationBuffer.
		/// </summary>
		/// <returns>An array of bytes that contains a copy of the data currently in the serialization buffer.</returns>
		public byte[] ToByteArray()
		{
			byte[] data = new byte[availableData];
			System.Buffer.BlockCopy(buffer, 0, data, 0, availableData);
			return data;
		}
	}


	// ======================================================================================================================
	// Dynamic Serializer/Deserializer generation
	// ======================================================================================================================

	delegate void TypeSerializer<T>(T value);
	delegate object GenericObjectCreator();
	delegate T ObjectCreator<T>();
	delegate void DynamicSerializer(SerializationBuffer buffer, object target);


	static class SerializationHelper
	{
		private static object syncObj = new object();
		private static Dictionary<ConstructorInfo, object> delegateCache = new Dictionary<ConstructorInfo, object>();
		private static Dictionary<string, DynamicSerializer[]> cachedSerializers = new Dictionary<string, DynamicSerializer[]>();

		//=============================================
		// Object instantiation using dynamic code.
		//=============================================

		public static ObjectCreator<T> GetObjectCreator<T>()
		{
			object cachedDelegate;
			Type t = typeof(T);
			ConstructorInfo constructorInfo = t.GetConstructor(Type.EmptyTypes);
			if (constructorInfo == null)
				throw new Exception("Type " + t.FullName + " requires a parameter less constructor.");

			lock (syncObj)
			{
				if (!delegateCache.TryGetValue(constructorInfo, out cachedDelegate))
				{
					ObjectCreator<T> creator = (ObjectCreator<T>)CreateDelegate(constructorInfo, typeof(ObjectCreator<T>));
					delegateCache.Add(constructorInfo, creator);
					return creator;
				}
				else
				{
					return (ObjectCreator<T>)cachedDelegate;
				}
			}
		}


		public static GenericObjectCreator GetObjectCreator(Type t)
		{
			object cachedDelegate;
			ConstructorInfo constructorInfo = t.GetConstructor(Type.EmptyTypes);
			if (constructorInfo == null)
				throw new Exception("Type " + t.FullName + " requires a parameter less constructor.");

			lock (syncObj)
			{
				if (!delegateCache.TryGetValue(constructorInfo, out cachedDelegate))
				{
					GenericObjectCreator creator = (GenericObjectCreator)CreateDelegate(constructorInfo, typeof(GenericObjectCreator));
					delegateCache.Add(constructorInfo, creator);
					return creator;
				}
				else
				{
					return (GenericObjectCreator)cachedDelegate;
				}
			}
		}


		private static Delegate CreateDelegate(ConstructorInfo constructor, Type delegateType)
		{
			string methodName = String.Format("__{0}_DynamicObjectCreator", constructor.DeclaringType.Name);
			DynamicMethod method = new DynamicMethod(methodName, constructor.DeclaringType, Type.EmptyTypes, true);
			ILGenerator gen = method.GetILGenerator();
			gen.Emit(OpCodes.Newobj, constructor);
			gen.Emit(OpCodes.Ret);
			return method.CreateDelegate(delegateType);
		}


		//=============================================
		// Object serialization using dynamic code.
		//=============================================


		/// <summary>
		/// Looks in the cache of dynamic serializers to see if we already have a serializer for the specified data type. If so, this
		/// method returns the serializers rightaway, otherwise, new serializers are created dynamically, added to the cache and then returned.
		/// </summary>
		public static DynamicSerializer GetDynamicSerializer(Type type)
		{
			DynamicSerializer[] serializers;
			string typeName = type.FullName;        // TODO: Test against nested types, I dont know if the name of a nested type (which includes the symbol +) can be used as a valid name.
			lock (syncObj)
			{
				if (!cachedSerializers.TryGetValue(typeName, out serializers))
				{
					serializers = new DynamicSerializer[2];
					serializers[0] = CreateDynamicSerializer(typeName + "_Serialize", type);
					serializers[1] = CreateDynamicDeserializer(typeName + "_Deserialize", type);
					cachedSerializers.Add(typeName, serializers);
				}
				return serializers[0];
			}
		}


		/// <summary>
		/// Looks in the cache of dynamic serializers to see if we already have a serializer for the specified data type. If so, this
		/// method returns the serializers rightaway, otherwise, new serializers are created dynamically, added to the cache and then returned.
		/// </summary>
		public static DynamicSerializer GetDynamicDeserializer(Type type)
		{
			DynamicSerializer[] serializers;
			string typeName = type.FullName;        // TODO: Test against nested types, I dont know if the name of a nested type (which includes the symbol +) can be used as a valid name.
			lock (syncObj)
			{
				if (!cachedSerializers.TryGetValue(typeName, out serializers))
				{
					serializers = new DynamicSerializer[2];
					serializers[0] = CreateDynamicSerializer(typeName + "_Serialize", type);
					serializers[1] = CreateDynamicDeserializer(typeName + "_Deserialize", type);
					cachedSerializers.Add(typeName, serializers);
				}
				return serializers[1];
			}
		}


		/// <summary>
		/// Creates a dynamic method that inserts the values of properties & fields of an object (arg1) into the specified SerializationBuffer (arg0).
		/// </summary>
		private static DynamicSerializer CreateDynamicSerializer(string methodName, Type type)
		{
			DynamicMethod serializeMethod = new DynamicMethod(methodName, typeof(void), new Type[] { typeof(SerializationBuffer), typeof(object) }, true);
			ILGenerator il = serializeMethod.GetILGenerator();

			LocalBuilder target = il.DeclareLocal(type);

			Label L01 = il.DefineLabel();
			Label L02 = il.DefineLabel();

			il.Emit(OpCodes.Ldarg_1);
			if (type.IsClass)
				il.Emit(OpCodes.Castclass, type);
			else
				il.Emit(OpCodes.Unbox_Any, type);
			il.Emit(OpCodes.Stloc, target);

			List<MemberInfo> members = GetObjectMembers(type);
			foreach (MemberInfo m in members)
			{
				if (m.MemberType == MemberTypes.Property)
				{
					PropertyInfo p = m as PropertyInfo;
					if (p.CanRead && p.CanWrite)
					{
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldloc, target);
						il.Emit(OpCodes.Callvirt, p.GetGetMethod());
						if (p.PropertyType == typeof(string))
							il.Emit(OpCodes.Ldc_I4_0); // pass argument value for useUTF as false
						il.Emit(OpCodes.Call, GetSerializationMethod(p.PropertyType));
					}
				}
				else if (m.MemberType == MemberTypes.Field)
				{
					FieldInfo f = m as FieldInfo;
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldloc, target);
					il.Emit(OpCodes.Ldfld, f);
					if (f.FieldType == typeof(string))
						il.Emit(OpCodes.Ldc_I4_0); // pass argument value for useUTF as false
					il.Emit(OpCodes.Call, GetSerializationMethod(f.FieldType));
				}
			}
			il.Emit(OpCodes.Ret);
			return (DynamicSerializer)serializeMethod.CreateDelegate(typeof(DynamicSerializer));
		}


		/// <summary>
		/// Creates a dynamic method that extracts values from a SerializationBuffer (arg0) and stores the values in the properties/fields of the specified object (arg1).
		/// </summary>
		private static DynamicSerializer CreateDynamicDeserializer(string methodName, Type type)
		{
			DynamicMethod deserializeMethod = new DynamicMethod(methodName, typeof(void), new Type[] { typeof(SerializationBuffer), typeof(object) }, true);
			MethodInfo typeofMethod = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
			ILGenerator il = deserializeMethod.GetILGenerator();
			Label methodEnd = il.DefineLabel();

			LocalBuilder loc0 = il.DeclareLocal(type);
			il.Emit(OpCodes.Ldarg_1);
			if (type.IsClass)
				il.Emit(OpCodes.Castclass, type);
			else
				il.Emit(OpCodes.Unbox_Any, type);
			il.Emit(OpCodes.Stloc, loc0);

			List<MemberInfo> members = GetObjectMembers(type);
			foreach (MemberInfo m in members)
			{
				if (m.MemberType == MemberTypes.Property)
				{
					PropertyInfo p = m as PropertyInfo;
					if (p.CanRead && p.CanWrite)
					{
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Callvirt, EOMGetter);
						il.Emit(OpCodes.Brtrue, methodEnd);
						if (p.PropertyType.IsEnum)
						{
							il.Emit(OpCodes.Ldloc, loc0);
							il.Emit(OpCodes.Ldarg_0);
							il.Emit(OpCodes.Ldtoken, p.PropertyType);
							il.Emit(OpCodes.Call, typeofMethod);
							il.Emit(OpCodes.Callvirt, GetDeserializationMethod(p.PropertyType));
							il.Emit(OpCodes.Unbox_Any, p.PropertyType);
							il.Emit(OpCodes.Callvirt, p.GetSetMethod());
						}
						else
						{
							il.Emit(OpCodes.Ldloc, loc0);
							il.Emit(OpCodes.Ldarg_0);
							il.Emit(OpCodes.Call, GetDeserializationMethod(p.PropertyType));
							il.Emit(OpCodes.Callvirt, p.GetSetMethod());
						}
					}
				}
				else if (m.MemberType == MemberTypes.Field)
				{
					FieldInfo f = m as FieldInfo;
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Callvirt, EOMGetter);
					il.Emit(OpCodes.Brtrue, methodEnd);
					if (f.FieldType.IsEnum)
					{
						il.Emit(OpCodes.Ldloc, loc0);
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Ldtoken, f.FieldType);
						il.Emit(OpCodes.Call, typeofMethod);
						il.Emit(OpCodes.Callvirt, GetDeserializationMethod(f.FieldType));
						il.Emit(OpCodes.Unbox_Any, f.FieldType);
						il.Emit(OpCodes.Stfld, f);
					}
					else
					{
						il.Emit(OpCodes.Ldloc, loc0);
						il.Emit(OpCodes.Ldarg_0);
						il.Emit(OpCodes.Call, GetDeserializationMethod(f.FieldType));
						il.Emit(OpCodes.Stfld, f);
					}
				}
			}
			il.MarkLabel(methodEnd);
			il.Emit(OpCodes.Ret);
			return (DynamicSerializer)deserializeMethod.CreateDelegate(typeof(DynamicSerializer));
		}


		private static List<MemberInfo> GetObjectMembers(Type type)
		{
			List<MemberInfo> list = new List<MemberInfo>();
			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				list.AddRange(GetObjectMembers(type.BaseType));
			}
			MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			foreach (MemberInfo m in members)
			{
				if (m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
					list.Add(m);
			}
			return list;
		}


		private static MethodInfo EOMGetter = typeof(SerializationBuffer).GetProperty("EOM").GetGetMethod();


		private static void PushDefaultValueToStack(ILGenerator il, Type t)
		{
			if (t == typeof(bool) || t == typeof(byte) || t == typeof(char) || t == typeof(byte) || t == typeof(int))
			{
				il.Emit(OpCodes.Ldc_I4_0);
			}
			else if (t == typeof(long))
			{
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Conv_I8);
			}
			else if (t == typeof(float))
			{
				il.Emit(OpCodes.Ldc_R4, 0.0f);
			}
			else if (t == typeof(double))
			{
				il.Emit(OpCodes.Ldc_R8, 0.0d);
			}
			else if (t == typeof(DateTime))
			{
				il.Emit(OpCodes.Ldsfld, typeof(DateTime).GetProperty("Today", BindingFlags.Public | BindingFlags.Static).GetGetMethod());
			}
			else if (t == typeof(TimeSpan))
			{
				il.Emit(OpCodes.Ldsfld, typeof(TimeSpan).GetField("Zero", BindingFlags.Public | BindingFlags.Static));
			}
			else if (t == typeof(Guid))
			{
				il.Emit(OpCodes.Ldsfld, typeof(Guid).GetField("Empty", BindingFlags.Public | BindingFlags.Static));
			}
			else
			{
				il.Emit(OpCodes.Ldnull);
			}
		}

		class SampleObj
		{
			public string Name;
			public string Description;
			public BindingFlags Flags;

			public void CallSample(int a, string b, double c)
			{
				Console.WriteLine("ble");
			}
		}


		/// <summary>
		/// Returns the method that needs to be invoked to insert a value in the SerializationBuffer based on the specified data type.
		/// </summary>
		private static MethodInfo GetSerializationMethod(Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			Type streamType = typeof(SerializationBuffer);

			if (type == typeof(bool))
				return streamType.GetMethod("AddBoolean", flags);
			if (type == typeof(char))
				return streamType.GetMethod("AddChar", flags);
			if (type == typeof(byte))
				return streamType.GetMethod("AddByte", flags);
			if (type == typeof(int) || type.IsEnum)
				return streamType.GetMethod("AddInt32", flags);
			if (type == typeof(long))
				return streamType.GetMethod("AddInt64", flags);
			if (type == typeof(float))
				return streamType.GetMethod("AddSingle", flags);
			if (type == typeof(double))
				return streamType.GetMethod("AddDouble", flags);
			if (type == typeof(decimal))
				return streamType.GetMethod("AddDecimal", flags);
			if (type == typeof(DateTime))
				return streamType.GetMethod("AddDateTime", flags);
			if (type == typeof(TimeSpan))
				return streamType.GetMethod("AddTimeSpan", flags);
			if (type == typeof(Guid))
				return streamType.GetMethod("AddGuid", flags);
			if (type == typeof(string))
				return streamType.GetMethod("AddString", flags);
			if (type.IsArray)
			{
				MethodInfo addArray = streamType.GetMethod("AddArray", flags);
				return addArray.MakeGenericMethod(type.GetElementType());
			}
			if (typeof(Stream).IsAssignableFrom(type))
			{
				return streamType.GetMethod("AddStream", flags);
			}
			if (typeof(IList).IsAssignableFrom(type))
			{
				MethodInfo addList = streamType.GetMethod("AddList", flags);
				return addList.MakeGenericMethod(type.GetGenericArguments()[0]);
			}
			if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface)
			{
				MethodInfo addObject = streamType.GetMethod("AddObject", flags);
				return addObject.MakeGenericMethod(type);
			}
			throw new Exception("Cannot handle a value of type " + type.FullName);
		}


		/// <summary>
		/// Returns the method that needs to be invoked to extract a value from the SerializationBuffer based on the specified data type.
		/// </summary>
		private static MethodInfo GetDeserializationMethod(Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			Type streamType = typeof(SerializationBuffer);

			if (type == typeof(bool))
				return streamType.GetMethod("GetBoolean", flags);
			if (type == typeof(char))
				return streamType.GetMethod("GetChar", flags);
			if (type == typeof(byte))
				return streamType.GetMethod("GetByte", flags);
			if (type == typeof(int))
				return streamType.GetMethod("GetInt32", flags);
			if (type.IsEnum)
				return streamType.GetMethod("GetEnum", flags);
			if (type == typeof(long))
				return streamType.GetMethod("GetInt64", flags);
			if (type == typeof(float))
				return streamType.GetMethod("GetSingle", flags);
			if (type == typeof(double))
				return streamType.GetMethod("GetDouble", flags);
			if (type == typeof(decimal))
				return streamType.GetMethod("GetDecimal", flags);
			if (type == typeof(DateTime))
				return streamType.GetMethod("GetDateTime", flags);
			if (type == typeof(TimeSpan))
				return streamType.GetMethod("GetTimeSpan", flags);
			if (type == typeof(Guid))
				return streamType.GetMethod("GetGuid", flags);
			if (type == typeof(string))
				return streamType.GetMethod("GetString", flags);
			if (type.IsArray)
			{
				MethodInfo addArray = streamType.GetMethod("GetArray", flags);
				return addArray.MakeGenericMethod(type.GetElementType());
			}
			if (typeof(Stream).IsAssignableFrom(type))
			{
				return streamType.GetMethod("GetStream", flags);
			}
			if (typeof(IList).IsAssignableFrom(type))
			{
				MethodInfo addList = streamType.GetMethod("GetList", flags);
				return addList.MakeGenericMethod(type.GetGenericArguments()[0]);
			}
			if (typeof(ICanSerialize).IsAssignableFrom(type) || type.IsClass || type.IsInterface)
			{
				MethodInfo addObject = streamType.GetMethod("GetObject", flags);
				return addObject.MakeGenericMethod(type);
			}
			throw new Exception("Cannot handle a value of type " + type.FullName);
		}


		// ==================================== Serializer/Deserializer TEMPLATE CODE ====================================
		// I use these bits and ILDASM to get the IL code necesary for CreateDynamicSerializer / CreateDynamicDeserializer

		private static void AAAA_SerialObjTemplateCode(SerializationBuffer buffer, object obj)
		{
			SampleObj target = (SampleObj)obj;
			buffer.AddString(target.Name);
			buffer.AddString(target.Description);
			buffer.AddInt32((int)target.Flags);
		}

		private static void AAAA_DeserialObjTemplateCode(SerializationBuffer buffer, object obj)
		{
			SampleObj target = (SampleObj)obj;

			if (buffer.EOM) return;
			target.Name = buffer.GetString();

			if (buffer.EOM) return;
			target.Description = buffer.GetString();

			if (buffer.EOM) return;
			target.Flags = (BindingFlags)buffer.GetEnum(typeof(BindingFlags));

			// This will help me determine how arguments are arranged in the stack cause im lost at it  :S
			target.CallSample(10, "hello", 1.01);
		}
	}
}
