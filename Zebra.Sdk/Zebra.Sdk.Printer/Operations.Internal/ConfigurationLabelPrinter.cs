using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class ConfigurationLabelPrinter : PrinterOperationCaresAboutLinkOsVersion<object>
	{
		public ConfigurationLabelPrinter(Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation) : base(connection, language, linkOsInformation)
		{
		}

		public override object Execute()
		{
			this.SelectProperChannel();
			this.IsOkToProceed();
			this.PrintConfigurationLabel();
			return null;
		}

		private void IsOkToProceed()
		{
			if (!base.IsLinkOs2_5_OrHigher() && (this.connection is StatusConnection || !this.connection.Connected))
			{
				throw new ConnectionException("Cannot print config label over status channel on this version of firmware");
			}
		}

		private void PrintConfigurationLabel()
		{
			if (!base.IsLinkOs2_5_OrHigher())
			{
				(new ToolsUtilZpl(this.connection)).PrintConfigurationLabel();
				return;
			}
			if (base.IsPrintingChannelInLineMode())
			{
				this.connection.Write(Encoding.UTF8.GetBytes("! U1 setvar \"device.print_out_report\" \"settings\"\r\n"));
				return;
			}
			(new PrinterCommandImpl("{}{\"device.print_out_report\":\"settings\"}")).SendAndWaitForValidJsonResponse(this.connection);
		}
	}
}