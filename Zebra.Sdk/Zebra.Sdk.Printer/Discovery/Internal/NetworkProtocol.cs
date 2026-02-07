using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class NetworkProtocol : EnumAttributes
	{
		public static NetworkProtocol NONE;

		public static NetworkProtocol FTP;

		public static NetworkProtocol LPD;

		public static NetworkProtocol TCP_RAW;

		public static NetworkProtocol UDP_RAW;

		public static NetworkProtocol HTTP;

		public static NetworkProtocol SMTP;

		public static NetworkProtocol POP3;

		public static NetworkProtocol SNMP;

		public static NetworkProtocol TELNET;

		public static NetworkProtocol WEBLINK;

		public static NetworkProtocol TLS;

		public static NetworkProtocol HTTPS;

		private static List<EnumAttributes> possibleProtocols;

		static NetworkProtocol()
		{
			NetworkProtocol.NONE = new NetworkProtocol(0, "None");
			NetworkProtocol.FTP = new NetworkProtocol(1, "FTP");
			NetworkProtocol.LPD = new NetworkProtocol(2, "LPD");
			NetworkProtocol.TCP_RAW = new NetworkProtocol(4, "TCP");
			NetworkProtocol.UDP_RAW = new NetworkProtocol(8, "UDP");
			NetworkProtocol.HTTP = new NetworkProtocol(16, "HTTP");
			NetworkProtocol.SMTP = new NetworkProtocol(32, "SMTP");
			NetworkProtocol.POP3 = new NetworkProtocol(64, "POP3");
			NetworkProtocol.SNMP = new NetworkProtocol(128, "SNMP");
			NetworkProtocol.TELNET = new NetworkProtocol(256, "Telnet");
			NetworkProtocol.WEBLINK = new NetworkProtocol(512, "Weblink");
			NetworkProtocol.TLS = new NetworkProtocol(1024, "TLS");
			NetworkProtocol.HTTPS = new NetworkProtocol(2048, "HTTPS");
			NetworkProtocol.possibleProtocols = new List<EnumAttributes>()
			{
				NetworkProtocol.NONE,
				NetworkProtocol.FTP,
				NetworkProtocol.LPD,
				NetworkProtocol.TCP_RAW,
				NetworkProtocol.UDP_RAW,
				NetworkProtocol.HTTP,
				NetworkProtocol.SMTP,
				NetworkProtocol.POP3,
				NetworkProtocol.SNMP,
				NetworkProtocol.TELNET,
				NetworkProtocol.WEBLINK,
				NetworkProtocol.TLS,
				NetworkProtocol.HTTPS
			};
		}

		public NetworkProtocol(int value, string description) : base(value, description)
		{
		}

		public static HashSet<NetworkProtocol> GetEnumSetFromBitmask(int availableProtocolBitfields)
		{
			HashSet<NetworkProtocol> networkProtocols = new HashSet<NetworkProtocol>();
			foreach (NetworkProtocol possibleProtocol in NetworkProtocol.possibleProtocols)
			{
				if ((availableProtocolBitfields & possibleProtocol.Value) == 0)
				{
					continue;
				}
				networkProtocols.Add(possibleProtocol);
			}
			return networkProtocols;
		}

		public static NetworkProtocol IntToEnum(int bitFieldValue)
		{
			NetworkProtocol nONE = NetworkProtocol.NONE;
			foreach (NetworkProtocol possibleProtocol in NetworkProtocol.possibleProtocols)
			{
				if (possibleProtocol.Value != bitFieldValue)
				{
					continue;
				}
				nONE = possibleProtocol;
				return nONE;
			}
			return nONE;
		}
	}
}