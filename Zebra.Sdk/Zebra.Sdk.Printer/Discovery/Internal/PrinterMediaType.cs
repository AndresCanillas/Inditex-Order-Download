using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrinterMediaType : EnumAttributes
	{
		public static PrinterMediaType CONTINUOUS;

		public static PrinterMediaType BLACK_MARK;

		public static PrinterMediaType GAP;

		private static List<EnumAttributes> possibleTypes;

		static PrinterMediaType()
		{
			PrinterMediaType.CONTINUOUS = new PrinterMediaType(0, "Continuous");
			PrinterMediaType.BLACK_MARK = new PrinterMediaType(1, "Black Mark");
			PrinterMediaType.GAP = new PrinterMediaType(2, "GAP");
			PrinterMediaType.possibleTypes = new List<EnumAttributes>()
			{
				PrinterMediaType.CONTINUOUS,
				PrinterMediaType.BLACK_MARK,
				PrinterMediaType.GAP
			};
		}

		public PrinterMediaType(int value, string description) : base(value, description)
		{
		}

		public static PrinterMediaType IntToEnum(int value)
		{
			PrinterMediaType gAP = PrinterMediaType.GAP;
			foreach (PrinterMediaType possibleType in PrinterMediaType.possibleTypes)
			{
				if (possibleType.Value != value)
				{
					continue;
				}
				gAP = possibleType;
				return gAP;
			}
			return gAP;
		}
	}
}