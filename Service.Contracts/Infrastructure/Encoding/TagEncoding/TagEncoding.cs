using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Linq;

namespace Service.Contracts
{
	// =====================================================================
	// TagEncoding.
	// =====================================================================
	/* 
	 * Base class used by all the encoding types supported by the system.
	 * 
	 * This class includes many operations and validations necesary to 
	 * identify and split the components of a code provided in binary format
	 * (byte array) or as a string in hexadecimal, decimal or alphanumeric
	 * format. Or in URN format as specified by the EPC standard.
	 * 
	 * It is expected that derived classes only have to provide a minimum
	 * of code to work correctly. Derived classes must adhere to the following
	 * guide lines:
	 * 
	 * - Must provide a public constructor with no parameters. This constructor
	 *   must initialize the internal fields array (of type BitFieldInfo). This
	 *   array contains all the information required to describe the way in which
	 *   a code is assembled (its composing fields).
	 * 
	 * - Override the TypeID property, this property must return an integer.
	 *   Care must be taken to ensure that this number is unique and that
	 *   different encodings DO NOT return the same value. Additionally once
	 *   assigned, this value should never be changed. See table 1 for a list 
	 *   of the IDs assigned to all supported encodings.
	 * 
	 * - Override the Name property, it will simply return the name of the standard
	 *   in a user friendly way. For instance: "GID-96".
	 * 
	 * - The urn format is meant to display the fields that compose a code in a
	 *   more readable string. Each derived class must override the UrnNameSpace
	 *   property. UrnNameSpace MUST return a string describing the way in which
	 *   a URN for the specified encoding must be assembled. The system will 
	 *   automatically replace field names found in the URN by their actual values
	 *   when the URN format is requested. For instance, in the case of GID, the
	 *   UrnNameSpace would be "urn:epc:gid-96:GMN.ClassID.Serial". 
	 *   
	 * - Override UrnPattern property. This property must return a string with
	 *   a regular expression which will be used to validate a code provided
	 *   in URN format. The regular expression must explicitly capture all
	 *   components of the code in groups.
	 * 
	 * - Additionally, derived classes can override any of the following:
	 *		> Indexers
	 *		> InitializeFromByteArray
	 *		> InitializeFromUrn
	 *		> InitializeFields
	 *		
	 *	 This is only advised if the properties in the fields that compose the 
	 *	 code are variable (determined from values within the code itself),
	 *	 or when there might be any special considerations for Urns. For instance,
	 *	 in the case of many encodings, the value of the partition field
	 *	 afects the length of the Company and ItemReference fields. This makes 
	 *	 necesary to evaluate part of the code, before it is possible to determine
	 *	 its full structure.
	 *	 
	 * 
  	 * Table 1.
	 * ============================================================================
	 * Name and TypeID of the different encoding standards supported by the system:
	 * 
	 * Name				TypeID
	 * ---------------- ---------------
	 * Gid-96			1	
	 * Sgtin-96			2	
	 * Sscc-96			3	
	 * Sgln-96			4	
	 * Grai-96			5	
	 * Giai-96			6	
	 * DoD-96			7	
	 * Sgtin-198		8	
	 * Sgln-195			9	
	 * Grai-170			10	
	 * Giai-202			11	
	 * Tempe128			12
     * Mayoral96        13
     * Inditex128       14
     * InditexV2128     15
	 * UnknownEncoding	999
	 */

	public interface ITagEncoding
	{
		int TypeID { get; }
		string Name { get; }
		string UrnNameSpace { get; }
		bool InitializeFromByteArray(byte[] code);
		bool InitializeFromHexString(string hex);
		bool InitializeFromUrn(string urn);
		int BitLength { get; }
		int FieldCount { get; }
		BitFieldInfo this[int idx] { get; }
		BitFieldInfo this[string fieldName] { get; }
		bool ContainsField(string fieldName);
		byte[] GetBytes();
		string GetHexadecimal();
		string GetUrnFormat();
	}


	public abstract class TagEncoding : ITagEncoding
	{
		/// <summary>
		/// Array containing the specifications of each field that composes the code.
		/// Must be initialized in the constructor of each derived class.
		/// </summary>
		protected BitFieldInfo[] fields;

		/// <summary>
		/// ID assigned to the encoding type. See Table 1.
		/// </summary>
		public abstract int TypeID { get; }

		/// <summary>
		/// Name of the encoding standard or specification
		/// </summary>
		public abstract string Name { get; }
		
		/// <summary>
		/// NameSpace used to conform the URN.
		/// </summary>
		public abstract string UrnNameSpace { get; }

		/// <summary>
		/// Regular expression to validate and parse the code when provided in URN format.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: If this expression is not conformant to the requirements of the system,
		/// then InitializeFromURN will not work correctly.
		/// </remarks>
		protected abstract string UrnPattern { get; }

		/// <summary>
		/// Initializes this code from its binary representation.
		/// </summary>
		public virtual bool InitializeFromByteArray(byte[] code)
		{
			int len = code.Length;
			// The API only supports encodings of up to 256 bits.
			if (len < 1 || len > 32)
				throw new InvalidOperationException("Invalid data supplied");
			if (BitLength / 8 != code.Length)
				return false;
			bool sw = true;
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].IsFixed)
				{
					int fixedValue;
					int headerValue = xtConvert.MakeInt32(code, fields[i].StartBit, fields[i].BitLength);
					if (fields[i].Format == BitFieldFormat.Decimal)
						fixedValue = Convert.ToInt32(fields[i].Value);
					else if (fields[i].Format == BitFieldFormat.Hexadecimal)
					{
						string hex = fields[i].Value;
						fixedValue = Convert.ToInt32(hex, 16);
					}
					else throw new InvalidOperationException("Invalid fixed field format.");
					sw = sw && (headerValue == fixedValue);
				}
			}
			if (!sw) return false;
			sw = InitializeFields(code);
			return sw;
		}

		/// <summary>
		/// Initializes this code from its hexadecimal representation.
		/// </summary>
		public bool InitializeFromHexString(string hex)
		{
			byte[] rawData = xtConvert.HexNumberToByteArray(hex);
			return InitializeFromByteArray(rawData);
		}

		/// <summary>
		/// Initializes this code from its URN representatrion.
		/// </summary>
		public bool InitializeFromUrn(string urn)
		{
			return ParseUrn(urn);
		}

		/// <summary>
		/// Gets the length of the code in bits.
		/// </summary>
		public int BitLength
		{
			get
			{
				int bitLen = 0;
				for (int i = fields.Length; --i >= 0; )
					bitLen += fields[i].BitLength;
				return bitLen;
			}
		}

		/// <summary>
		/// Allows to determine how many fields compose this code.
		/// </summary>
		public int FieldCount
		{
			get { return fields.Length; }
		}

		/// <summary>
		/// Provides access to each of the fields that compose this code by index.
		/// </summary>
		public virtual BitFieldInfo this[int idx]
		{
			get { return fields[idx]; }
		}

		/// <summary>
		/// Provides access to each of the fields that compose this code by name.
		/// </summary>
		public virtual BitFieldInfo this[string fieldName]
		{
			get
			{
				var field = fields.FirstOrDefault(p => p.Name == fieldName);
				if(field == null)
					throw new ArgumentException("Field " + fieldName + " is not in the collection.");
				return field;
			}
		}

		/// <summary>
		/// Determines if the tag encoding has the specified field.
		/// </summary>
		public bool ContainsField(string fieldName)
		{
			var field = fields.FirstOrDefault(p => p.Name == fieldName);
			return (field != null);
		}

		/// <summary>
		/// Retrieves the binary representation of the code.
		/// </summary>
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

		/// <summary>
		/// Retrieves the hexadecimal representation of the code.
		/// </summary>
		public string GetHexadecimal()
		{
			byte[] byteForm = GetBytes();
			return xtConvert.ByteArrayToHexNumber(byteForm);
		}

		/// <summary>
		/// Retrieves the URN representation of the code.
		/// </summary>
		public string GetUrnFormat()
		{
			string s = UrnNameSpace;
			for (int i = 0; i < fields.Length; i++)
				s = s.Replace(fields[i].Name, fields[i].Value);
			return s;
		}

		/// <summary>
		/// Extracts the value of each field from the provided binary data.
		/// </summary>
		protected virtual bool InitializeFields(byte[] code)
		{
			if (code.Length * 8 < BitLength)
				return false;
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].BitLength != 0)
				{
					fields[i].SetBytes(xtConvert.ExtractBits(code, fields[i].StartBit, fields[i].BitLength));
				}
			}
			return true;
		}


		/// <summary>
		/// Parses a string using the URN pattern. Then assigns the values of each of the fields from the URN. In case of error returns false.
		/// </summary>
		protected bool ParseUrn(string urn)
		{
			int idx = 0;
			string val;
			Regex re = new Regex(UrnPattern, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Match m = re.Match(urn);
			if (!m.Success) return false;
			CaptureCollection cc = m.Groups["component"].Captures;
			for (int i = 0; i < fields.Length; i++)
			{
				if (!fields[i].IsFixed)
				{
					if (idx < cc.Count)
					{
						val = cc[idx++].Value.Replace(" ", "");
						fields[i].Value = val;
					}
					else return false;
				}
			}
			return true;
		}


		/// <summary>
		/// Return the number of decimal digits that can be used to represent a field based of its bit length.
		/// For instance a 40 bit field, would be able to store up to 13 decimal digits.
		/// </summary>
		public static int NumDigits(int bitlen)
		{
			return ((int)Math.Pow(2, bitlen)).ToString().Length-1;
		}
	}


	/// <summary>
	/// Many tag encoding schemes defined by GS1 include a field called "Filter", usually the value of this field cannot be determined from the barcode,
	/// which is the way in which other fields are determined in the Standard Tag Encoding algorithm. Therefore, it is necesary to provide the value
	/// of this field from configuration.
	/// </summary>
	public class GS1TagConfig
	{
		public string Filter;
	}
}


