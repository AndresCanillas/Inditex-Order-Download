using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class PrinterCalibrator : PrinterOperationBase<object>
	{
		public PrinterCalibrator(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		private void Calibrate()
		{
			if (base.IsPrintingChannelInLineMode())
			{
				SGD.SET(SGDUtilities.CALIBRATE_PRINTER, "", this.connection);
				return;
			}
			(new PrinterCommandImpl(SGDUtilities.CALIBRATE_PRINTER_JSON)).SendAndWaitForValidJsonResponse(this.connection);
		}

		public override object Execute()
		{
			this.SelectStatusChannelIfOpen();
			this.Calibrate();
			return null;
		}
	}
}