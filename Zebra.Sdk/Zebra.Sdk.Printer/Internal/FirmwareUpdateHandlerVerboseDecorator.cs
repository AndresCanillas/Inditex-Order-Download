using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FirmwareUpdateHandlerVerboseDecorator : FirmwareUpdateHandler
	{
		private FirmwareUpdateHandler myDecoratedFimwareUpdateHander;

		private bool isVerbose;

		private string connectionIdentifierString;

		private string firmwareFileIdentifierString;

		private int lastPercentComplete = -1;

		public FirmwareUpdateHandlerVerboseDecorator(bool verboseFlag, string connectionId, string fwFileId, FirmwareUpdateHandler firmwareUpdateHandlerToDecorate)
		{
			this.myDecoratedFimwareUpdateHander = firmwareUpdateHandlerToDecorate;
			this.isVerbose = verboseFlag;
			this.connectionIdentifierString = connectionId;
			this.firmwareFileIdentifierString = fwFileId;
		}

		public override void FirmwareDownloadComplete()
		{
			if (this.isVerbose)
			{
				Console.WriteLine("{0} accepted firmware file {1}", this.connectionIdentifierString, this.firmwareFileIdentifierString);
				Console.WriteLine("Flashing firmware to printer...");
			}
			this.myDecoratedFimwareUpdateHander.firmwareDownloadComplete();
		}

		public override void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion)
		{
			if (this.isVerbose)
			{
				Console.WriteLine("{0} is back online with address {1} and firmware version {2}", this.connectionIdentifierString, (printer == null ? "unknown" : printer.Connection.SimpleConnectionName), firmwareVersion);
			}
			this.myDecoratedFimwareUpdateHander.printerOnline(printer, firmwareVersion);
		}

		public override void ProgressUpdate(int bytesWritten, int totalBytes)
		{
			if (this.isVerbose)
			{
				if (this.lastPercentComplete == -1)
				{
					Console.Write("FW download progress : [{0,20}]", " ");
				}
				int num = (int)((double)bytesWritten / (double)totalBytes * 100);
				if (num == 100)
				{
					Console.Write("{0}", StringUtilities.Repeat("\b", 12));
					Console.Write("{0}", "100");
					Console.WriteLine("{0}]", StringUtilities.Repeat("*", 8));
				}
				else if (this.lastPercentComplete != num)
				{
					this.lastPercentComplete = num;
					int num1 = this.lastPercentComplete / 5;
					string str = string.Format("{0:00}", this.lastPercentComplete);
					if (this.lastPercentComplete % 5 != 0)
					{
						Console.Write("{0}", StringUtilities.Repeat("\b", 12));
						Console.Write("{0,-2}", str);
						if (num1 >= 11)
						{
							Console.Write("{0,-9}]", StringUtilities.Repeat("*", (num1 - 11 >= 0 ? num1 - 11 : 0)));
						}
						else
						{
							Console.Write("{0,-9}]", " ");
						}
					}
					else
					{
						Console.Write("{0}", StringUtilities.Repeat("\b", 21));
						if (num1 >= 10)
						{
							Console.Write("{0,-9}", StringUtilities.Repeat("*", 9));
							Console.Write("{0,-2}", str);
							Console.Write("{0,-9}]", StringUtilities.Repeat("*", (num1 - 11 >= 0 ? num1 - 11 : 0)));
						}
						else
						{
							Console.Write("{0,-9}", StringUtilities.Repeat("*", num1));
							Console.Write("{0,-11}]", str);
						}
					}
				}
			}
			this.myDecoratedFimwareUpdateHander.progressUpdate(bytesWritten, totalBytes);
		}
	}
}