using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class PrinterOperationCaresAboutLinkOsVersion<T> : PrinterOperationBase<T>
	{
		protected LinkOsInformation linkOsInformation;

		public PrinterOperationCaresAboutLinkOsVersion(Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation) : base(connection, language)
		{
			this.linkOsInformation = linkOsInformation;
		}

		protected bool IsLinkOs2_5_OrHigher()
		{
			if (this.linkOsInformation.Major == 2)
			{
				return this.linkOsInformation.Minor >= 5;
			}
			if (this.linkOsInformation.Major >= 3)
			{
				return true;
			}
			return false;
		}

		protected virtual void SelectProperChannel()
		{
			if (this.connection is MultichannelConnection)
			{
				if (this.IsLinkOs2_5_OrHigher())
				{
					this.SelectStatusChannelIfOpen();
					return;
				}
				this.connection = ((MultichannelConnection)this.connection).PrintingChannel;
			}
		}
	}
}