using System;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Interface definition for a callback to be invoked for printer discovery events
	///       </summary>
	public interface DiscoveryHandler
	{
		/// <summary>
		///       This method is invoked when there is an error during discovery. The discovery will be cancelled when this method 
		///       is invoked. <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryFinished" /> will not be called if this method is invoked.
		///       </summary>
		/// <param name="message">the error message.</param>
		void DiscoveryError(string message);

		/// <summary>
		///       This method is invoked when discovery is finished.
		///       </summary>
		void DiscoveryFinished();

		/// <summary>
		///       This method is invoked when a printer has been discovered. This method will be invoked for each printer that is found.
		///       </summary>
		/// <param name="printer">a discovered printer.</param>
		void FoundPrinter(DiscoveredPrinter printer);
	}
}