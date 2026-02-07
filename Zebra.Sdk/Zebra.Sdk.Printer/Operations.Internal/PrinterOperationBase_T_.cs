using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal abstract class PrinterOperationBase<T> : PrinterOperation<T>
	{
		protected Connection connection;

		protected PrinterLanguage printerLanguage;

		public PrinterOperationBase(Connection connection, PrinterLanguage language)
		{
			this.connection = connection;
			this.printerLanguage = language;
		}

		protected bool IsPrintingChannelInLineMode()
		{
			if (this.connection is StatusConnection)
			{
				return false;
			}
			return this.printerLanguage == PrinterLanguage.LINE_PRINT;
		}

		protected virtual void SelectStatusChannelIfOpen()
		{
			Connection statusChannel;
			MultichannelConnection multichannelConnection = this.connection as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection != null)
			{
				if (multichannelConnection1.StatusChannel.Connected)
				{
					statusChannel = multichannelConnection1.StatusChannel;
				}
				else
				{
					statusChannel = multichannelConnection1.PrintingChannel;
				}
				this.connection = statusChannel;
			}
		}
	}
}