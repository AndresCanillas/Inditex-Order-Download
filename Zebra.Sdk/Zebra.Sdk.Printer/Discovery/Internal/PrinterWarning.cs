using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrinterWarning : EnumAttributes
	{
		public static PrinterWarning NONE;

		public static PrinterWarning HEAD_UNDER_TEMP;

		public static PrinterWarning RIBBON_IN;

		public static PrinterWarning BATTERY_LOW;

		public static PrinterWarning RFID_ERROR;

		private static List<PrinterWarning> possibleErrors;

		static PrinterWarning()
		{
			PrinterWarning.NONE = new PrinterWarning(0, 0, "None");
			PrinterWarning.HEAD_UNDER_TEMP = new PrinterWarning(2, 4096, "Head Cold");
			PrinterWarning.RIBBON_IN = new PrinterWarning(2, 8192, "Ribbon In");
			PrinterWarning.BATTERY_LOW = new PrinterWarning(2, 16384, "Battery Low");
			PrinterWarning.RFID_ERROR = new PrinterWarning(2, 32768, "RFID Error");
			PrinterWarning.possibleErrors = new List<PrinterWarning>()
			{
				PrinterWarning.NONE,
				PrinterWarning.HEAD_UNDER_TEMP,
				PrinterWarning.RIBBON_IN,
				PrinterWarning.BATTERY_LOW,
				PrinterWarning.RFID_ERROR
			};
		}

		private PrinterWarning(int segment, int value, string description) : base(segment, value, description)
		{
		}

		public static HashSet<PrinterWarning> GetEnumSetFromBitmask(int segment, int availableWarningBitfield)
		{
			HashSet<PrinterWarning> printerWarnings = new HashSet<PrinterWarning>();
			foreach (PrinterWarning possibleError in PrinterWarning.possibleErrors)
			{
				if ((availableWarningBitfield & possibleError.Value) == 0 || possibleError.Segment != segment)
				{
					continue;
				}
				printerWarnings.Add(possibleError);
			}
			return printerWarnings;
		}

		public static PrinterWarning IntToEnum(int segment, int bitFieldValue)
		{
			PrinterWarning nONE = PrinterWarning.NONE;
			foreach (PrinterWarning possibleError in PrinterWarning.possibleErrors)
			{
				if (possibleError.Value != bitFieldValue || possibleError.Segment != segment)
				{
					continue;
				}
				nONE = possibleError;
				return nONE;
			}
			return nONE;
		}
	}
}