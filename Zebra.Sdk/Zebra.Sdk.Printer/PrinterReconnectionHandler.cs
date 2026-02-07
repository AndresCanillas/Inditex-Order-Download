using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Interface definition for a callback to be invoked when a printer comes back online and has been rediscovered.
	///       </summary>
	public interface PrinterReconnectionHandler
	{
		/// <summary>
		///       Called when the printer is back online and has been rediscovered.
		///       </summary>
		/// <param name="printer">The printer object which came back online.</param>
		/// <param name="firmwareVersion">The new firmware version on the printer.</param>
		void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion);
	}
}