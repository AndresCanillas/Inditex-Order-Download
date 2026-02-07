using System;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Defines methods used to reestablish a connection to a printer which may have been closed.
	///       </summary>
	public interface ConnectionReestablisher
	{
		/// <summary>
		///       Reestablishes a connection to a printer which may have been closed due to an event, like a reboot.
		///       </summary>
		/// <param name="handler">Handles recreating and opening a connection to a printe.r</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If the printer cannot be found.</exception>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection can not be created or open.</exception>
		/// <exception cref="T:System.TimeoutException">If a connection can not be reestablished after a defined timeout</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the connection can not talk to the printer.</exception>
		void ReestablishConnection(PrinterReconnectionHandler handler);
	}
}