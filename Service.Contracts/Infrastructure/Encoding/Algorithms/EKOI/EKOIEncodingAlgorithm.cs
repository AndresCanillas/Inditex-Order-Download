using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts.Infrastructure.Encoding.Algorithms.EKOI
{
    public class EKOIEncodingAlgorithm : ITagEncodingAlgorithm, IConfigurable<EKOIEncodingConfig>
    {
        private EKOIEncodingConfig config;

        public EKOIEncodingAlgorithm(IFactory factory)
        {
            //tag = new SimpleSerialTagEncoding();
            config = new EKOIEncodingConfig();
            config.TagEncoding = new Sgtin96();
            config.Barcode = new EAN13();
            config.Sequence = new SingleSerialSequence(factory.GetInstance<ISerialRepository>());
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


		/*
		 * El código de barras en código 128 impreso en la etiqueta (Número de soporte único) respeta el siguiente formato:
		 * 12 primeros caracteres del EAN 13 (sin el carácter 13 que corresponde al Check Digit) + 6 caracteres del número de serie en Decimal
		 */
		private TagEncodingInfo EncodeTag(JObject data, long serial)
		{

			var serialStr = serial.ToString("D6");
			config.Barcode.Code = data.GetValue<string>("Barcode"); // this line validate the barcode as EAN13
			config.Barcode.Encode(config.TagEncoding, data.GetValue<string>("Barcode"));

			if (config.TagEncoding.ContainsField("SerialNumber"))
				config.TagEncoding["SerialNumber"].Value = serial.ToString();
			else
				throw new InvalidOperationException("Invalid configuration detected in the RFID encoding pipe line, the tag encoding scheme does not contains a field called 'SerialNumber' which is required for the StandardEncodingAlgorithm");


			string accessPwd = config.AccessPasswordMethod.DerivePassword(config.TagEncoding);

			string epc = config.TagEncoding.GetHexadecimal();

			string userMemory = config.UserMemory.GetContent(config.TagEncoding, data);

			string killPwd = config.KillPasswordMethod.DerivePassword(config.TagEncoding);


			var tag = new TagEncodingInfo()
			{
				EPC = epc,
				Barcode = serialStr,
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
				KillLock = config.KillPasswordLock,
				VerifyRFIDWhilePrinting = config.VerifyRFIDWhilePrinting
			};
			tag.TrackingCode = BarcodeProcessing.ProcessTrackingCodeMask(config.TrackingCodeMask, data, tag);

			return tag;
		}


		public TagEncodingInfo EncodeSample(JObject data)
        {
            config.Sequence.Data = data;
            long serial = config.Sequence.GetCurrent();
            return EncodeTag(data, serial);
        }


        public string EncodeHeader(JObject data, int count, long startingSerial, int tagsPerSheet = 1)
        {
            return "";
        }

        public EKOIEncodingConfig GetConfiguration()
        {
            return config;
        }

        public void SetConfiguration(EKOIEncodingConfig config)
        {
            this.config = config;
        }

        public ISerialSequence Sequence
        {
            get { return config.Sequence; }
        }

        public bool IsSerialized
        {
            get { return config.IsSerialized; }
        }
    }

    public class EKOIEncodingConfig
    {
        public ITagEncoding TagEncoding;                    // Specifies how tags will be encoded. Supported encoding schemes include all GS1 standards such as Sgtin96, Sgtin198, Sgln96, Sgln195, etc. As well as some custom encoding schemes defined by our customers such as Tempe128.
        public IBarcode1D Barcode;                          // Specifies which type of barcode is used for this client.
        public ISerialSequence Sequence;// Specifies what kind of sequence will be used to generate serials.
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
        public bool VerifyRFIDWhilePrinting;				// Controls part of the behavior of printers such as SATO/ZEBRA, if set to false, the system will not attempt to emmit commands to verify the RFID encoding of each printed label.

    }
}
