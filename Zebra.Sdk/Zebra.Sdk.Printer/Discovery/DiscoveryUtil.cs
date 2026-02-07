using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Defines functions used when discovering information about a printer.
	///       </summary>
	public class DiscoveryUtil
	{
		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Printer.Discovery.DiscoveryUtil</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public DiscoveryUtil()
		{
		}

		/// <summary>
		///       Reads the discovery packet from the provided connection and returns a discovery data map
		///       </summary>
		/// <param name="connection">A <see cref="T:Zebra.Sdk.Comm.Connection" /> to a printer</param>
		/// <returns>A discovery data map representative of the provided packet</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryPacketDecodeException">If provided a malformed discovery packet</exception>
		public static Dictionary<string, string> GetDiscoveryDataMap(Connection connection)
		{
			return DiscoveryUtil.ParseDiscoveryPacket(SGD.GET(SGDUtilities.DISCOVERY_NAME, connection));
		}

		/// <summary>
		///       Decodes the provided MIME encoded discovery packet and returns a discovery data map
		///       </summary>
		/// <param name="discoveryPacketMimed">A Base64 encoded discovery packet</param>
		/// <returns>A discovery data map representative of the provided packet</returns>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryPacketDecodeException">If provided a malformed discovery packet</exception>
		public static Dictionary<string, string> ParseDiscoveryPacket(string discoveryPacketMimed)
		{
			if (discoveryPacketMimed == null)
			{
				throw new DiscoveryPacketDecodeException("Unable to parse the supplied discovery packet due to an invalid discovery packet length");
			}
			if (discoveryPacketMimed.Contains(":"))
			{
				discoveryPacketMimed = discoveryPacketMimed.Substring(0, discoveryPacketMimed.IndexOf(':'));
			}
			return DiscoveredPrinterNetworkFactory.GetDiscoveredPrinterNetwork(Convert.FromBase64String(discoveryPacketMimed)).DiscoveryDataMap;
		}
	}
}