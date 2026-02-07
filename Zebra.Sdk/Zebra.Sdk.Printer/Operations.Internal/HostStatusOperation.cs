using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class HostStatusOperation : PrinterOperationBase<PrinterStatus>
	{
		public HostStatusOperation(Connection connection, PrinterLanguage language) : base(connection, language)
		{
		}

		public override PrinterStatus Execute()
		{
			this.SelectStatusChannelIfOpen();
			if (base.IsPrintingChannelInLineMode())
			{
				return new HostStatusOperation.PrinterStatusLinkOsLineMode(this.connection);
			}
			return new PrinterStatusLinkOs(this.connection);
		}

		internal class PrinterStatusLinkOsLineMode : PrinterStatusLinkOs
		{
			public PrinterStatusLinkOsLineMode(Connection connection) : base(connection)
			{
			}

			protected override byte[] GetStatusInfoFromPrinter()
			{
				return this.printerConnection.SendAndWaitForResponse(Encoding.UTF8.GetBytes(SGDUtilities.DecorateWithGetCommand(SGDUtilities.HOST_STATUS)), this.printerConnection.MaxTimeoutForRead, this.printerConnection.TimeToWaitForMoreData, null);
			}
		}
	}
}