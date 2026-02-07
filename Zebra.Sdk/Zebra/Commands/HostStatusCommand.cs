using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Services.Zebra.Commands
{
    public class HostStatusCommand : BaseCommand
    {
        public HostStatusCommand()
        {
            SetMessage("~HS");
            IsOneWay = false;
        }

        public bool IsValidResponse;
        public bool PaperOut;
        public bool Paused;
        public int LabelLength;
        public int NumberOfFormatsInBuffer;
        public bool BufferFull;
        public bool ComDiagnostics;
        public bool PartialFormatInProgress;
        public bool CorruptRAM;
        public bool UnderTemperature;
        public bool OverTemperature;
        public bool HeadUp;
        public bool RibbonOut;
        public bool ThermalTransferMode;
        public string PrintMode;
        public bool LabelWaitingPeelOff;
        public int LabelsRemainingInBatch;

        public override void SetResponse(string response)
        {
            base.SetResponse(response);
            IsValidResponse = false;
			int startIdx = response.IndexOf("\x02");
			if (startIdx >= 0)
				response = response.Substring(startIdx);
			else
				return; // Response is invalid as we cannot find the STX control character
            string[] lines = response.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                string[] tokens = lines[0].Split(',');
                if (tokens.Length >= 12)
                {
                    PaperOut = tokens[1] == "1";
                    Paused = tokens[2] == "1";
                    Int32.TryParse(tokens[3], out LabelLength);
                    Int32.TryParse(tokens[4], out NumberOfFormatsInBuffer);
                    BufferFull = tokens[5] == "1";
                    ComDiagnostics = tokens[6] == "1";
                    PartialFormatInProgress = tokens[7] == "1";
                    CorruptRAM = tokens[9] == "1";
                    UnderTemperature = tokens[10] == "1";
                    OverTemperature = tokens[11] == "1";
                    tokens = lines[1].Split(',');
                    if (tokens.Length >= 11)
                    {
                        HeadUp = tokens[2] == "1";
                        RibbonOut = tokens[3] == "1";
                        ThermalTransferMode = tokens[4] == "1";
                        PrintMode = tokens[5];
                        LabelWaitingPeelOff = tokens[7] == "1";
                        Int32.TryParse(tokens[8], out LabelsRemainingInBatch);
                        IsValidResponse = true;
                    }
                }
            }
        }

		public override void Reset()
		{
			IsValidResponse = false;
		}
	}
}
