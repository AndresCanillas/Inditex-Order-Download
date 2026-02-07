using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ZebraPrinterFactoryHelper
	{
		private ZebraPrinterFactoryHelper()
		{
		}

		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter)
		{
			if (genericPrinter is ZebraPrinterCpcl)
			{
				return null;
			}
			return (new LinkOsPrinterCreatorSgdOrJson(genericPrinter.PrinterControlLanguage)).Create(genericPrinter);
		}

		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter, LinkOsInformation info)
		{
			return (new LinkOsPrinterCreatorSgdOrJson(info)).Create(genericPrinter);
		}

		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter, PrinterLanguage language)
		{
			return (new LinkOsPrinterCreatorSgdOrJson(language)).Create(genericPrinter);
		}

		public static ZebraPrinterLinkOs CreateLinkOsPrinter(ZebraPrinter genericPrinter, LinkOsInformation info, PrinterLanguage language)
		{
			if (genericPrinter is ZebraPrinterCpcl && genericPrinter.PrinterControlLanguage == PrinterLanguage.CPCL)
			{
				return null;
			}
			return (new LinkOsPrinterCreatorSgdOrJson(info, language)).Create(genericPrinter);
		}

		private static string GetApplNameHocusPocus(Connection connection)
		{
			string i;
			int num = 0;
			for (i = ""; i.Length == 0; i = SGD.GET(SGDUtilities.APPL_NAME, connection))
			{
				int num1 = num;
				num = num1 + 1;
				if (num1 >= 10)
				{
					break;
				}
			}
			string str = SGD.GET(SGDUtilities.APPL_NAME, connection);
			if (str.Length <= 0)
			{
				return i;
			}
			return str;
		}

		public static ZebraPrinter GetInstance(Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetInstance(CPCLUtilities.VERSION_PREFIXES, connection);
		}

		public static ZebraPrinter GetInstance(string[] cpclFwVersionPrefixes, Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetInstance(ZebraPrinterFactoryHelper.GetLanguage(connection, cpclFwVersionPrefixes), connection);
		}

		public static ZebraPrinter GetInstance(PrinterLanguage language, Connection connection)
		{
			return ZebraPrinterFactoryHelper.GetPrinterInstance(connection, language);
		}

		private static PrinterLanguage GetLanguage(Connection connection, string[] cpclFwVersionPrefixes)
		{
			PrinterLanguage zPL = PrinterLanguage.ZPL;
			connection = ConnectionUtil.SelectConnection(connection);
			if (!connection.Connected)
			{
				throw new ConnectionException("Connection is not open.");
			}
			string applNameHocusPocus = ZebraPrinterFactoryHelper.GetApplNameHocusPocus(connection);
			if (applNameHocusPocus.Length == 0)
			{
				throw new ZebraPrinterLanguageUnknownException();
			}
			if (StringUtilities.DoesPrefixExistInArray(cpclFwVersionPrefixes, applNameHocusPocus))
			{
				zPL = PrinterLanguage.CPCL;
			}
			return zPL;
		}

		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection)
		{
			return (new LinkOsPrinterCreatorSgdOrJson((PrinterLanguage)null)).Create(connection);
		}

		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection, LinkOsInformation info)
		{
			return (new LinkOsPrinterCreatorSgdOrJson(info)).Create(connection);
		}

		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection, PrinterLanguage language)
		{
			return (new LinkOsPrinterCreatorSgdOrJson(language)).Create(connection);
		}

		public static ZebraPrinterLinkOs GetLinkOsPrinter(Connection connection, LinkOsInformation info, PrinterLanguage language)
		{
			return (new LinkOsPrinterCreatorSgdOrJson(info, language)).Create(connection);
		}

		private static ZebraPrinter GetPrinterInstance(Connection connection, PrinterLanguage language)
		{
			ZebraPrinter zebraPrinterCpcl;
			if (language == PrinterLanguage.CPCL || language == PrinterLanguage.LINE_PRINT)
			{
				zebraPrinterCpcl = new ZebraPrinterCpcl(connection, language);
			}
			else
			{
				zebraPrinterCpcl = new ZebraPrinterZpl(connection);
			}
			return zebraPrinterCpcl;
		}
	}
}