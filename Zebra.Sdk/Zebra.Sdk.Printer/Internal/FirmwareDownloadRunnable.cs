using System;
using System.Runtime.CompilerServices;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FirmwareDownloadRunnable
	{
		private Connection zebraPrinterConnection;

		private string firmwareFilePath;

		private FirmwareUpdateHandlerBase handler;

		private Exception exceptionCaughtDuringRun;

		public string ExceptionMessage
		{
			get
			{
				return this.exceptionCaughtDuringRun.Message;
			}
		}

		public bool ExceptionOccured
		{
			get
			{
				return this.exceptionCaughtDuringRun != null;
			}
		}

		public FirmwareDownloadRunnable(Connection zebraPrinterConnection, string firmwareFilePath, FirmwareUpdateHandlerBase handler)
		{
			this.zebraPrinterConnection = zebraPrinterConnection;
			this.firmwareFilePath = firmwareFilePath;
			this.handler = handler;
		}

		public void Run()
		{
			try
			{
				ZebraPrinterFactory.GetInstance(this.zebraPrinterConnection).SendFileContents(this.firmwareFilePath, new FirmwareDownloadRunnable.DownloadProgressMonitor()
				{
					updateProgress = (int bytesWritten, int totalBytes) => ((FirmwareUpdateHandler)this.handler).progressUpdate(bytesWritten, totalBytes)
				});
			}
			catch (Exception exception)
			{
				this.exceptionCaughtDuringRun = exception;
				if (this.zebraPrinterConnection.Connected)
				{
					try
					{
						this.zebraPrinterConnection.Close();
					}
					catch (ConnectionException)
					{
					}
				}
			}
		}

		private class DownloadProgressMonitor : ProgressMonitor
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