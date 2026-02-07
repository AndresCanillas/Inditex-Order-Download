using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A factory used to acquire an instance of a ZebraPrinter.
	///       </summary>
	public class ZebraPrinterFactory
	{
		private ZebraPrinterFactory()
		{
		}

		/// <summary>
		///       Create a wrapper around a Zebra printer that provides access to Link-OS features.
		///       </summary>
		/// <param name="genericPrinter">An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter)
		{
			return ZebraPrinterFactoryHelper.CreateLinkOsPrinter(genericPrinter);
		}

		/// <summary>
		///       Create a wrapper around a Zebra printer that provides access to Link-OS features.
		///       </summary>
		/// <param name="genericPrinter">An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></param>
		/// <param name="info">Link-OS Information</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter, LinkOsInformation info)
		{
			return ZebraPrinterFactoryHelper.CreateLinkOsPrinter(genericPrinter, info);
		}

		/// <summary>
		///       Create a wrapper around a Zebra printer that provides access to Link-OS features.
		///       </summary>
		/// <param name="genericPrinter">An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></param>
		/// <param name="language">The printer control language</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter, PrinterLanguage language)
		{
			return ZebraPrinterFactoryHelper.CreateLinkOsPrinter(genericPrinter, language);
		}

		/// <summary>
		///       Create a wrapper around a Zebra printer that provides access to Link-OS features.
		///       </summary>
		/// <param name="genericPrinter">An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></param>
		/// <param name="info">Link-OS Information</param>
		/// <param name="language">The printer control language</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter, LinkOsInformation info, PrinterLanguage language)
		{
			return ZebraPrinterFactoryHelper.CreateLinkOsPrinter(genericPrinter, info, language);
		}

		/// <summary>
		///       Factory method to create the correct <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /> concrete class based on the printer's control language.
		///       </summary>
		/// <param name="connection">An open connection to a printer</param>
		/// <returns>An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language cannot be determined</exception>
		public static ZebraPrinter GetInstance(Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetInstance(connection);
		}

		/// <summary>
		///       Factory method to create the correct <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /> concrete class based on the printer's control language.
		///       </summary>
		/// <param name="cpclFwVersionPrefixes">An array of possible CPCL version number prefixes</param>
		/// <param name="connection">An open connection to a printer</param>
		/// <returns>An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language cannot be determined</exception>
		public static ZebraPrinter GetInstance(string[] cpclFwVersionPrefixes, Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetInstance(cpclFwVersionPrefixes, connection);
		}

		/// <summary>
		///       Factory method to create the correct <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /> concrete class based on the printer's control language.
		///       </summary>
		/// <param name="language">The language of the printer instance to be created</param>
		/// <param name="connection">An open connection to a printer</param>
		/// <returns>An instance of a <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinter GetInstance(PrinterLanguage language, Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetInstance(language, connection);
		}

		/// <summary>
		///       Create Link-OS Zebra printer from a connection that provides access to Link-OS features.
		///       </summary>
		/// <param name="connection">An open connection to a Link-OS printer</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetLinkOsPrinter(connection);
		}

		/// <summary>
		///       Create Link-OS Zebra printer from a connection that provides access to Link-OS features.
		///       </summary>
		/// <param name="connection">An open connection to a Link-OS printer</param>
		/// <param name="info">Link-OS Information</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection, LinkOsInformation info)
		{
			return ZebraPrinterFactoryHelper.GetLinkOsPrinter(connection, info);
		}

		/// <summary>
		///       Create Link-OS Zebra printer from a connection that provides access to Link-OS features.
		///       </summary>
		/// <param name="connection">An open connection to a Link-OS printer</param>
		/// <param name="language">The printer control language</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection, PrinterLanguage language)
		{
			return ZebraPrinterFactoryHelper.GetLinkOsPrinter(connection, language);
		}

		/// <summary>
		///       Create Link-OS Zebra printer from a connection that provides access to Link-OS features.
		///       </summary>
		/// <param name="connection">An open connection to a Link-OS printer</param>
		/// <param name="info">Link-OS Information</param>
		/// <param name="language">The printer control language</param>
		/// <returns>A Link-OS printer</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs</exception>
		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection, LinkOsInformation info, PrinterLanguage language)
		{
			return ZebraPrinterFactoryHelper.GetLinkOsPrinter(connection, info, language);
		}
	}
}