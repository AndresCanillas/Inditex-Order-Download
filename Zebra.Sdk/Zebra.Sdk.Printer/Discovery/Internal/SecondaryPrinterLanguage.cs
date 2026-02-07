using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class SecondaryPrinterLanguage : EnumAttributes
	{
		public static SecondaryPrinterLanguage UNKNOWN;

		public static SecondaryPrinterLanguage SGD;

		public static SecondaryPrinterLanguage SNMP;

		private static List<SecondaryPrinterLanguage> possibleLanguages;

		static SecondaryPrinterLanguage()
		{
			SecondaryPrinterLanguage.UNKNOWN = new SecondaryPrinterLanguage(0, "Unknown");
			SecondaryPrinterLanguage.SGD = new SecondaryPrinterLanguage(1, "SGD");
			SecondaryPrinterLanguage.SNMP = new SecondaryPrinterLanguage(2, "SNMP");
			SecondaryPrinterLanguage.possibleLanguages = new List<SecondaryPrinterLanguage>()
			{
				SecondaryPrinterLanguage.UNKNOWN,
				SecondaryPrinterLanguage.SGD,
				SecondaryPrinterLanguage.SNMP
			};
		}

		private SecondaryPrinterLanguage(int value, string description) : base(value, description)
		{
		}

		public static HashSet<SecondaryPrinterLanguage> GetEnumSetFromBitmask(int availableLanguagesBitfield)
		{
			HashSet<SecondaryPrinterLanguage> secondaryPrinterLanguages = new HashSet<SecondaryPrinterLanguage>();
			foreach (SecondaryPrinterLanguage possibleLanguage in SecondaryPrinterLanguage.possibleLanguages)
			{
				if ((availableLanguagesBitfield & possibleLanguage.Value) == 0)
				{
					continue;
				}
				secondaryPrinterLanguages.Add(possibleLanguage);
			}
			return secondaryPrinterLanguages;
		}

		public static SecondaryPrinterLanguage IntToEnum(int bitFieldValue)
		{
			SecondaryPrinterLanguage uNKNOWN = SecondaryPrinterLanguage.UNKNOWN;
			foreach (SecondaryPrinterLanguage possibleLanguage in SecondaryPrinterLanguage.possibleLanguages)
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