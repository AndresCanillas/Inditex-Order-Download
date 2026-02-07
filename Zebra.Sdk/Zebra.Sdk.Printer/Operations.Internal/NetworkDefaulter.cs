using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class NetworkDefaulter : PrinterOperationBase<object>
	{
		public NetworkDefaulter(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		public override object Execute()
		{
			this.SelectStatusChannelIfOpen();
			this.ResetPrinter();
			return null;
		}

		public void ResetPrinter()
		{
			if (base.IsPrintingChannelInLineMode())
			{
				SGD.SET(SGDUtilities.NETWORK_DEFAULT, "y", this.connection);
				return;
			}
			(new PrinterCommandImpl(SGDUtilities.NETWORK_DEFAULT_JSON)).SendAndWaitForValidJsonResponse(this.connection);
		}
	}
}