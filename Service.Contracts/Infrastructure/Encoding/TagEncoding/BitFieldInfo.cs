using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Service.Contracts
{
	/// <summary>
	/// Defines how to interpret the content of a field.
	/// </summary>
	public enum BitFieldFormat
	{
		Decimal,		// The Value property of the field will be interpreted as a decimal number. Example: "923020", the bitLenght of the field must be in the range [1, 64].
		Hexadecimal,	// The Value property of the field will be interpreted as an hexadecimal string. Example: "A0034F0", the bitLenght of the field must be a multiple of 4
		EANUCC128,		// The value property of the field is interpreted as an EANUCC128 code (7 bits per character), the bitLenght of the field must be a multiple of 7
		ACSII,			// The value property of the field is interpreted as ASCII (8 bits per character), the bitLenght of the field must be a multiple of 8
		UTF8			// The value property of the field is interpreted as UTF-8 (8 bits per character), the bitLenght of the field must be a multiple of 8
	}


	/// <summary>
	/// Delegate used to notify when the value of a field changes.
	/// </summary>
	public delegate void BitFieldValueEvent(BitFieldInfo sender, byte[] value);


	// =====================================================================
	// BitFieldInfo
	// =====================================================================

	/// <summary>
	/// Contains information about a field that forms part of an encoding.
	/// </summary>
	/// <remarks>
	///		- Name is the name of the field.
	///		
	///		- The StartBit property determines from which bit within the binary
	///		  code a field starts.
	///		  
	///		- The BitLength property in the other hand indicated how many bits of
	///		  data are used to encode the value of the field.
	///		  
	///		- The Format property indicates how the value of the field should be
	///		  interpreted and represented as a string. 
	///		  
	///		- AddLeadingZeros indicates if the value property should add zeros to
	///		  the left when the value of the field is smaller than the maximum
	///		  number of decimal numers possible based of the bitLength. This is used
	///		  only if the field format is "Decimal".
	///		  
	///		- IsFixed indicates if the field has a fixed value or not.
	///		
	///		- The value property returns a string that depends on the field format.
	///		  In all cases, the value of a field is nothing more than a sequence of
	///		  bits (binary data). However, how that binary data is translated to and
	///		  from a string is controlled by the Format property.
	/// </remarks>
	public class BitFieldInfo
	{
		private byte[] data;		// A reference to the raw binary data of the field.
		private string name;		// Name of the field
		private int startBit;		// Bit where the field starts (0-N)
		private int bitLength;		// Length of the field in bits (1-N)
		private BitFieldFormat format; // Tells how the binary data should be interpreted and represented as string.
		private bool addLeadingZeros;	// Flag indicating if field should be padded with zeros to the left. NOTE: Has effect only if Format is Decimal.
		private bool isFixed;		// Indicates if the value of the field is set by default to an specific value.
		private string value;       // Contains the value of the field after extracting the field bits from the data array and applying formatting
		private bool valueDecoded;	// Indicates if the value is up to date.

		private BitFieldValueEvent ValueChanged;


		public BitFieldInfo(string name, int startBit, int bitLength, BitFieldFormat format, bool addLeadingZeros, bool isFixed, string defaultValue, BitFieldValueEvent valueChanged)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));
			if(startBit < 0)
				throw new ArgumentOutOfRangeException(nameof(startBit));
			if(bitLength < 0 || bitLength > 256)
				throw new ArgumentOutOfRangeException(nameof(bitLength));
			if (isFixed && (format != BitFieldFormat.Decimal && format != BitFieldFormat.Hexadecimal))
				throw new InvalidOperationException("Fixed fields can only have Decimal or Hexadecimal formats.");
			this.name = name;
			this.startBit = startBit;
			this.bitLength = bitLength;
			this.format = format;
			this.addLeadingZeros = addLeadingZeros;
			if(!String.IsNullOrWhiteSpace(defaultValue))
				Value = defaultValue;
			this.isFixed = isFixed;
			this.ValueChanged = valueChanged;
		}

		/// <summary>
		/// Gets the name of the field
		/// </summary>
		public string Name { get { return name; } }

		/// <summary>
		/// Gets or sets the start bit of the field
		/// </summary>
		public int StartBit
		{
			get { return startBit; }
			set
			{
				startBit = value;
				valueDecoded = false;
			}
		}

		/// <summary>
		/// Gets or sets the bit length of the field
		/// </summary>
		public int BitLength
		{
			get { return bitLength; }
			set
			{
				bitLength = value;
				valueDecoded = false;
			}
		}

		/// <summary>
		/// Gets or sets the format of the field
		/// </summary>
		public BitFieldFormat Format
		{
			get { return format; }
			set
			{
				format = value;
				valueDecoded = false;
			}
		}

		/// <summary>
		/// Gets a flag indicating if the field has a fixed value.
		/// </summary>
		public bool IsFixed { get { return isFixed; } }

		/// <summary>
		/// Gets the raw binary data of the field.
		/// </summary>
		public byte[] GetBytes()
		{
			return data;
		}

		/// <summary>
		/// Sets the raw binary data of the field.
		/// </summary>
		public void SetBytes(byte[] data)
		{
			this.data = data;
			valueDecoded = false;
			ValueChanged?.Invoke(this, data);
		}

		/// <summary>
		/// Gets or sets the field value
		/// </summary>
		public string Value
		{
			get
			{
				if (!valueDecoded)
				{
					value = Decode();
					valueDecoded = true;
				}
				return value;
			}
			set
			{
				Encode(value);
				valueDecoded = false;
			}
		}

		private void Encode(string value)
		{
			switch (format)
			{
				case BitFieldFormat.Decimal:
					if (!xtConvert.IsNumber(value))
						throw new InvalidOperationException($"Value for field {name} is in an invalid format, was expecting a number.");
					long l = Convert.ToInt64(value);
					data = xtConvert.Int64ToByteArray(l);
					break;
				case BitFieldFormat.Hexadecimal:
					if (!xtConvert.IsHexadecimal(value))
						throw new InvalidOperationException($"Value for field {name} is in an invalid format, was expecting an hexadecimal string.");
					data = xtConvert.HexNumberToByteArray(value);
					break;
				case BitFieldFormat.EANUCC128:
					if (!EANUCC128Encoding.IsEANUCC128(value))
						throw new InvalidOperationException($"Value for field {name} is in an invalid format, was expecting an EANUCC128 string.");
					data = EANUCC128Encoding.Encode(value);
					break;
				case BitFieldFormat.ACSII:
					data = Encoding.ASCII.GetBytes(value);
					break;
				case BitFieldFormat.UTF8:
					data = Encoding.UTF8.GetBytes(value);
					break;
				default:
					throw new InvalidOperationException($"Field {name} has a format {format} that is not implemented.");
			}
			ValueChanged?.Invoke(this, data);
		}

		private string Decode()
		{
			if (bitLength == 0)
				return "";
			if (data == null || data.Length == 0)
				throw new InvalidOperationException($"Field {name} has not been assigned a value.");
			switch (format)
			{
				case BitFieldFormat.Decimal:
					if (data.Length <= 8)
					{
						var v = xtConvert.ByteArrayToInt64(data, 0).ToString();
						if (addLeadingZeros)
						{
							var digits = TagEncoding.NumDigits(bitLength);
							if (v.Length < digits)
								return new String('0', digits - v.Length) + v;
						}
						return v;
					}
					else throw new InvalidOperationException($"The size of field {name} is not supported.");
				case BitFieldFormat.Hexadecimal:
					var hex = xtConvert.ByteArrayToHexNumberWithBitLength(data, bitLength);
					if (addLeadingZeros && hex.Length < bitLength / 4)
						return new String('0', bitLength / 4 - hex.Length) + hex;
					else
						return hex;
				case BitFieldFormat.EANUCC128:
					return EANUCC128Encoding.Decode(data);
				case BitFieldFormat.ACSII:
					return Encoding.ASCII.GetString(data);
				case BitFieldFormat.UTF8:
					return Encoding.UTF8.GetString(data);
				default:
					throw new InvalidOperationException($"Field {name} has a format {format} that is not implemented.");
			}
		}

		public override string ToString()
		{
			return Value;
		}
	}
}
