using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
	public class StandardEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<StandardEncodingConfig>
	{
		private StandardEncodingConfig config;

		public StandardEncodingAlgorithm(IFactory factory)
		{
			config = new StandardEncodingConfig();
			config.TagEncoding = new Sgtin96();
			config.Barcode = new EAN13();
			config.Sequence = new SingleSerialSequence(factory.GetInstance<ISerialRepository>());
			config.AccessPasswordMethod = new FixedPassword();
			config.KillPasswordMethod = new FixedPassword();
			config.WriteLocks = true;
			config.EpcMemoryLock = RFIDLockType.Lock;
			config.UserMemoryLock = RFIDLockType.Lock;
			config.AccessPasswordLock = RFIDLockType.Lock;
			config.KillPasswordLock = RFIDLockType.Lock;
			config.UserMemory = new UserMemoryFixed();
		}


		public ISerialSequence Sequence
		{
			get { return config.Sequence; }
		}


		public bool IsSerialized
		{
			get { return config.IsSerialized; }
		}


		public List<TagEncodingInfo> Encode(EncodeRequest request)
		{
			var result = new List<TagEncodingInfo>();
			if (config.RoundUnitsPerPage && request.TagsPerSheet > 1)
				request.Quantity = (int)Math.Ceiling((double)request.Quantity / request.TagsPerSheet) * request.TagsPerSheet;

			config.Sequence.Data = request.VariableData;
			var serials = config.Sequence.AcquireMultiple(request.Quantity);  // Similar to Acquire, but AcquireMultiple allocates N serials in a single call, these serials are unique and are considered as consumed.

			for (int i = 0; i < request.Quantity; i++)
			{
				var serial = serials[i];
				result.Add(EncodeTag(request.VariableData, serial));
			}
			return result;
		}


		private TagEncodingInfo EncodeTag(JObject data, long serial)
		{
			config.Barcode.Encode(config.TagEncoding, data.GetValue<string>("Barcode"));
			if (config.TagEncoding.ContainsField("SerialNumber"))
				config.TagEncoding["SerialNumber"].Value = serial.ToString();
			else
				throw new InvalidOperationException("Invalid configuration detected in the RFID encoding pipe line, the tag encoding scheme does not contains a field called 'SerialNumber' which is required for the StandardEncodingAlgorithm");
			string accessPwd = config.AccessPasswordMethod.DerivePassword(config.TagEncoding);
			string killPwd = config.KillPasswordMethod.DerivePassword(config.TagEncoding);
			string epc = config.TagEncoding.GetHexadecimal();
			string userMemory = config.UserMemory.GetContent(config.TagEncoding, data);
			var tag = new TagEncodingInfo()
			{
				EPC = epc,
				Barcode = config.Barcode.Code,
				SerialNumber = serial,
				WriteUserMemory = config.UserMemory.WriteUserMemory,
				UserMemory = userMemory,
				WriteAccessPassword = config.AccessPasswordMethod.WritePassword,
				AccessPassword = accessPwd,
				WriteKillPassword = config.KillPasswordMethod.WritePassword,
				KillPassword = killPwd,
				WriteLocks = config.WriteLocks,
				EPCLock = config.EpcMemoryLock,
				UserLock = config.UserMemoryLock,
				AccessLock = config.AccessPasswordLock,
				KillLock = config.KillPasswordLock
			};
			tag.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, data, tag);
			tag.VerifyRFIDWhilePrinting = config.VerifyRFIDWhilePrinting;
			return tag;
		}


		public TagEncodingInfo EncodeSample(JObject data)
		{
			config.Sequence.Data = data;
			long serial = config.Sequence.GetCurrent();  // GetCurrent simply gets the current serial number, we dont expect uniqueness, also serials are NOT consumed by this call.
			return EncodeTag(data, serial);
		}


		public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
		{
			if (data == null || count <= 0 || startingSerial <= 0)
				throw new Exception("Received invalid input parameters.");
			if (config.BrandIndentifier == null || config.BrandIndentifier.Length != 4)
				throw new Exception("Invalid RFID configuration. Missing Brand Identifier which is required to generate RFID headers.");
			var ean13 = data["Barcode"].ToString().Substring(0, 12);
			var roundedCount = (int)Math.Ceiling((double)count / tagsPerSheet) * tagsPerSheet;
			if (config.RoundUnitsPerPage)
				return $"{config.BrandIndentifier}{ean13}{startingSerial:D10}{count:D6}{roundedCount:D6}";
			else
				return $"{config.BrandIndentifier}{ean13}{startingSerial:D10}{count:D6}{count:D6}";
		}


		public StandardEncodingConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(StandardEncodingConfig config)
		{
			this.config = config;
			if(config.Barcode is EAN13)
				(config.Barcode as EAN13).GS1CompanyPrefixOverrides = config.GS1CompanyPrefixOverrides;
		}
	}


	public class StandardEncodingConfig
	{
		public ITagEncoding TagEncoding;                    // Specifies how tags will be encoded. Supported encoding schemes include all GS1 standards such as Sgtin96, Sgtin198, Sgln96, Sgln195, etc. As well as some custom encoding schemes defined by our customers such as Tempe128.
		public IBarcode1D Barcode;                          // Specifies which type of barcode is used for this client.
		public ISerialSequence Sequence;                    // Specifies what kind of sequence will be used to generate serials.
		public IPasswordDeriveMethod AccessPasswordMethod;  // Specifies how do we generate the access password used in the tag.
		public IPasswordDeriveMethod KillPasswordMethod;    // Specifies how do we generate the kill password used in the tag.
		public IUserMemoryMethod UserMemory;                // Specifies the method used to initialize the user memory data bank
		public bool WriteLocks;                             // Specifies if the command to update the tag locks should be emmited or not
		public RFIDLockType EpcMemoryLock;                  // Specifies the type of lock used for the EPC data bank
		public RFIDLockType UserMemoryLock;                 // Specifies the type of lock used for the User data bank
		public RFIDLockType AccessPasswordLock;             // Specifies the type of lock used for the Access Password data bank
		public RFIDLockType KillPasswordLock;               // Specifies the type of lock used for the Kill Password data bank

		// The following fields are meant to be used to generate header barcodes required by the Table/ConveyorBelt encoding programs
		[Required, MaxLen(4), MinLen(4)]
		public string BrandIndentifier = "1234";                     // Identificador usado por la mesa y la cinta :/
		public bool RoundUnitsPerPage;                      // Indicates if unit quantity should be rounded per page (true) or if exact number requested should be used (false).
		public string TrackingCodeMask;                     // A mask used to create a barcode that can be used in the label to associate information encoded in the RFID Tag with an external system. Can be left null or empty if the client does not use TrackingCode or if the code is not related to the RFID system.
															// Example Mask:  "[IsPVPV0102.url_for_qr_code_value]/gtin/0[Barcode]/ser/%Serial%"
		public bool IsSerialized;

		public bool VerifyRFIDWhilePrinting;                // Controls part of the behavior of printers such as SATO/ZEBRA, if set to false, the system will not attempt to emmit commands to verify the RFID encoding of each printed label.
															// This verification step can be disabled to increase performance, but only if the client is not requesting RFID reports of encoded tags.

		public List<GS1CompanyPrefix> GS1CompanyPrefixOverrides;
	}


	/* NOTES:
	 * 
	 * Tracking Code Mask:
	 * 
	 *		In the mask, any sequence of characters that is not recognized as a known value is left as it is without change.
	 *		There are two types of Known values:
	 *			> Data Fields, which is data that comes from the system database. These fields are denoted by the use of '[' & ']'
	 *			  it is also necessary to specify the name of the table where the field is defined. Example:  [VariableData.Barcode]
	 *     
	 *			> RFID encoding field, which are fields that exist in the particular Encoding Algorithm or are related to the RFID chip itself.
	 *			  These fields are denoted by placing the field name between '%',
	 *     
	 *       
	 *       Additionally you can reference the following general RFID encoding fields (these are unrelated to the encoding scheme and are always available):
	 *			- EPC		(retrieves the EPC as an hex string)
	 *			- AccPwd	(retrieves the access password as an hex string)
	 *			- KillPwd	(retrieves the kill password as an hex string)
	 *			- UsrMem	(retrieves the user memory as an hex string)
	 *			- Serial	(retrieves the serial number as base 10)
	 *  
	 *		If any field specified in the mask is not found, then the encoding algorithm will throw an exception.
	 *  
	 *  GS1CompanyPrefixOverrides:
	 *  
	 *		These are used to determine the partition value when processing an EAN13 / GTIN14 (TBD), if none are supplied, the system will
	 *		use the prefixes provided by GS1. Any prefixes supplied here will take precedence over the default GS1 prefixes (acting as overrides).
	 *		In general is not recomended to override GS1 prefixes, unless you find a missing prefix, which is unlikely to happen.
	 */
}
