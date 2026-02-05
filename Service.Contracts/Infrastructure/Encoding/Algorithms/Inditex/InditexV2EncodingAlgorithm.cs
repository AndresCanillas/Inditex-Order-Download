using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;


namespace Service.Contracts
{
	public class InditexV2EncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<InditexV2EcodingConfig>
	{
		protected InditexV2Tag tag;

		protected InditexV2EcodingConfig config;

		public InditexV2EncodingAlgorithm(IFactory factory)
		{

			var inditexSeed = "12101492"; // this value was obtained from Inditext

			tag = new InditexV2Tag();
			config = new InditexV2EcodingConfig();
			config.Sequence = new SingleSerialSequence(factory.GetInstance<ISerialRepository>());
			config.BrandField = "Brand";
			config.SectionField = "Section";
			config.ProductTypeField = "ProductType";
			config.ModelField = "Model";
			config.QualityField = "Quality";
			config.ColorField = "Color";
			config.SizeField = "Size";
			config.TagTypeField = "TagType";
			config.TagTypeField = "TagType";
			config.TagSubTypeField = "TagSubTypeField";
			config.InventoryTagField = "InventoryTagField";

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
            ValidateNotEmptyFieldsNames();
            ValidateFieldsExist(data);

            var brand = data.GetValue<string>(config.BrandField);
            var section = data.GetValue<string>(config.SectionField);
            var prodType = data.GetValue<string>(config.ProductTypeField);
            var modelVal = data.GetValue<string>(config.ModelField);
            var qualityVal = data.GetValue<string>(config.QualityField);
            var colorVal = data.GetValue<string>(config.ColorField);
            var sizeVal = data.GetValue<string>(config.SizeField);
            var tagTypeVal = data.GetValue<string>(config.TagTypeField);
            var tagSubTypeVal = data.GetValue<string>(config.TagSubTypeField);
            var inventoryTagVal = data.GetValue<string>(config.InventoryTagField);

            // Validate contains Data
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
            if (String.IsNullOrWhiteSpace(tagTypeVal))
                throw new Exception($"Value of field {config.TagTypeField} in table VariableData is null or empty");
            if (String.IsNullOrWhiteSpace(tagSubTypeVal))
                throw new Exception($"Value of field {config.TagSubTypeField} in table VariableData is null or empty");

            tag["BrandId"].Value = brand;
            tag["Section"].Value = section;
            tag["ProductType"].Value = prodType;
            tag["InventoryTAG"].Value = inventoryTagVal;
            tag["TagType"].Value = tagTypeVal;
            tag["TagSubType"].Value = tagSubTypeVal;
            tag["SerialNumber"].Value = serial.ToString();

            int model = Int32.Parse(modelVal);
            int quality = Int32.Parse(qualityVal);
            int color = Int32.Parse(colorVal);
            int size = Int32.Parse(sizeVal);

            tag.SetMCCT(model, quality, color, size);

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

            return taginfo;
        }

        public ISerialSequence Sequence
        {
            get { return config.Sequence; }
        }

        public bool IsSerialized
        {
            get { return config.IsSerialized; }
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


        public InditexV2EcodingConfig GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(InditexV2EcodingConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// check if required fields are configured correctly, all fields required a value
        /// </summary>
        private void ValidateNotEmptyFieldsNames()
        {
            if (String.IsNullOrWhiteSpace(config.BrandField))
                throw new Exception($"Invalid RFID configuration: 'BrandField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.SectionField))
                throw new Exception($"Invalid RFID configuration: 'SectionField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.ProductTypeField))
                throw new Exception($"Invalid RFID configuration: 'ProductTypeField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.ModelField))
                throw new Exception($"Invalid RFID configuration: 'ModelField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.QualityField))
                throw new Exception($"Invalid RFID configuration: 'QualityField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.ColorField))
                throw new Exception($"Invalid RFID configuration: 'ColorField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.SizeField))
                throw new Exception($"Invalid RFID configuration: 'SizeField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.TagTypeField))
                throw new Exception($"Invalid RFID configuration: 'TagTypeField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.TagSubTypeField))
                throw new Exception($"Invalid RFID configuration: 'TagSubTypeField' is null or empty");
            if (String.IsNullOrWhiteSpace(config.InventoryTagField))
                throw new Exception($"Invalid RFID configuration: 'InventoryTAGField' is null or empty");

        }
        /// <summary>
        /// Validate if the field names exist inner data received (use VariableData Catalogs to check)
        /// </summary>
        private void ValidateFieldsExist(JObject data)
        {
            var brandFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.BrandField) != null;
            var sectionFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.SectionField) != null;
            var productTypeFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ProductTypeField) != null;
            var modelFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ModelField) != null;
            var qualityFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.QualityField) != null;
            var colorFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ColorField) != null;
            var sizeFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.SizeField) != null;
            var tagTypeFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.TagTypeField) != null;
            var tagSubTypeFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.TagSubTypeField) != null;
            var invetoryTagFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.InventoryTagField) != null;

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
            if (!tagTypeFieldExist)
                throw new InvalidOperationException($"Invalid RFID configuration: Field {config.TagTypeField} does not exist in table VariableData.");
            if (!tagTypeFieldExist)
                throw new InvalidOperationException($"Invalid RFID configuration: Field {config.TagSubTypeField} does not exist in table VariableData.");
            if (!invetoryTagFieldExist)
                throw new InvalidOperationException($"Invalid RFID configuration: Field {config.InventoryTagField} does not exist in table VariableData.");

        }
    }

    public class InditexV2EcodingConfig
    {
        public ISerialSequence Sequence;                    // Specifies what kind of sequence will be used to generate serials.
        public string BrandField;                           // Specifies the name of the field that stores the brand.
        public string SectionField;                         // Specifies the name of the field that stores the section.
        public string ProductTypeField;                     // Specifies the name of the field that stores the product type.
        public string ModelField;                           // Specifies the name of the field that stores the model.
        public string QualityField;                         // Specifies the name of the field that stores the Quality.
        public string ColorField;                           // Specifies the name of the field that stores the Color.
        public string SizeField;                            // Specifies the name of the field that stores the Size.
        public string TagTypeField;                         // Specifies the name of the field that stores the Size.
        public string TagSubTypeField;                      // Specifies the name of the field that stores the Size.
        public string InventoryTagField;
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
    }
}
