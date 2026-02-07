using System;
using System.Collections.Generic;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	public class DiscoveredPrinterNetworkFactory
	{
		private readonly static int DISCOVERY_VERSION_OFFSET;

		private readonly static int LEGACY_DISCOVERY_VERSION;

		private readonly static int ADVANCED_DISCOVERY_VERSION;

		static DiscoveredPrinterNetworkFactory()
		{
			DiscoveredPrinterNetworkFactory.DISCOVERY_VERSION_OFFSET = 3;
			DiscoveredPrinterNetworkFactory.LEGACY_DISCOVERY_VERSION = 3;
			DiscoveredPrinterNetworkFactory.ADVANCED_DISCOVERY_VERSION = 4;
		}

		private DiscoveredPrinterNetworkFactory()
		{
		}

		public static DiscoveredPrinterNetwork GetDiscoveredPrinterNetwork(byte[] rawDiscoveryPacket)
		{
			DiscoveredPrinterNetwork discoveredPrinterNetworkAdvanced = null;
			if (rawDiscoveryPacket == null || (int)rawDiscoveryPacket.Length <= DiscoveredPrinterNetworkFactory.DISCOVERY_VERSION_OFFSET)
			{
				throw new DiscoveryPacketDecodeException("Unable to parse the supplied discovery packet due to an invalid discovery packet length");
			}
			int discoveryVersionNumber = DiscoveredPrinterNetworkFactory.GetDiscoveryVersionNumber(rawDiscoveryPacket);
			if (discoveryVersionNumber != DiscoveredPrinterNetworkFactory.LEGACY_DISCOVERY_VERSION)
			{
				if (discoveryVersionNumber != DiscoveredPrinterNetworkFactory.ADVANCED_DISCOVERY_VERSION)
				{
					throw new DiscoveryPacketDecodeException("Unable to parse the supplied discovery packet due to an invalid discovery packet version");
				}
				discoveredPrinterNetworkAdvanced = (new DiscoveryPacketDecoderAdvanced(rawDiscoveryPacket)).GetDiscoveredPrinterNetworkAdvanced();
			}
			else
			{
				discoveredPrinterNetworkAdvanced = (new DiscoveryPacketDecoderLegacy(rawDiscoveryPacket)).GetDiscoveredPrinterNetwork();
			}
			return discoveredPrinterNetworkAdvanced;
		}

		private static int GetDiscoveryVersionNumber(byte[] rawDiscoveryPacket)
		{
			int num = rawDiscoveryPacket[DiscoveredPrinterNetworkFactory.DISCOVERY_VERSION_OFFSET];
			if (num < 0)
			{
				num += 256;
			}
			return num;
		}

		public static bool IsLinkOsPrinter(DiscoveredPrinter printer)
		{
			bool flag = false;
			Dictionary<string, string> discoveryDataMap = printer.DiscoveryDataMap;
			if (discoveryDataMap != null && discoveryDataMap.ContainsKey("LINK_OS_MAJOR_VER"))
			{
				string item = discoveryDataMap["LINK_OS_MAJOR_VER"];
				try
				{
					flag = int.Parse(item) != 0;
				}
				catch (Exception)
				{
				}
			}
			return flag;
		}
	}
}