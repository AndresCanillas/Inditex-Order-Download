using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterStatusZpl : PrinterStatus
	{
		protected virtual byte LineSeparatorChar
		{
			get
			{
				return (byte)3;
			}
		}

		public PrinterStatusZpl(Connection connection) : base(connection)
		{
		}

		protected virtual int FindStartOfHsResponse(byte[] printerStatusAsByteArray)
		{
			int num = 0;
			while (num < (int)printerStatusAsByteArray.Length && printerStatusAsByteArray[num] != 2)
			{
				num++;
			}
			return num;
		}

		protected string[] GetPrinterStatus()
		{
			byte[] statusInfoFromPrinter = this.GetStatusInfoFromPrinter();
			StringBuilder stringBuilder = new StringBuilder();
			string[] strArrays = new string[0];
			if (statusInfoFromPrinter != null)
			{
				if (this.FindStartOfHsResponse(statusInfoFromPrinter) == (int)statusInfoFromPrinter.Length)
				{
					throw new ConnectionException("Malformed status response - unable to determine printer status");
				}
				byte lineSeparatorChar = this.LineSeparatorChar;
				for (int i = 0; i < (int)statusInfoFromPrinter.Length; i++)
				{
					if (statusInfoFromPrinter[i] == lineSeparatorChar)
					{
						statusInfoFromPrinter[i] = 44;
					}
					if (statusInfoFromPrinter[i] > 31 && statusInfoFromPrinter[i] < 127)
					{
						stringBuilder.Append(Convert.ToString((char)statusInfoFromPrinter[i]));
					}
				}
			}
			if (stringBuilder.Length >= 1)
			{
				strArrays = StringUtilities.Split(stringBuilder.ToString(), ",");
				if ((int)strArrays.Length < 25)
				{
					throw new ConnectionException("Malformed status response - unable to determine printer status");
				}
			}
			return strArrays;
		}

		private static ZplPrintMode GetPrintModeFromHs(char printModeCode)
		{
			char upper = Convert.ToString(printModeCode).ToUpper()[0];
			switch (upper)
			{
				case '0':
				{
					return ZplPrintMode.REWIND;
				}
				case '1':
				{
					return ZplPrintMode.PEEL_OFF;
				}
				case '2':
				{
					return ZplPrintMode.TEAR_OFF;
				}
				case '3':
				{
					return ZplPrintMode.CUTTER;
				}
				case '4':
				{
					return ZplPrintMode.APPLICATOR;
				}
				case '5':
				{
					return ZplPrintMode.DELAYED_CUT;
				}
				case '6':
				{
					return ZplPrintMode.LINERLESS_PEEL;
				}
				case '7':
				{
					return ZplPrintMode.LINERLESS_REWIND;
				}
				case '8':
				{
					return ZplPrintMode.PARTIAL_CUTTER;
				}
				case '9':
				{
					return ZplPrintMode.RFID;
				}
				default:
				{
					if (upper == 'K')
					{
						break;
					}
					else
					{
						return ZplPrintMode.UNKNOWN;
					}
				}
			}
			return ZplPrintMode.KIOSK;
		}

		protected virtual byte[] GetStatusInfoFromPrinter()
		{
			return ((PrinterCommand)(new PrinterCommandImpl(ZPLUtilities.PRINTER_STATUS))).SendAndWaitForResponse(this.printerConnection);
		}

		protected override void UpdateStatus()
		{
			int num = 1;
			int num1 = 2;
			int num2 = 5;
			int num3 = 10;
			int num4 = 11;
			int num5 = 14;
			int num6 = 15;
			int num7 = 20;
			int num8 = 4;
			int num9 = 7;
			int num10 = 3;
			int num11 = 17;
			string[] printerStatus = this.GetPrinterStatus();
			this.labelsRemainingInBatch = int.Parse(printerStatus[num7]);
			this.numberOfFormatsInReceiveBuffer = int.Parse(printerStatus[num8]);
			this.isPartialFormatInProgress = printerStatus[num9].Equals("1");
			this.isHeadCold = printerStatus[num3].Equals("1");
			this.isHeadOpen = printerStatus[num5].Equals("1");
			this.isHeadTooHot = printerStatus[num4].Equals("1");
			this.isPaperOut = printerStatus[num].Equals("1");
			this.isRibbonOut = printerStatus[num6].Equals("1");
			this.isReceiveBufferFull = printerStatus[num2].Equals("1");
			this.isPaused = printerStatus[num1].Equals("1");
			this.labelLengthInDots = int.Parse(printerStatus[num10]);
			this.printMode = PrinterStatusZpl.GetPrintModeFromHs(printerStatus[num11][0]);
		}
	}
}