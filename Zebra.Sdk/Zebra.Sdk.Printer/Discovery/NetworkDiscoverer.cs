using System;
using System.Collections.Generic;
using Zebra.Sdk.Printer.Discovery.Internal;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       A class used to discover printers on an IP Network.
	///       </summary>
	public class NetworkDiscoverer
	{
		private NetworkDiscoverer()
		{
		}

		/// <summary>
		///       Sends a directed broadcast discovery packet to the subnet specified by <c>ipAddress</c>.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="ipAddress">The IP address of the subnet.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void DirectedBroadcast(DiscoveryHandler discoveryHandler, string ipAddress)
		{
			(new DirectedBroadcast(ipAddress)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a directed broadcast discovery packet to the subnet specified by <c>ipAddress</c>.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="ipAddress">The IP address of the subnet.</param>
		/// <param name="waitForResponsesTimeout">Time to wait, in milliseconds, before determining that there are no more discovery responses.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void DirectedBroadcast(DiscoveryHandler discoveryHandler, string ipAddress, int waitForResponsesTimeout)
		{
			(new DirectedBroadcast(ipAddress, waitForResponsesTimeout)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       This method will search the network using a combination of discovery methods to find printers on the network.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void FindPrinters(DiscoveryHandler discoveryHandler)
		{
			(new FindPrinters()).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a discovery request to the list of printer DNS names or IPs in <c>printersToFind</c>.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="printersToFind">A list of IP addresses or DNS names for the printers to be discovered.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void FindPrinters(DiscoveryHandler discoveryHandler, List<string> printersToFind)
		{
			(new PrinterNameSearch(discoveryHandler, printersToFind)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a discovery request to the list of printer DNS names or IPs in <c>printersToFind</c>.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="printersToFind">A list of IP addresses or DNS names for the printers to be discovered.</param>
		/// <param name="waitForResponsesTimeout">Time to wait, in milliseconds, before determining that there are no more discovery responses.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void FindPrinters(DiscoveryHandler discoveryHandler, List<string> printersToFind, int waitForResponsesTimeout)
		{
			(new PrinterNameSearch(discoveryHandler, printersToFind, waitForResponsesTimeout)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a local broadcast packet.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void LocalBroadcast(DiscoveryHandler discoveryHandler)
		{
			(new LocalBroadcast()).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a local broadcast packet.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="waitForResponsesTimeout">Time to wait, in milliseconds, before determining that there are no more discovery responses.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void LocalBroadcast(DiscoveryHandler discoveryHandler, int waitForResponsesTimeout)
		{
			(new LocalBroadcast(waitForResponsesTimeout)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a multicast discovery packet.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="hops">The number of hops.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void Multicast(DiscoveryHandler discoveryHandler, int hops)
		{
			(new MulticastBroadcast(hops)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a multicast discovery packet.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="hops">The number of hops.</param>
		/// <param name="waitForResponsesTimeout">Time to wait, in milliseconds, before determining that there are no more discovery responses.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void Multicast(DiscoveryHandler discoveryHandler, int hops, int waitForResponsesTimeout)
		{
			(new MulticastBroadcast(hops, waitForResponsesTimeout)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a discovery packet to the IPs specified in the <c>subnetRange</c>.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="subnetRange">The subnet search range.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void SubnetSearch(DiscoveryHandler discoveryHandler, string subnetRange)
		{
			(new SubnetSearch(subnetRange)).DoBroadcast(discoveryHandler);
		}

		/// <summary>
		///       Sends a discovery packet to the IPs specified in the <c>subnetRange</c>.
		///       </summary>
		/// <param name="discoveryHandler">A <see cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryHandler" /> instance that is used to handle discovery events (e.g. found a printer, errors, discovery finished).</param>
		/// <param name="subnetRange">The subnet search range.</param>
		/// <param name="waitForResponsesTimeout">Time to wait, in milliseconds, before determining that there are no more discovery responses.</param>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while starting the discovery (errors during discovery will be sent via <see cref="M:Zebra.Sdk.Printer.Discovery.DiscoveryHandler.DiscoveryError(System.String)" />.</exception>
		public static void SubnetSearch(DiscoveryHandler discoveryHandler, string subnetRange, int waitForResponsesTimeout)
		{
			(new SubnetSearch(subnetRange, waitForResponsesTimeout)).DoBroadcast(discoveryHandler);
		}
	}
}