using Service.Contracts;
using Service.Contracts.LabelService;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Zebra.Commands
{
	public class PrintLabelCommand : BaseCommand, IPrintLabelCommand
	{
		private IRFIDConfigRepository rfidRepo;
		private ILabelRepository labelRepo;
		private IZebraRFIDEncoder encoder;
		private IBLabelServiceClient labelService;
		private ILogService log;
		private IVariableData productData;
		private IAppInfo appInfo;
		private bool encodeRFID;
		private string preamble;
		private bool loggedZPL;

		public PrintLabelCommand(
			IAppConfig configuration,
			IRFIDConfigRepository rfidRepo,
			ILabelRepository labelRepo,
			IZebraRFIDEncoder encoder,
			IBLabelServiceClient labelService,
			ILogService log,
			IAppInfo appInfo)
		{
			this.rfidRepo = rfidRepo;
			this.labelRepo = labelRepo;
			this.encoder = encoder;
			this.labelService = labelService;
			this.labelService.Url = configuration["WebLink:LabelService"];
			this.log = log;
			this.appInfo = appInfo;
			IsOneWay = true;
		}


        public async Task PrepareLabel(int projectid, int labelid, int orderid, string orderNumber, int detailid, IVariableData productData, IPrinterSettings settings, string driverName, bool isSample)
        {
            ProjectID = projectid;
            var label = labelRepo.GetByID(labelid, true);
            LabelID = label.ID;
            LabelName = label.Name;
            this.productData = productData;
            ProductCode = productData.Barcode;
            encodeRFID = !isSample && label.EncodeRFID;
			if (encodeRFID)
            {
                var rfidConfig = rfidRepo.SearchRFIDConfig(projectid);
				if (rfidConfig == null)
                    throw new InvalidOperationException($"Could not find any valid RFID configuration for project {projectid}");
                encoder.SetRFIDConfig(rfidConfig);
                encoder.OrderID = orderid;
                encoder.OrderNumber = orderNumber;
                encoder.DetailID = detailid;
            }
            var zpl = await labelRepo.PrintArticleByQuantityAsync(labelid, orderid, detailid, driverName, settings, isSample);
            zpl = StripPreamble(zpl, out preamble);
            SetMessage(zpl);
            loggedZPL = false;
        }


        public async Task PrepareHeader(IVariableData productData, IPrinterSettings settings, string driverName)
        {
            PrintHeaderRequest cfg = new PrintHeaderRequest();
            cfg.XOffset = (int)settings.XOffset;
            cfg.YOffset = (int)settings.YOffset;
            cfg.Speed = settings.Speed;
            cfg.Darkness = settings.Darkness;
            cfg.Rotated = settings.Rotated;
            cfg.DriverName = driverName;
            cfg.Rotated = settings.Rotated;
            cfg.ChangeOrientation = settings.ChangeOrientation;
            cfg.HeaderType = HeaderType.GenericHeader;
            cfg.Values = productData.Data;
            var response = await labelService.PrintHeaderAsync(cfg);
			if (!response.Success)
                throw new Exception("Error processing request to LabelService: " + response.ErrorMessage);
            SetMessage(response.Content);
            loggedZPL = false;
        }


        public override string ToString()
        {
            return $"{typeof(PrintLabelCommand).Name} - {LastSerial} {LastEPC}";
        }


        public bool EncodeRFID { get => encodeRFID; }
        public int ProjectID { get; set; }
        public int LabelID { get; set; }
        public string LabelName { get; set; }
        public string ProductCode { get; set; }
        public long LastSerial { get; set; }
        public string LastEPC { get; set; }
        public string Preamble
        {
            get { return preamble; }
            set { preamble = value; }
        }
        public string AccessPassword { get; set; }
        public string KillPassword { get; set; }
		public bool EnableCut { get; set; }		// If set to true, changes print mode to CUT causing the cutter to be cycled after printing the label.
		public bool IsLastInBatch { get; set; }	// Set to true to change mode to tear off instead of rewind unless cutter is enabled.

        public override byte[] ToByteArray()
        {
            Reset();
            string rfidCommands = "";
            var zpl = Message;
			if (encodeRFID)
			{
				rfidCommands = encoder.Encode(productData);
				LastSerial = encoder.LastSerial;
				LastEPC = encoder.LastEPC;
				AccessPassword = encoder.AccessPassword;
				KillPassword = encoder.KillPassword;
			}
			else
			{
				rfidCommands = "^FN0\"0\"^FS^FH_^HV0,0,TID:[]EPC:[,]_0D_0A,L^FS"; 
				LastSerial = 0;
				LastEPC = "";
				AccessPassword = "";
				KillPassword = "";
			}
			zpl = InsertRFIDCommands(zpl, rfidCommands);
			zpl = SetPrintMode(zpl);
			if (!loggedZPL)
            {
                loggedZPL = true;
                var ls = log.GetSection("ZPL");
                ls.LogMessage("====================== ZPL START (Label {0} - {1}) ======================", LabelID, LabelName);
                ls.LogMessage(zpl);
                ls.LogMessage("========================================= ZPL END =========================================");
            }
            return Encoding.UTF8.GetBytes(zpl);
        }


        private string InsertRFIDCommands(string zpl, string rfidCommands)
        {
            // First look for a barcode element, we asume the barcode element will be close to the center of the label near the RFID chip.
            int barcodePosition = zpl.IndexOf("^B");  // NOTE: this is safe because only barcode commands start with "^B"
            if(barcodePosition >= 0)
            {
                // The label has a native barcode element, insert the RFID commands right before the barcode.
                zpl = zpl.Substring(0, barcodePosition) + rfidCommands + zpl.Substring(barcodePosition);
            }
            else
            {
                // The label does not have a native barcode, best we can do is insert the RFID commands at the top, before the first ^FO command, and pray all goes well.
                int fieldStart = zpl.IndexOf("^FO");
				if (fieldStart >= 0)
                {
                    zpl = zpl.Substring(0, fieldStart) + rfidCommands + zpl.Substring(fieldStart);
                }
                else
                {
                    // No native barcode and no ^FO command, try insert RFID commands at the end of the format...
                    int formatEnd = zpl.LastIndexOf("^XZ");
					if (formatEnd >= 0)
                    {
                        zpl = zpl.Substring(0, formatEnd) + rfidCommands + zpl.Substring(formatEnd);
                    }
                    else throw new Exception("Invalid ZPL, cannot insert RFID commands");
                }
            }
            return zpl;
        }


        // Nice label generates a command that sets the print mode to tear-off (^MMT), this causes back feed after each printed label.
        // We need to chage that to rewind mode (^MMR) unless it is the last label in the batch.
        private string SetPrintMode(string zpl)
        {
            var mode = "^MMR\r\n";
            if(IsLastInBatch)
                mode = "^MMT\r\n";
			if (EnableCut)
                mode = "^MMC,N\r\n";
            var idx1 = zpl.IndexOf("^MM");
            if(idx1 >= 0)
            {
                var idx2 = zpl.IndexOf("^", idx1 + 3);  // NOTE: search for the next command (starting with ^)
				if (idx2 > idx1)
                    zpl = zpl.Substring(0, idx1) + mode + zpl.Substring(idx2);
                else
                    throw new Exception("Invalid ZPL, cannot insert PrintMode command (1)");
            }
            else
            {
                idx1 = zpl.IndexOf("^XA");
				if (idx1 >= 0)
                {
                    idx1 += 3;
                    zpl = zpl.Substring(0, idx1) + mode + zpl.Substring(idx1);
                }
                else
                    throw new Exception("Invalid ZPL, cannot insert PrintMode command (2)");
            }
            return zpl;
        }


        //Nice Label usually generates this preamble for zebra printers: 
        //_CT ~~CD,~CC^~CT~^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^PR6,6~SD10^JUS^LRN^CI0^XZ
        // Followed by the actual format...
        // This method removes the preample (if found) from the message, returning the updated message.
        // It also returns the preamble in the out parameter.
        private string StripPreamble(string message, out string preamble)
        {
            preamble = null;
            if(ContainsPreamble(message))
            {
                if(message.IndexOf("^JUS") < 100)
                {
                    int preambleEnd = message.IndexOf("^XZ");
                    if(preambleEnd < 140)
                    {
                        preambleEnd += 3;
                        preamble = message.Substring(0, preambleEnd);
                        return message.Substring(preambleEnd);
                    }
                }
            }
            return message;
        }


        private bool ContainsPreamble(string message)
        {
            int idx = 0;
            int formatCount = 0;
            do
            {
                idx = message.IndexOf("^XZ", idx);
				if (idx >= 0)
                {
                    idx += 3;
                    formatCount++;
                }
            } while (idx >= 0);
            return formatCount >= 2;
        }
    }
}
