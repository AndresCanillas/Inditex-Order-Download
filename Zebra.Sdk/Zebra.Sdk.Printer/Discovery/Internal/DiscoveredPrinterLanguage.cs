using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class DiscoveredPrinterLanguage : EnumAttributes
	{
		public static DiscoveredPrinterLanguage UNKNOWN;

		public static DiscoveredPrinterLanguage ZPL;

		public static DiscoveredPrinterLanguage CPCL;

		public static DiscoveredPrinterLanguage EPL;

		private static List<DiscoveredPrinterLanguage> possibleLanguages;

		static DiscoveredPrinterLanguage()
		{
			DiscoveredPrinterLanguage.UNKNOWN = new DiscoveredPrinterLanguage(0, "Unknown");
			DiscoveredPrinterLanguage.ZPL = new DiscoveredPrinterLanguage(1, "ZPL");
			DiscoveredPrinterLanguage.CPCL = new DiscoveredPrinterLanguage(2, "CPCL");
			DiscoveredPrinterLanguage.EPL = new DiscoveredPrinterLanguage(4, "EPL");
			DiscoveredPrinterLanguage.possibleLanguages = new List<DiscoveredPrinterLanguage>()
			{
				DiscoveredPrinterLanguage.UNKNOWN,
				DiscoveredPrinterLanguage.ZPL,
				DiscoveredPrinterLanguage.CPCL,
				DiscoveredPrinterLanguage.EPL
			};
		}

		public DiscoveredPrinterLanguage(int value, string description) : base(value, description)
		{
		}

		public static HashSet<DiscoveredPrinterLanguage> GetEnumSetFromBitmask(int availableLanguagesBitfield)
		{
			HashSet<DiscoveredPrinterLanguage> discoveredPrinterLanguages = new HashSet<DiscoveredPrinterLanguage>();
			foreach (DiscoveredPrinterLanguage possibleLanguage in DiscoveredPrinterLanguage.possibleLanguages)
			{
				if ((availableLanguagesBitfield & possibleLanguage.Value) == 0)
				{
					continue;
				}
				discoveredPrinterLanguages.Add(possibleLanguage);
			}
			return discoveredPrinterLanguages;
		}

		public static DiscoveredPrinterLanguage IntToEnum(int bitFieldValue)
		{
			DiscoveredPrinterLanguage uNKNOWN = DiscoveredPrinterLanguage.UNKNOWN;
			foreach (DiscoveredPrinterLanguage possibleLanguage in DiscoveredPrinterLanguage.possibleLanguages)
			{
				if (possibleLanguage.Value != bitFieldValue)
				{
					continue;
				}
				uNKNOWN = possibleLanguage;
				return uNKNOWN;
			}
			return uNKNOWN;
		}
	}
}