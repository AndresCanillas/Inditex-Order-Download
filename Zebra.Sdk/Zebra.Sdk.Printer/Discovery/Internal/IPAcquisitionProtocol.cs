using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class IPAcquisitionProtocol : EnumAttributes
	{
		public static IPAcquisitionProtocol ALL;

		public static IPAcquisitionProtocol GLEAN;

		public static IPAcquisitionProtocol RARP;

		public static IPAcquisitionProtocol BOOTP;

		public static IPAcquisitionProtocol DHCP;

		public static IPAcquisitionProtocol DHCP_AND_BOOTP;

		public static IPAcquisitionProtocol STATIC;

		private static List<EnumAttributes> possibleInterfaces;

		static IPAcquisitionProtocol()
		{
			IPAcquisitionProtocol.ALL = new IPAcquisitionProtocol(0, "All");
			IPAcquisitionProtocol.GLEAN = new IPAcquisitionProtocol(1, "Glean");
			IPAcquisitionProtocol.RARP = new IPAcquisitionProtocol(2, "RARP");
			IPAcquisitionProtocol.BOOTP = new IPAcquisitionProtocol(3, "Bootp");
			IPAcquisitionProtocol.DHCP = new IPAcquisitionProtocol(4, "DHCP");
			IPAcquisitionProtocol.DHCP_AND_BOOTP = new IPAcquisitionProtocol(5, "DHCP and Bootp");
			IPAcquisitionProtocol.STATIC = new IPAcquisitionProtocol(6, "Static");
			IPAcquisitionProtocol.possibleInterfaces = new List<EnumAttributes>()
			{
				IPAcquisitionProtocol.ALL,
				IPAcquisitionProtocol.GLEAN,
				IPAcquisitionProtocol.RARP,
				IPAcquisitionProtocol.BOOTP,
				IPAcquisitionProtocol.DHCP,
				IPAcquisitionProtocol.DHCP_AND_BOOTP,
				IPAcquisitionProtocol.STATIC
			};
		}

		public IPAcquisitionProtocol(int value, string description) : base(value, description)
		{
		}

		public static IPAcquisitionProtocol IntToEnum(int bitFieldValue)
		{
			IPAcquisitionProtocol aLL = IPAcquisitionProtocol.ALL;
			foreach (IPAcquisitionProtocol possibleInterface in IPAcquisitionProtocol.possibleInterfaces)
			{
				if (possibleInterface.Value != bitFieldValue)
				{
					continue;
				}
				aLL = possibleInterface;
				return aLL;
			}
			return aLL;
		}
	}
}