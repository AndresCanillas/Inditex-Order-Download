using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class PrinterDefaulter : PrinterOperationBase<object>
	{
		public PrinterDefaulter(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		private void DefaultPrinter()
		{
			if (base.IsPrintingChannelInLineMode())
			{
				SGD.SET(SGDUtilities.PRINTER_DEFAULT, "reload printer", this.connection);
				return;
			}
			(new PrinterCommandImpl(SGDUtilities.PRINTER_DEFAULT_JSON)).SendAndWaitForValidJsonResponse(this.connection);
		}

		public override object Execute()
		{
			this.SelectStatusChannelIfOpen();
			this.DefaultPrinter();
			return null;
		}
	}
}