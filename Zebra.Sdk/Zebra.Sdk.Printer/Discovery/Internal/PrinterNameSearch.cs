using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class PrinterNameSearch : BroadcastA
	{
		public PrinterNameSearch(DiscoveryHandler discoveryHandler, List<string> printersToFind) : this(discoveryHandler, printersToFind, BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY)
		{
		}

		public PrinterNameSearch(DiscoveryHandler discoveryHandler, List<string> printersToFind, int waitForResponsesTimeout) : base(waitForResponsesTimeout)
		{
			if (discoveryHandler == null)
			{
				throw new DiscoveryException("A DiscoveryHandler must be supplied");
			}
			List<IPAddress> pAddresses = new List<IPAddress>();
			foreach (string str in printersToFind)
			{
				try
				{
					pAddresses.Add(IPAddress.Parse(str));
				}
				catch (Exception)
				{
					discoveryHandler.DiscoveryError(string.Concat("An invalid printer name/ip address was provided: ", str));
				}
			}
			this.broadcastIpAddresses = pAddresses.ToArray<IPAddress>();
		}

		protected override void SetSocketOptions(ZebraDiscoSocket sock)
		{
		}
	}
}