using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterStatusCpcl : PrinterStatus
	{
		public PrinterStatusCpcl(Connection connection) : base(connection)
		{
		}

		protected override void UpdateStatus()
		{
			byte[] numArray = ((PrinterCommand)(new PrinterCommandImpl(CPCLUtilities.PRINTER_STATUS))).SendAndWaitForResponse(this.printerConnection);
			if ((int)numArray.Length != 1)
			{
				throw new ConnectionException(string.Concat("Malformed status response - unable to determine printer status (received ", (int)numArray.Length, " bytes)"));
			}
			int num = 4;
			int num1 = 2;
			this.labelsRemainingInBatch = 0;
			this.numberOfFormatsInReceiveBuffer = 0;
			this.isPartialFormatInProgress = false;
			this.isHeadCold = false;
			this.isHeadOpen = (numArray[0] & num) == num;
			this.isHeadTooHot = false;
			this.isPaperOut = (numArray[0] & num1) == num1;
			this.isRibbonOut = false;
			this.isReceiveBufferFull = false;
			this.isPaused = false;
			this.labelLengthInDots = 0;
			this.printMode = ZplPrintMode.UNKNOWN;
		}
	}
}