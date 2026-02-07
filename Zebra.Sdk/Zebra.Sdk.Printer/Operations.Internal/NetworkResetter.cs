using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class NetworkResetter : PrinterOperationBase<object>
	{
		public NetworkResetter(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		public override object Execute()
		{
			this.SelectStatusChannelIfOpen();
			this.ResetPrinter();
			return null;
		}

		private void ResetPrinter()
		{
			if (base.IsPrintingChannelInLineMode())
			{
				SGD.SET(SGDUtilities.NETWORK_RESET, "y", this.connection);
				return;
			}
			(new PrinterCommandImpl(SGDUtilities.NETWORK_RESET_JSON)).SendAndWaitForValidJsonResponse(this.connection);
		}
	}
}