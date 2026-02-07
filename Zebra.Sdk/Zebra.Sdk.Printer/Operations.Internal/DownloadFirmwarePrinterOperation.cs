using System;
using System.IO;
using System.Runtime.CompilerServices;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class DownloadFirmwarePrinterOperation : PrinterOperationBase<object>
	{
		private Stream firmwareInputStream;

		private int fileSize;

		private FirmwareUpdateHandlerBase handler;

		public DownloadFirmwarePrinterOperation(Connection connection, Stream firmwareInputStream, int fileSize, PrinterLanguage language, FirmwareUpdateHandlerBase handler) : base(connection, language)
		{
			this.firmwareInputStream = firmwareInputStream;
			this.fileSize = fileSize;
			this.handler = handler;
		}

		public override object Execute()
		{
			this.SelectProperChannel();
			this.IsOkToProceed();
			this.UpdateFirmwareUnconditionallyNoReconnect();
			return null;
		}

		private void IsOkToProceed()
		{
			if (this.connection is StatusConnection)
			{
				throw new ConnectionException("Cannot download firmware over status channel");
			}
			if (!this.connection.Connected)
			{
				throw new ConnectionException("The connection is not an open printing channel");
			}
		}

		protected void SelectProperChannel()
		{
			if (this.connection is MultichannelConnection)
			{
				this.connection = ((MultichannelConnection)this.connection).PrintingChannel;
			}
		}

		private void UpdateFirmwareUnconditionallyNoReconnect()
		{
			DownloadFirmwarePrinterOperation.DownloadProgressMonitor downloadProgressMonitor = new DownloadFirmwarePrinterOperation.DownloadProgressMonitor()
			{
				updateProgress = (int bytesWritten, int totalBytes) => ((FirmwareUpdateHandler)this.handler).progressUpdate(bytesWritten, totalBytes)
			};
			FileUtilities.SendFileContentsInChunks(this.connection, downloadProgressMonitor, this.firmwareInputStream, this.fileSize);
			((FirmwareUpdateHandler)this.handler).firmwareDownloadComplete();
			this.connection.Close();
		}

		internal class DownloadProgressMonitor : ProgressMonitor
		{
			public DownloadProgressMonitor()
			{
			}

			public override void UpdateProgress(int bytesWritten, int totalBytes)
			{
				throw new NotImplementedException();
			}
		}
	}
}