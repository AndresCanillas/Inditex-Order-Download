using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrinterPortStatus : EnumAttributes
	{
		public static PrinterPortStatus NONE;

		public static PrinterPortStatus ONLINE;

		public static PrinterPortStatus OFFLINE;

		public static PrinterPortStatus TONER_LOW;

		public static PrinterPortStatus PAPER_OUT;

		public static PrinterPortStatus PAPER_JAMMED;

		public static PrinterPortStatus DOOR_OPEN;

		public static PrinterPortStatus PRINTER_ERROR;

		public static PrinterPortStatus UNKNOWN;

		private static List<EnumAttributes> possibleStatus;

		static PrinterPortStatus()
		{
			PrinterPortStatus.NONE = new PrinterPortStatus(0, "None");
			PrinterPortStatus.ONLINE = new PrinterPortStatus(1, "Online");
			PrinterPortStatus.OFFLINE = new PrinterPortStatus(2, "Offline");
			PrinterPortStatus.TONER_LOW = new PrinterPortStatus(3, "Toner Low");
			PrinterPortStatus.PAPER_OUT = new PrinterPortStatus(4, "Paper Out");
			PrinterPortStatus.PAPER_JAMMED = new PrinterPortStatus(5, "Paper Jammed");
			PrinterPortStatus.DOOR_OPEN = new PrinterPortStatus(6, "Door Open");
			PrinterPortStatus.PRINTER_ERROR = new PrinterPortStatus(7, "Printer Error");
			PrinterPortStatus.UNKNOWN = new PrinterPortStatus(8, "Unknown");
			PrinterPortStatus.possibleStatus = new List<EnumAttributes>()
			{
				PrinterPortStatus.NONE,
				PrinterPortStatus.ONLINE,
				PrinterPortStatus.OFFLINE,
				PrinterPortStatus.TONER_LOW,
				PrinterPortStatus.PAPER_OUT,
				PrinterPortStatus.PAPER_JAMMED,
				PrinterPortStatus.DOOR_OPEN,
				PrinterPortStatus.PRINTER_ERROR,
				PrinterPortStatus.UNKNOWN
			};
		}

		private PrinterPortStatus(int value, string description) : base(value, description)
		{
		}

		public static PrinterPortStatus IntToEnum(int value)
		{
			PrinterPortStatus uNKNOWN = PrinterPortStatus.UNKNOWN;
			foreach (PrinterPortStatus possibleStatu in PrinterPortStatus.possibleStatus)
			{
				if (possibleStatu.Value != value)
				{
					continue;
				}
				uNKNOWN = possibleStatu;
				return uNKNOWN;
			}
			return uNKNOWN;
		}
	}
}