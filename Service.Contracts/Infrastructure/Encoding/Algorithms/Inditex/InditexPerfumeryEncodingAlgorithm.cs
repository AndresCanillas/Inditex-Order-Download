using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
    public class InditexPerfumeryEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<InditexPerfumeryEncodingConfig>
    {
        protected Inditex128 tag;
        protected InditexPerfumeryEncodingConfig config;

        public InditexPerfumeryEncodingAlgorithm(IFactory factory)
        {
            var inditexSeed = "12101492"; // this value was obtained from Inditext

            tag = new Inditex128();
            config = new InditexPerfumeryEncodingConfig();
            config.Sequence = new SingleSerialSequence(factory.GetInstance<ISerialRepository>());
            config.BrandField = "Brand";
            config.SectionField = "Section";
            config.ProductTypeField = "ProductType";
            config.ModelField = "Model";
            config.QualityField = "Quality";
            config.ColorField = "Color";
            config.SizeField = "Size";
            config.BachDateField = "XADSFASDF";
            var accessPwd = new InditexMD5PasswordDerive();
            accessPwd.SetConfiguration(new PasswordDeriveMethodConfig() { Seed = inditexSeed });

            config.AccessPasswordMethod = accessPwd;
            config.EpcMemoryLock = RFIDLockType.Lock;
            config.UserMemoryLock = RFIDLockType.Lock;
            config.AccessPasswordLock = RFIDLockType.Lock;
            config.KillPasswordLock = RFIDLockType.PermaLock;

            var killPwd = new InditexMD5PasswordDerive();
            killPwd.SetConfiguration(new PasswordDeriveMethodConfig() { Seed = inditexSeed });
            config.KillPasswordMethod = killPwd;

            config.UserMemory = new UserMemorySerial();
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
			List<TagEncodingInfo> result = new List<TagEncodingInfo>();
			config.Sequence.Data = request.VariableData;
			var serials = config.Sequence.AcquireMultiple(request.Quantity);
			for (int i = 0; i < request.Quantity; i++)
			{
				long serial = serials[i];
				result.Add(EncodeTag(request.VariableData, serial));
			}
			return result;
		}


		private TagEncodingInfo EncodeTag(JObject data, long serial)
		{
			tag["SerialNumber"].Value = serial.ToString();

			if (String.IsNullOrWhiteSpace(config.BrandField))
				throw new Exception($"Invalid RFID configuration: BrandField is null or empty");
			if (String.IsNullOrWhiteSpace(config.SectionField))
				throw new Exception($"Invalid RFID configuration: SectionField is null or empty");
			if (String.IsNullOrWhiteSpace(config.ProductTypeField))
				throw new Exception($"Invalid RFID configuration: ProductTypeField is null or empty");
			if (String.IsNullOrWhiteSpace(config.ModelField))
				throw new Exception($"Invalid RFID configuration: ModelField is null or empty");
			if (String.IsNullOrWhiteSpace(config.QualityField))
				throw new Exception($"Invalid RFID configuration: QualityField is null or empty");
			if (String.IsNullOrWhiteSpace(config.ColorField))
				throw new Exception($"Invalid RFID configuration: ColorField is null or empty");
			if (String.IsNullOrWhiteSpace(config.SizeField))
				throw new Exception($"Invalid RFID configuration: SizeField is null or empty");
			if (String.IsNullOrWhiteSpace(config.BachDateField))
				throw new Exception($"Invalid RFID configuration: BachDateField is null or empty");

			var brandFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.BrandField) != null;
			var sectionFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.SectionField) != null;
			var productTypeFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ProductTypeField) != null;
			var modelFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ModelField) != null;
			var qualityFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.QualityField) != null;
			var colorFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ColorField) != null;
			var sizeFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.SizeField) != null;
			var batchDateExist = data.Properties().FirstOrDefault(p => p.Name == config.BachDateField) != null;

			if (!brandFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.BrandField} does not exist in table VariableData.");
			if (!sectionFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.SectionField} does not exist in table VariableData.");
			if (!productTypeFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.ProductTypeField} does not exist in table VariableData.");
			if (!modelFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.ModelField} does not exist in table VariableData.");
			if (!qualityFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.QualityField} does not exist in table VariableData.");
			if (!colorFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.ColorField} does not exist in table VariableData.");
			if (!sizeFieldExists)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.SizeField} does not exist in table VariableData.");
			if (!batchDateExist)
				throw new InvalidOperationException($"Invalid RFID configuration: Field {config.BachDateField} does not exist in table VariableData.");


			var brand = data.GetValue<string>(config.BrandField);
			var section = data.GetValue<string>(config.SectionField);
			var prodType = data.GetValue<string>(config.ProductTypeField);
			var modelVal = data.GetValue<string>(config.ModelField);
			var qualityVal = data.GetValue<string>(config.QualityField);
			var colorVal = data.GetValue<string>(config.ColorField);
			var sizeVal = data.GetValue<string>(config.SizeField);
			var batchDateVal = data.GetValue<string>(config.BachDateField);

			if (String.IsNullOrWhiteSpace(brand))
				throw new Exception($"Value of field {config.BrandField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(section))
				throw new Exception($"Value of field {config.SectionField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(prodType))
				throw new Exception($"Value of field {config.ProductTypeField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(modelVal))
				throw new Exception($"Value of field {config.ModelField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(qualityVal))
				throw new Exception($"Value of field {config.QualityField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(colorVal))
				throw new Exception($"Value of field {config.ColorField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(sizeVal))
				throw new Exception($"Value of field {config.SizeField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(batchDateVal))
				throw new Exception($"Value of field {config.BachDateField} in table VariableData is null or empty");

			tag["Brand"].Value = data.GetValue<string>(config.BrandField);
			tag["Section"].Value = data.GetValue<string>(config.SectionField);
			tag["ProdType"].Value = data.GetValue<string>(config.ProductTypeField);
			int model = Int32.Parse(modelVal);
			int quality = Int32.Parse(qualityVal);
			int color = Int32.Parse(colorVal);
			int size = Int32.Parse(sizeVal);

			tag.SetMCCT(model, quality, color, size);
			tag.SetWriteDate(batchDateVal);
			string accessPwd = config.AccessPasswordMethod.DerivePassword(tag);
			string epc = tag.GetHexadecimal();
			string userMemory = config.UserMemory.GetContent(tag, data);
			string killPwd = config.KillPasswordMethod.DerivePassword(tag);
			var taginfo = new TagEncodingInfo()
			{
				EPC = epc,
				Barcode = "",
				SerialNumber = serial,
				WriteUserMemory = config.UserMemory.WriteUserMemory,
				UserMemory = userMemory,
				WriteAccessPassword = true,
				AccessPassword = accessPwd,
				WriteKillPassword = config.KillPasswordMethod.WritePassword,
				KillPassword = killPwd,
				WriteLocks = config.WriteLocks,
				EPCLock = config.EpcMemoryLock,
				UserLock = config.UserMemoryLock,
				AccessLock = config.AccessPasswordLock,
				KillLock = config.KillPasswordLock
			};
			taginfo.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, data, taginfo);
			taginfo.VerifyRFIDWhilePrinting = config.VerifyRFIDWhilePrinting;
			return taginfo;
		}


		public TagEncodingInfo EncodeSample(JObject data)
        {
            config.Sequence.Data = data;
            long serial = config.Sequence.GetCurrent();
            return EncodeTag(data, serial);
        }


        public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
        {
            return "";  // Not implemented yet for inditex... hopefully by then we will already have created new programs for Table/Belt/Hercules machines
        }


        public InditexPerfumeryEncodingConfig GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(InditexPerfumeryEncodingConfig config)
        {
            this.config = config;
        }
    }

    
    public class InditexPerfumeryEncodingConfig
    {
        public ISerialSequence Sequence;                    // Specifies what kind of sequence will be used to generate serials.
                                                            // The following properties MUST contains the name of fields from the product data table (or related tables), the referenced fields are expected to contain integer values that will be encoded into the different fields of the EPC...
        public string BrandField;                           // Specifies the name of the field that stores the brand.
        public string SectionField;                         // Specifies the name of the field that stores the section.
        public string ProductTypeField;                     // Specifies the name of the field that stores the product type.
        public string ModelField;                           // Specifies the name of the field that stores the model.
        public string QualityField;                         // Specifies the name of the field that stores the Quality.
        public string ColorField;                           // Specifies the name of the field that stores the Color.
        public string SizeField;                            // Specifies the name of the field that stores the Size.
        public string BachDateField;                        // Specifies the name of the field that stores the Batch Date Number,  number YDDDO -> year, day of the year, batch number -> will be asigned for the Customer inner his orders
        public IPasswordDeriveMethod AccessPasswordMethod;  // Specifies how do we generate the access password used in the tag.
        public IPasswordDeriveMethod KillPasswordMethod;    // Specifies how do we generate the kill password used in the tag.
        public IUserMemoryMethod UserMemory;                // Specifies the method used to initialize the user memory data bank
        public bool WriteLocks;                             // Specifies if the command to update the tag locks should be emmited or not
        public RFIDLockType EpcMemoryLock;                  // Specifies the type of lock used for the EPC data bank
        public RFIDLockType UserMemoryLock;                 // Specifies the type of lock used for the User data bank
        public RFIDLockType AccessPasswordLock;             // Specifies the type of lock used for the Access Password data bank
        public RFIDLockType KillPasswordLock;               // Specifies the type of lock used for the Kill Password data bank
        public string TrackingCodeMask;                     // A mask used to create a QR code that can be used in the label to associate information encoded in the RFID Tag with an external system. Can be left null or empty if the client does not use QRCodes or if the QR code is not related to the RFID system. This field works in the same way as the StandardEncodngAlgorithm, see that for more information on how to setup a QRMask.
        public bool IsSerialized;

        public bool VerifyRFIDWhilePrinting;                // Controls part of the behavior of printers such as SATO/ZEBRA, if set to false, the system will not attempt to emmit commands to verify the RFID encoding of each printed label.
    }
}
