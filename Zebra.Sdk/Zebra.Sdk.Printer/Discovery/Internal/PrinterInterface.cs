using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrinterInterface : EnumAttributes
	{
		public static PrinterInterface UNKNOWN;

		public static PrinterInterface INTERNAL_WIRED;

		public static PrinterInterface EXTERNAL_WIRED;

		public static PrinterInterface WIRELESS;

		public static PrinterInterface BLUETOOTH;

		public static PrinterInterface PARALLEL;

		public static PrinterInterface SERIAL;

		public static PrinterInterface USB;

		public static PrinterInterface SD_CARD;

		public static PrinterInterface BATTERY;

		private static List<EnumAttributes> possibleInterfaces;

		static PrinterInterface()
		{
			PrinterInterface.UNKNOWN = new PrinterInterface(0, "Unknown");
			PrinterInterface.INTERNAL_WIRED = new PrinterInterface(1, "Internal Wired");
			PrinterInterface.EXTERNAL_WIRED = new PrinterInterface(2, "External Wired");
			PrinterInterface.WIRELESS = new PrinterInterface(4, "Wireless");
			PrinterInterface.BLUETOOTH = new PrinterInterface(8, "Bluetooth");
			PrinterInterface.PARALLEL = new PrinterInterface(16, "Parallel");
			PrinterInterface.SERIAL = new PrinterInterface(32, "Serial");
			PrinterInterface.USB = new PrinterInterface(64, "USB");
			PrinterInterface.SD_CARD = new PrinterInterface(128, "SD Card");
			PrinterInterface.BATTERY = new PrinterInterface(256, "Battery");
			PrinterInterface.possibleInterfaces = new List<EnumAttributes>()
			{
				PrinterInterface.UNKNOWN,
				PrinterInterface.INTERNAL_WIRED,
				PrinterInterface.EXTERNAL_WIRED,
				PrinterInterface.WIRELESS,
				PrinterInterface.BLUETOOTH,
				PrinterInterface.PARALLEL,
				PrinterInterface.SERIAL,
				PrinterInterface.USB,
				PrinterInterface.SD_CARD,
				PrinterInterface.BATTERY
			};
		}

		public PrinterInterface(int value, string description) : base(value, description)
		{
		}

		public static HashSet<PrinterInterface> GetEnumSetFromBitmask(int availableInterfacesBitfield)
		{
			HashSet<PrinterInterface> printerInterfaces = new HashSet<PrinterInterface>();
			foreach (PrinterInterface possibleInterface in PrinterInterface.possibleInterfaces)
			{
				if ((availableInterfacesBitfield & possibleInterface.Value) == 0)
				{
					continue;
				}
				printerInterfaces.Add(possibleInterface);
			}
			return printerInterfaces;
		}

		public static PrinterInterface IntToEnum(int bitFieldValue)
		{
			PrinterInterface uNKNOWN = PrinterInterface.UNKNOWN;
			foreach (PrinterInterface possibleInterface in PrinterInterface.possibleInterfaces)
			{
				if (possibleInterface.Value != bitFieldValue)
				{
					continue;
				}
				uNKNOWN = possibleInterface;
				return uNKNOWN;
			}
			return uNKNOWN;
		}
	}
}