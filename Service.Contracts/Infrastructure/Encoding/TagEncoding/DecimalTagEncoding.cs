using Service.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * Specification
 * ==========================
 * 
 * The SGTIN-96 encoding  (Serialized Global Trade Item Number) is used when 
 * the company is already registered with the EAN.UCC and whishes to keep
 * using their GTIN bar codes in their RFID tags. 
 * 
 * The structure of the SGTIN-96 code is as follows:
 * 
 * | Header | Filter | Partition | CompanyPrefix | ItemRef | #Serial |
 * |--------|--------|-----------|---------------|---------|---------|
 * | 8 bits | 3 bits |   3 bits  |  20-40 bits   |24-4 bits| 38 bits |
 * 
 * 
 * Where:
 * 
 *	- Header is a fixed value (always equal to 48)
 *  - Filter must be a value between 0 and 7.
 *  - Partition controls the size of the CompanyPrefix and ItemReference fields.
 *  - CompanyPrefix is the prefix assigned to the company by the EAN.UCC (must be provided to us by the company).
 *	- ItemReference is the number assigned by the company to the product, and
 *	- SerialNumber is a "unique" identifier assigned by the company to be able to track each item individually (it is
 *	  assumed that each item will have its own unique serial number). How and when this serial number is initialized
 *	  and incremented depends on the company policies. For instance, each ItemReference code can have its own independent
 *	  serial sequence, or to simplify things, the company might decide to keep a single sequence of serials that is used
 *	  across all products regardless of the ItemReferece.
 *	  
 *	  
 * URNs format
 * ==========================
 * URN format for this standard is defined below:
 * 
 *		"urn:epc:sgtin-96:Filter.CompanyPrefix.ItemReference.SerialNumber"
 *
 * 
 */

namespace Service.Contracts
{
	public class DecimalTagEncoding : ITagEncoding, IConfigurable<DecimalTagEncodingConfig>
	{
		private DecimalTagEncodingConfig config;
		private BitFieldInfo[] fields;

		public DecimalTagEncoding()
		{
			config = new DecimalTagEncodingConfig();
			fields = new BitFieldInfo[] {
				new BitFieldInfo("Prefix",        84, 12, BitFieldFormat.Hexadecimal, false, true, "101", null),
				new BitFieldInfo("ItemReference", 32, 52, BitFieldFormat.Hexadecimal, false, false, null, null),
				new BitFieldInfo("Suffix",        32, 0,  BitFieldFormat.Hexadecimal, false, true,  "",   null),
				new BitFieldInfo("SerialNumber",  0,  32, BitFieldFormat.Hexadecimal, true,  false, null, null)
			};
		}


		public int TypeID
		{
			get { return 2; }
		}

		public string Name
		{
			get { return "DecimalEncoding"; }
		}

		public string UrnNameSpace
		{
			get { return "urn:epc:tag:decimal:Prefix.ItemReference.Suffix.SerialNumber"; }
		}

		protected string UrnPattern
		{
			get { return @"^urn:epc:tag:decimal:((?<component>\[\d+-\d+\]|\d+|\*)(\.|$)){4}"; }
		}


		public bool InitializeFromByteArray(byte[] code)
		{
			if(code.Length != 12)
				return false;

			bool sw = true;
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].IsFixed)
				{
					string fieldValue;
					if (fields[i].BitLength == 0)
						fieldValue = "";
					else
						fieldValue = xtConvert.MakeInt32(code, fields[i].StartBit, fields[i].BitLength).ToString("X2");
					string fixedValue = fields[i].Value;
					sw = sw && (fieldValue == fixedValue);
				}
			}
			if (!sw) return false;
			sw = InitializeFields(code);
			return sw;
		}


		private bool InitializeFields(byte[] code)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].BitLength != 0 && !fields[i].IsFixed)
				{
					fields[i].SetBytes(xtConvert.ExtractBits(code, fields[i].StartBit, fields[i].BitLength));
				}
			}
			return true;
		}


		public bool InitializeFromHexString(string hex)
		{
			byte[] rawData = xtConvert.HexNumberToByteArray(hex);
			return InitializeFromByteArray(rawData);
		}

		public bool InitializeFromUrn(string urn)
		{
			return false;  // Since this encoding algorithm is not standard, we cannot initialize from an EPC read
		}

		public int BitLength
		{
			get => 96;
		}

		public int FieldCount
		{
			get => 4;
		}

		/// <summary>
		/// Provides access to each of the fields that compose this code by index.
		/// </summary>
		public virtual BitFieldInfo this[int idx]
		{
			get { return fields[idx]; }
		}

		public virtual BitFieldInfo this[string fieldName]
		{
			get
			{
				var field = fields.FirstOrDefault(p => p.Name == fieldName);
				if (field == null)
					throw new ArgumentException("Field " + fieldName + " is not in the collection.");
				return field;
			}
		}

		public bool ContainsField(string fieldName)
		{
			var field = fields.FirstOrDefault(p => p.Name == fieldName);
			return (field != null);
		}


		public byte[] GetBytes()
		{
			BitArray bits = new BitArray(BitLength, false);
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].BitLength != 0)
					xtConvert.SetBits(bits, fields[i].StartBit, fields[i].BitLength, fields[i].GetBytes());
			}
			byte[] byteForm = new byte[BitLength / 8];
			bits.CopyTo(byteForm, 0);
			return byteForm;
		}


		public string GetHexadecimal()
		{
			byte[] byteForm = GetBytes();
			return xtConvert.ByteArrayToHexNumber(byteForm);
		}


		public string GetUrnFormat()
		{
			string s = UrnNameSpace;
			for (int i = 0; i < fields.Length; i++)
				s = s.Replace(fields[i].Name, fields[i].Value);
			return s;
		}


		public DecimalTagEncodingConfig GetConfiguration()
		{
			return config;
		}


		public void SetConfiguration(DecimalTagEncodingConfig config)
		{
			this.config = config;

			if(config.SerialBitLenght < 24)
				throw new Exception("Invalid configuration detected: Serial bit length cannot be less than 24 bits");

			if (config.SerialBitLenght % 4 != 0)
				throw new Exception("Invalid configuration detected: Serial bit length MUST be a multiple of 4");

			if (String.IsNullOrWhiteSpace(config.Prefix))
			{
				fields[0].BitLength = 0;
			}
			else
			{
				fields[0].BitLength = config.Prefix.Length * 4;  // for bits per digit
				fields[0].Value = config.Prefix;
			}

			if (String.IsNullOrWhiteSpace(config.Suffix))
			{
				fields[2].BitLength = 0;
			}
			else
			{
				fields[2].BitLength = config.Suffix.Length * 4;  // for bits per digit
				fields[2].Value = config.Suffix;
			}

			fields[3].BitLength = config.SerialBitLenght;

			var barcodeBitlen = 96 - fields[0].BitLength - fields[2].BitLength - fields[3].BitLength;
			if (barcodeBitlen < 50)
				throw new Exception("Invalid configuration detected: Barcode bit length cannot be less than 50 bits");

			fields[1].BitLength = barcodeBitlen;
		}


		public override string ToString()
		{
			return $"urn:epc:tag:decimal:{this["Prefix"]}.{this["ItemReference"]}.{this["Suffix"]}.{this["SerialNumber"]}";
		}
	}


	public class DecimalTagEncodingConfig
	{
		public string BarcodeField;               // Specifies the name of the field that stores the barcode.
		public string Prefix;                        // Specifies a decimal value to be added in front of the barcode.
		public string Suffix;                         // Specifies a decimal value to be added after the barcode.
		public int SerialBitLenght = 32;		  // Specifies how many bits will be used for serial number.
	}
}
