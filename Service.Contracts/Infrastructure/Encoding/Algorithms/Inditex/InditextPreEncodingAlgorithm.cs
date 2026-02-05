using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
    public class InditextPreEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<InditexEncodingConfig>
    {
        protected Tempe128 tag;
        protected InditexEncodingConfig config;

        public InditextPreEncodingAlgorithm(IFactory factory)
        {

            var inditexSeed = "12101492";//"12101492"; // this value was obtained from Inditext
            tag = new Tempe128();
            config = new InditexEncodingConfig();
            config.Sequence = new SingleSerialSequence(factory.GetInstance<ISerialRepository>());
            config.BrandField = "Brand";
            config.SectionField = "Section";
            config.ProductTypeField = "ProductType";
            config.ModelField = "Model";
            config.QualityField = "Quality";
            config.ColorField = "Color";
            config.SizeField = "Size";
            
            var accessPwd = new InditexMD5PasswordDerive();
            accessPwd.SetConfiguration(new PasswordDeriveMethodConfig() { Seed = inditexSeed });

            config.AccessPasswordMethod = accessPwd;
            config.EpcMemoryLock = RFIDLockType.UnLock;
            config.UserMemoryLock = RFIDLockType.UnLock;
            config.AccessPasswordLock = RFIDLockType.UnLock;
            config.KillPasswordLock = RFIDLockType.UnLock;

            var killPwd = new InditexMD5PasswordDerive();
            killPwd.SetConfiguration(new PasswordDeriveMethodConfig() { Seed = inditexSeed });
            config.KillPasswordMethod = killPwd;

            config.UserMemory = new UserMemorySerial();
            config.WriteLocks = false;

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

			var brandFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.BrandField) != null;
			var sectionFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.SectionField) != null;
			var productTypeFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ProductTypeField) != null;
			var modelFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ModelField) != null;
			var qualityFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.QualityField) != null;
			var colorFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.ColorField) != null;
			var sizeFieldExists = data.Properties().FirstOrDefault(p => p.Name == config.SizeField) != null;

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

			var brand = data.GetValue<string>(config.BrandField);
			var section = data.GetValue<string>(config.SectionField);
			var prodType = data.GetValue<string>(config.ProductTypeField);
			var modelVal = data.GetValue<string>(config.ModelField);
			var qualityVal = data.GetValue<string>(config.QualityField);
			var colorVal = data.GetValue<string>(config.ColorField);
			var sizeVal = data.GetValue<string>(config.SizeField);

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

			tag["Brand"].Value = data.GetValue<string>(config.BrandField);
			tag["Section"].Value = data.GetValue<string>(config.SectionField);
			tag["ProdType"].Value = data.GetValue<string>(config.ProductTypeField);
			tag["ActFlag"].Value = "0"; // fixed value for PREENCODING
										//int model = Int32.Parse(modelVal);
										//int quality = Int32.Parse(qualityVal);
										//int color = Int32.Parse(colorVal);
										//int size = Int32.Parse(sizeVal);

			//4354 / 543 / 452/ 50
			//tag.SetMCCT(model, quality, color, size); -> THIS METHOD NOT WORK IF ALL VALUES ARE 0 OR IF THE MODEL CONTAIN '0' AT THE BEGININ (0384)
			tag["MCCT"].Value = "000000000000"; // must be a result of concatenate as string model+quality+color+size
			tag.SetWriteDate(DateTime.Now);
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


        public InditexEncodingConfig GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(InditexEncodingConfig config)
        {
            this.config = config;
        }
    }
}
