using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
    public class MayoralEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<MayoralEncodingConfig>
    {
        private Mayoral96 tag;
        private MayoralEncodingConfig config;

        public ISerialSequence Sequence { get { return config.Sequence; } }
        public bool IsSerialized { get { return config.IsSerialized; } }

        public MayoralEncodingAlgorithm(IFactory factory)
        {
            var mayoralSeed = "000000";

            tag = new Mayoral96();
            config = new MayoralEncodingConfig();
            //config.Barcode = new EAN13();
            config.Sequence = new SingleSerialSequence(factory.GetInstance<ISerialRepository>());
            config.Prefix = "77";
            config.SeasonField = "Season";
            config.YearField = "Year";
            config.ArticleCodeField = "ArticleKey";// avoid to confuse with Order.Detail.ArticleCode field in data
            config.ColorCodeField = "ColorKey";
            config.SizeField = "SizeKey";
            config.OrderNumberField = "OrderNumber";

            var accessPwd = new FixedPassword();
            accessPwd.SetConfiguration(new PasswordDeriveMethodConfig() { Seed = mayoralSeed });

            config.AccessPasswordMethod = accessPwd;
            config.EpcMemoryLock = RFIDLockType.Lock;
            config.UserMemoryLock = RFIDLockType.Lock;
            config.AccessPasswordLock = RFIDLockType.Lock;
            config.KillPasswordLock = RFIDLockType.PermaLock;

            var killPwd = new FixedPassword();
            killPwd.SetConfiguration(new PasswordDeriveMethodConfig() { Seed = mayoralSeed });
            config.KillPasswordMethod = killPwd;

            config.UserMemory = new UserMemorySerial();


        }

        public MayoralEncodingConfig GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(MayoralEncodingConfig config)
        {
            this.config = config;
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
			if (String.IsNullOrWhiteSpace(config.SeasonField))
				throw new Exception($"Invalid Mayoral96 RFID configuration: SeasonField is null or empty");
			if (String.IsNullOrWhiteSpace(config.YearField))
				throw new Exception($"Invalid Mayoral96 RFID configuration: YearField is null or empty");
			if (String.IsNullOrWhiteSpace(config.ArticleCodeField))
				throw new Exception($"Invalid Mayoral96 RFID configuration: ArticleCodeField is null or empty");
			if (String.IsNullOrWhiteSpace(config.ColorCodeField))
				throw new Exception($"Invalid Mayoral96 RFID configuration: ColorCodeField is null or empty");
			if (String.IsNullOrWhiteSpace(config.SizeField))
				throw new Exception($"Invalid Mayoral96 RFID configuration: QualityField is null or empty");
			if (String.IsNullOrWhiteSpace(config.OrderNumberField))
				throw new Exception($"Invalid Mayoral96 RFID configuration: ColorField is null or empty");

			var seasonFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.SeasonField) != null;
			var yearFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.YearField) != null;
			var articleCodeFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.ArticleCodeField) != null;
			var colorCodeFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.ColorCodeField) != null;
			var sizeFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.SizeField) != null;
			var orderNumberFieldExist = data.Properties().FirstOrDefault(p => p.Name == config.OrderNumberField) != null;

			var prefix = config.Prefix;
			var season = data.GetValue<string>(config.SeasonField);
			var year = data.GetValue<string>(config.YearField);
			var articleCode = data.GetValue<string>(config.ArticleCodeField);
			var colorCode = data.GetValue<string>(config.ColorCodeField);
			var size = data.GetValue<string>(config.SizeField);
			var orderNo = data.GetValue<string>(config.OrderNumberField);
			//config.Barcode.Encode(tag, data.GetValue<string>("Barcode"));

			if (String.IsNullOrWhiteSpace(prefix))
				throw new Exception($"Value of field {prefix} in Configuration is null or empty");
			if (String.IsNullOrWhiteSpace(season))
				throw new Exception($"Value of field {config.SeasonField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(year))
				throw new Exception($"Value of field {config.YearField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(articleCode))
				throw new Exception($"Value of field {config.ArticleCodeField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(colorCode))
				throw new Exception($"Value of field {config.ColorCodeField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(size))
				throw new Exception($"Value of field {config.SizeField} in table VariableData is null or empty");
			if (String.IsNullOrWhiteSpace(orderNo))
				throw new Exception($"Value of field {config.OrderNumberField} in table VariableData is null or empty");

			// set field inner tag to generate EPC

			tag["Prefix"].Value = prefix.PadLeft(tag["Prefix"].BitLength / 4, '0');
			tag["Season"].Value = season.PadLeft(tag["Season"].BitLength / 4, '0');
			tag["Year"].Value = year.PadLeft(tag["Year"].BitLength / 4, '0');
			tag["ArticleCode"].Value = articleCode.PadLeft(tag["ArticleCode"].BitLength / 4, '0');
			tag["ColorCode"].Value = colorCode.PadLeft(tag["ColorCode"].BitLength / 4, '0');
			tag["Size"].Value = size.PadLeft(tag["Size"].BitLength / 4, '0');
			tag["OrderNumber"].Value = orderNo.PadLeft(tag["OrderNumber"].BitLength / 4, '0');
			tag["SerialNumber"].Value = serial.ToString().PadLeft(tag["SerialNumber"].BitLength / 4, '0');


			string epc = tag.ToHex();
			string accessPwd = config.AccessPasswordMethod.DerivePassword(tag);
			string userMemory = config.UserMemory.GetContent(tag, data);
			string killPwd = config.KillPasswordMethod.DerivePassword(tag);

			var tagInfo = new TagEncodingInfo()
			{
				EPC = epc,
				Barcode = data.GetValue<string>("Barcode"), // hardcode Barcode value from Mayoral Order
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
			tagInfo.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, data, tagInfo);
			tagInfo.VerifyRFIDWhilePrinting = config.VerifyRFIDWhilePrinting;

			return tagInfo;
		}


		public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
        {
            return "";
        }

        public TagEncodingInfo EncodeSample(JObject data)
        {
            config.Sequence.Data = data;
            long serial = config.Sequence.GetCurrent();
            return EncodeTag(data, serial);
        }
    }

    public  class MayoralEncodingConfig
    {

        public ISerialSequence Sequence;

        // Mayoral CustomFields, get from VariableData Table
        public string Prefix = "77";                        // fixed value, requested by the client
        public string SeasonField;                               // 2 digit, Specifies the name of the field that stores the Season.
        public string YearField;                                 // 1 digit, Specifies the name of the field that stores the Year.
        public string ArticleCodeField;                          // 5 digits, Specifies the name of the field that stores the Article Code.
        public string ColorCodeField;                            // 3 digits, Specifies the name of the field that stores the Color Code.
        public string SizeField;                                 // 2 digits, Specifies the name of the field that stores the Size.
        public string OrderNumberField;                          // 5 digits, Specifies the name of the field that stores the Order Number.


        //public IBarcode1D Barcode; 
        public IPasswordDeriveMethod AccessPasswordMethod;  // Specifies how do we generate the access password used in the tag.
        public IPasswordDeriveMethod KillPasswordMethod;    // Specifies how do we generate the kill password used in the tag.
        public IUserMemoryMethod UserMemory;                // Specifies the method used to initialize the user memory data bank
        public bool WriteLocks;                             // Specifies if the command to update the tag locks should be emmited or not
        public RFIDLockType EpcMemoryLock;                  // Specifies the type of lock used for the EPC data bank
        public RFIDLockType UserMemoryLock;                 // Specifies the type of lock used for the User data bank
        public RFIDLockType AccessPasswordLock;             // Specifies the type of lock used for the Access Password data bank
        public RFIDLockType KillPasswordLock;               // Specifies the type of lock used for the Kill Password data bank
        public bool IsSerialized;                           // Specifies what kind of sequence will be used to generate serials.


        public string TrackingCodeMask;                     // A mask used to create a barcode that can be used in the label to associate information encoded in the RFID Tag with an external system. Can be left null or empty if the client does not use TrackingCode or if the code is not related to the RFID system.
                                                            // Example Mask:  "[IsPVPV0102.url_for_qr_code_value]/gtin/0[Barcode]/ser/%Serial%"

        public bool VerifyRFIDWhilePrinting;                // Controls part of the behavior of printers such as SATO/ZEBRA, if set to false, the system will not attempt to emmit commands to verify the RFID encoding of each printed label.
                                                            // This verification step can be disabled to increase performance, but only if the client is not requesting RFID reports of encoded tags.

    }
}
