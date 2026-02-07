using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Handler class is used to update status while performing a firmware download and to notify the caller when the printer
	///       has reconnected after restarting.
	///       </summary>
	public abstract class FirmwareUpdateHandler : FirmwareUpdateHandlerBase, PrinterReconnectionHandler
	{
		internal Action firmwareDownloadComplete;

		internal Action<ZebraPrinterLinkOs, string> printerOnline;

		internal Action<int, int> progressUpdate;

		/// <summary>
		///       Default constructor.
		///       </summary>
		public FirmwareUpdateHandler()
		{
			FirmwareUpdateHandler firmwareUpdateHandler = this;
			this.firmwareDownloadComplete = new Action(firmwareUpdateHandler.FirmwareDownloadComplete);
			FirmwareUpdateHandler firmwareUpdateHandler1 = this;
			this.printerOnline = new Action<ZebraPrinterLinkOs, string>(firmwareUpdateHandler1.PrinterOnline);
			FirmwareUpdateHandler firmwareUpdateHandler2 = this;
			this.progressUpdate = new Action<int, int>(firmwareUpdateHandler2.ProgressUpdate);
		}

		/// <summary>
		///       Called when the firmware download completes.
		///       </summary>
		public abstract void FirmwareDownloadComplete();

		/// <summary>
		///       Called when the printer is back online and has been rediscovered.
		///       </summary>
		/// <param name="printer">The printer object which came back online.</param>
		/// <param name="firmwareVersion">The new firmware version on the printer.</param>
		public abstract void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion);

		/// <summary>
		///       Callback to notify the user of the firmware updating progress.
		///       </summary>
		/// <param name="bytesWritten">The total number of bytes written to the printer.</param>
		/// <param name="totalBytes">The total number of bytes to be written to the printer.</param>
		public abstract void ProgressUpdate(int bytesWritten, int totalBytes);
	}
}