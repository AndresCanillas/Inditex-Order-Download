using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Enumeration of the various printer control languages supported by Zebra Printers.
	///       </summary>
	public class PrinterLanguage
	{
		/// <summary>
		///       Printer control language ZPL
		///       </summary>
		public static PrinterLanguage ZPL;

		/// <summary>
		///       Printer control language CPCL
		///       </summary>
		public static PrinterLanguage CPCL;

		/// <summary>
		///       Printer control language line_print mode.
		///       </summary>
		public static PrinterLanguage LINE_PRINT;

		private static List<PrinterLanguage> possibleLanguages;

		private string description;

		static PrinterLanguage()
		{
			PrinterLanguage.ZPL = new PrinterLanguage("ZPL");
			PrinterLanguage.CPCL = new PrinterLanguage("CPCL");
			PrinterLanguage.LINE_PRINT = new PrinterLanguage("LINE_PRINT");
			PrinterLanguage.possibleLanguages = new List<PrinterLanguage>()
			{
				PrinterLanguage.ZPL,
				PrinterLanguage.CPCL,
				PrinterLanguage.LINE_PRINT
			};
		}

		private PrinterLanguage(string description)
		{
			this.description = description;
		}

		/// <summary>
		///       Converts the string name to the appropriate enum value.
		///       </summary>
		/// <param name="name">The printer control language name (e.g. "zpl", "cpcl", or "line_print")</param>
		/// <returns>The printer language</returns>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language cannot be determined.</exception>
		public static PrinterLanguage GetLanguage(string name)
		{
			if (name == null)
			{
				name = "<null>";
			}
			string upper = name.ToUpper();
			if (upper.Contains("ZPL"))
			{
				upper = "ZPL";
			}
			PrinterLanguage printerLanguage = null;
			foreach (PrinterLanguage possibleLanguage in PrinterLanguage.possibleLanguages)
			{
				if (!Regex.IsMatch(upper, possibleLanguage.ToString()))
				{
					continue;
				}
				printerLanguage = possibleLanguage;
			}
			if (printerLanguage == null)
			{
				throw new ZebraPrinterLanguageUnknownException(string.Concat(name, " is not a valid Zebra printer language"));
			}
			return printerLanguage;
		}

		/// <summary>
		///       The name of the printer language - (e.g. "ZPL" or "CPCL").
		///       </summary>
		/// <returns>ZPL, CPCL, or LINE_PRINT</returns>
		public override string ToString()
		{
			return this.description;
		}
	}
}