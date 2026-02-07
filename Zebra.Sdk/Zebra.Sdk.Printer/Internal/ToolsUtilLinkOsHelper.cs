using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Operations.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ToolsUtilLinkOsHelper
	{
		private Connection connection;

		private PrinterLanguage printerLanguage;

		public ToolsUtilLinkOsHelper(Connection connection, PrinterLanguage printerLanguage)
		{
			this.connection = connection;
			this.printerLanguage = printerLanguage;
		}

		public void PrintDirectoryLabel()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.PRINTER_DIRECTORY_LABEL));
		}

		public void PrintNetworkConfigurationLabel()
		{
			this.connection.Write(Encoding.UTF8.GetBytes(ZPLUtilities.PRINTER_NETWORK_CONFIG_LABEL));
		}

		public void SetClock(string dateTime)
		{
			(new ClockSetter(dateTime, this.connection, this.printerLanguage)).Execute();
		}
	}
}