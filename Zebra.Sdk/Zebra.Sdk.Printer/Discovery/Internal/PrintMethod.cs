using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrintMethod : EnumAttributes
	{
		public static PrintMethod DIRECT_THERMAL;

		public static PrintMethod THERMAL_TRANSFER;

		private static List<PrintMethod> possibleMethods;

		static PrintMethod()
		{
			PrintMethod.DIRECT_THERMAL = new PrintMethod(0, "Direct Thermal");
			PrintMethod.THERMAL_TRANSFER = new PrintMethod(1, "Thermal Transfer");
			PrintMethod.possibleMethods = new List<PrintMethod>()
			{
				PrintMethod.DIRECT_THERMAL,
				PrintMethod.THERMAL_TRANSFER
			};
		}

		private PrintMethod(int value, string description) : base(value, description)
		{
		}

		public static PrintMethod IntToEnum(int value)
		{
			PrintMethod dIRECTTHERMAL = PrintMethod.DIRECT_THERMAL;
			foreach (PrintMethod possibleMethod in PrintMethod.possibleMethods)
			{
				if (possibleMethod.Value != value)
				{
					continue;
				}
				dIRECTTHERMAL = possibleMethod;
				return dIRECTTHERMAL;
			}
			return dIRECTTHERMAL;
		}
	}
}