using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Handler class is used to update status while performing a firmware download.
	///       </summary>
	public interface FirmwareUpdateHandlerBase
	{
		/// <summary>
		///       Called when the firmware download completes. The printer will then begin flashing the firmware to memory followed
		///       by a reboot.
		///       </summary>
		void FirmwareDownloadComplete();

		/// <summary>
		///       Callback to notify the user of the firmware updating progress.
		///       </summary>
		/// <param name="bytesWritten">Total number of bytes written to the printer.</param>
		/// <param name="totalBytes">Total number of bytes to be written to the printer.</param>
		void ProgressUpdate(int bytesWritten, int totalBytes);
	}
}