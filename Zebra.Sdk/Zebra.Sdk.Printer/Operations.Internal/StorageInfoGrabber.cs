using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class StorageInfoGrabber : PrinterOperationCaresAboutLinkOsVersion<List<StorageInfo>>
	{
		public StorageInfoGrabber(Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation) : base(connection, language, linkOsInformation)
		{
		}

		public override List<StorageInfo> Execute()
		{
			this.SelectProperChannel();
			this.IsOkToProceed();
			return this.GetStorageInfo();
		}

		private List<StorageInfo> GetStorageInfo()
		{
			List<StorageInfo> storageInfo;
			FileUtilZpl fileUtilZpl = new FileUtilZpl(this.connection);
			if (!base.IsLinkOs2_5_OrHigher())
			{
				storageInfo = fileUtilZpl.GetStorageInfo();
			}
			else
			{
				storageInfo = (!base.IsPrintingChannelInLineMode() ? fileUtilZpl.GetStorageInfoViaJsonChannel() : fileUtilZpl.GetStorageInfoViaSgd());
			}
			return storageInfo;
		}

		private void IsOkToProceed()
		{
			if (!base.IsLinkOs2_5_OrHigher())
			{
				if (this.connection is StatusConnection)
				{
					throw new ConnectionException("Cannot retrieve storage info over the status channel on this version of firmware");
				}
				if (this.printerLanguage == PrinterLanguage.LINE_PRINT)
				{
					throw new ConnectionException("Cannot retrieve storage info when in line print mode on this version of firmware");
				}
				if (!this.connection.Connected)
				{
					throw new ConnectionException("Cannot retrieve storage info when there is no valid connection");
				}
			}
		}

		protected override void SelectProperChannel()
		{
			if (base.IsLinkOs2_5_OrHigher())
			{
				this.SelectStatusChannelIfOpen();
				return;
			}
			if (this.connection is MultichannelConnection)
			{
				this.connection = ((MultichannelConnection)this.connection).PrintingChannel;
			}
		}
	}
}