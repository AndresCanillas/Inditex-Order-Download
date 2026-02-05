using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Service.Contracts;

namespace Service.Contracts
{
	public class DecimalEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<DecimalEncodingAlgorithmConfig>
	{
		private DecimalEncodingAlgorithmConfig config;
		private DecimalTagEncoding encoding;

		public DecimalEncodingAlgorithm()
		{
			config = new DecimalEncodingAlgorithmConfig();
			encoding = new DecimalTagEncoding();
		}

		// DGT does not require any serial sequence because the serial is given directly in the barcode, which is provided by the client.
		public ISerialSequence Sequence
		{
			get { return config.Sequence; }
		}

		public bool IsSerialized
		{
			get { return true; }
		}


		public List<TagEncodingInfo> Encode(EncodeRequest request)
		{
			var result = new List<TagEncodingInfo>();

			config.Sequence.Data = request.VariableData;
			var serials = config.Sequence.AcquireMultiple(request.Quantity);  // Similar to Acquire, but AcquireMultiple allocates N serials in a single call, these serials are unique and are considered as consumed.

			for (int i = 0; i < request.Quantity; i++)
			{
				var serial = serials[i];
				result.Add(InternalEncodeTag(request.VariableData, serial));
			}
			return result;
		}


		private TagEncodingInfo InternalEncodeTag(JObject data, long serial)
		{
			if (String.IsNullOrWhiteSpace(config.BarcodeField))
				throw new Exception($"Invalid RFID configuration: BarcodeField is null or empty");

			var barcodeExists = data.Properties().FirstOrDefault(p => p.Name == config.BarcodeField) != null;
			if (!barcodeExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.BarcodeField} does not exist in table VariableData.");

			var barcode = data.GetValue<string>(config.BarcodeField);
			if (String.IsNullOrWhiteSpace(barcode))
				throw new Exception($"Value of field {config.BarcodeField} in table VariableData is null or empty");

			if (barcode.Length * 4 > encoding["ItemReference"].BitLength)
				throw new InvalidOperationException($"Invalid RFID configuration: Barcode {barcode} cannot be fit in the ItemReference field. ItemReference admits {encoding["ItemReference"].BitLength} bits");

			encoding["ItemReference"].Value = barcode;
			encoding["SerialNumber"].Value = serial.ToString();
			var epc = encoding.GetHexadecimal();

			string accessPwd = config.AccessPasswordMethod.DerivePassword(encoding);
			string killPwd = config.KillPasswordMethod.DerivePassword(encoding);
			string epc2 = encoding.GetHexadecimal();
			string userMemory = config.UserMemory.GetContent(encoding, data);
			var tag = new TagEncodingInfo()
			{
				EPC = epc,
				Barcode = barcode,
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
			return InternalEncodeTag(data, serial);
		}


		public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
		{
			if (data == null || count <= 0 || startingSerial <= 0)
				throw new Exception("Received invalid input parameters.");
			if (config.BrandIndentifier == null || config.BrandIndentifier.Length != 4)
				throw new Exception("Invalid RFID configuration. Missing Brand Identifier which is required to generate RFID headers.");
			var ean13 = data["Barcode"].ToString().Substring(0, 12);
			var roundedCount = (int)Math.Ceiling((double)count / tagsPerSheet) * tagsPerSheet;
			return $"{config.BrandIndentifier}{ean13}{startingSerial:D10}{count:D6}{count:D6}";
		}


		public DecimalEncodingAlgorithmConfig GetConfiguration()
		{
			return config;
		}


		public void SetConfiguration(DecimalEncodingAlgorithmConfig config)
		{
			this.config = config;
			encoding.SetConfiguration(new DecimalTagEncodingConfig()
			{
				BarcodeField = config.BarcodeField,
				Prefix = config.Prefix,
				Suffix = config.Suffix,
				SerialBitLenght = config.SerialBitLenght
			});
		}
    }


	public class DecimalEncodingAlgorithmConfig
	{
		public string BarcodeField;                         // Specifies the name of the field that stores the barcode.
		public string Prefix;                               // Specifies a decimal value to be added in front of the barcode.
		public string Suffix;                               // Specifies a decimal value to be added after the barcode.
		public int SerialBitLenght = 32;                    // Specifies how many bits will be used for serial number.

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
		public string TrackingCodeMask;                     // A mask used to create a barcode that can be used in the label to associate information encoded in the RFID Tag with an external system. Can be left null or empty if the client does not use TrackingCode or if the code is not related to the RFID system.
															// Example Mask:  "[IsPVPV0102.url_for_qr_code_value]/gtin/0[Barcode]/ser/%Serial%"
		public bool IsSerialized;

		public bool VerifyRFIDWhilePrinting;                // Controls part of the behavior of printers such as SATO/ZEBRA, if set to false, the system will not attempt to emmit commands to verify the RFID encoding of each printed label.
	}



}
