using System;
using Zebra.Sdk.Printer.Discovery.Internal;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Class definition for a callback to be invoked for Link-OS printer discovery events.
	///       </summary>
	public class DiscoveryHandlerLinkOsOnly : DiscoveryHandler
	{
		private DiscoveryHandler myDiscoveryHandler;

		/// <summary>
		///       Creates a DiscoveryHandler which will only report back Link-OS printers.
		///       </summary>
		/// <param name="internalDiscoveryHandler">Base discovery handler for callbacks.</param>
		public DiscoveryHandlerLinkOsOnly(DiscoveryHandler internalDiscoveryHandler)
		{
			this.myDiscoveryHandler = internalDiscoveryHandler;
		}

		/// <summary>
		///       This method is invoked when there is an error during discovery. The discovery will be cancelled when this method 
		///       is invoked. <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandlerLinkOsOnly.DiscoveryFinished" /> will not be called if this method is invoked.
		///       </summary>
		/// <param name="message">The error message.</param>
		public void DiscoveryError(string message)
		{
			this.myDiscoveryHandler.DiscoveryError(message);
		}

		/// <summary>
		///       This method is invoked when discovery is finished.
		///       </summary>
		public void DiscoveryFinished()
		{
			this.myDiscoveryHandler.DiscoveryFinished();
		}

		/// <summary>
		///       This method is invoked when a Link-OS printer has been discovered. This method will be invoked for each printer 
		///       that is found.
		///       </summary>
		/// <param name="printer">A discovered Link-OS printer.</param>
		public void FoundPrinter(DiscoveredPrinter printer)
		{
			if (DiscoveredPrinterNetworkFactory.IsLinkOsPrinter(printer))
			{
				this.myDiscoveryHandler.FoundPrinter(printer);
			}
		}
	}
}