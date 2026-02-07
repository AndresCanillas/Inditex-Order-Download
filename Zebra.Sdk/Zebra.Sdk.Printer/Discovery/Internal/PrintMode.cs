using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrintMode : EnumAttributes
	{
		public static PrintMode REWIND;

		public static PrintMode TEAR_OFF;

		public static PrintMode PEEL_OFF;

		public static PrintMode PACE;

		public static PrintMode CUTTER;

		public static PrintMode DELAYED_CUT;

		public static PrintMode APPLICATOR;

		public static PrintMode LINERLESS_PEEL;

		public static PrintMode LINERLESS_REWIND;

		public static PrintMode PARTIAL_CUTTER;

		public static PrintMode RFID;

		public static PrintMode LINERLESS_TEAR;

		private static List<PrintMode> possibleMethods;

		static PrintMode()
		{
			PrintMode.REWIND = new PrintMode(0, "Rewind");
			PrintMode.TEAR_OFF = new PrintMode(1, "Tear Off");
			PrintMode.PEEL_OFF = new PrintMode(2, "Peel Off");
			PrintMode.PACE = new PrintMode(3, "Pace");
			PrintMode.CUTTER = new PrintMode(4, "Cutter");
			PrintMode.DELAYED_CUT = new PrintMode(5, "Delayed Cuts");
			PrintMode.APPLICATOR = new PrintMode(6, "Applicator");
			PrintMode.LINERLESS_PEEL = new PrintMode(7, "Linerless Peel");
			PrintMode.LINERLESS_REWIND = new PrintMode(8, "Linerless Rewind");
			PrintMode.PARTIAL_CUTTER = new PrintMode(9, "Partial Cutter");
			PrintMode.RFID = new PrintMode(10, "RFID");
			PrintMode.LINERLESS_TEAR = new PrintMode(11, "Linerless Tear");
			PrintMode.possibleMethods = new List<PrintMode>()
			{
				PrintMode.REWIND,
				PrintMode.TEAR_OFF,
				PrintMode.PEEL_OFF,
				PrintMode.PACE,
				PrintMode.CUTTER,
				PrintMode.DELAYED_CUT,
				PrintMode.APPLICATOR,
				PrintMode.LINERLESS_PEEL,
				PrintMode.LINERLESS_REWIND,
				PrintMode.PARTIAL_CUTTER,
				PrintMode.RFID,
				PrintMode.LINERLESS_TEAR
			};
		}

		private PrintMode(int value, string description) : base(value, description)
		{
		}

		public static PrintMode IntToEnum(int value)
		{
			PrintMode rEWIND = PrintMode.REWIND;
			foreach (PrintMode possibleMethod in PrintMode.possibleMethods)
			{
				if (possibleMethod.Value != value)
				{
					continue;
				}
				rEWIND = possibleMethod;
				return rEWIND;
			}
			return rEWIND;
		}
	}
}