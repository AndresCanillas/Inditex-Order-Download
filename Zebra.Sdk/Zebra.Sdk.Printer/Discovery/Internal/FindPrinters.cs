using System;
using System.Net;
using System.Threading.Tasks;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class FindPrinters : BroadcastA
	{
		private readonly string ESI_REGISTERED_MULTICAST_GROUP_ADDRESS = "224.0.1.55";

		private readonly string LOCAL_BROADCAST_ADDRESS = "255.255.255.255";

		public FindPrinters() : base(BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY)
		{
		}

		protected override bool DoDiscovery()
		{
			try
			{
				Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(this.LOCAL_BROADCAST_ADDRESS);
				hostAddressesAsync.Wait();
				this.broadcastIpAddresses = hostAddressesAsync.Result;
				if (base.DoDiscovery())
				{
					hostAddressesAsync = Dns.GetHostAddressesAsync(this.ESI_REGISTERED_MULTICAST_GROUP_ADDRESS);
					hostAddressesAsync.Wait();
					this.broadcastIpAddresses = hostAddressesAsync.Result;
					return base.DoDiscovery();
				}
			}
			catch (Exception)
			{
				this.discoveryHandler.DiscoveryError("Unknown host address");
			}
			return false;
		}

		protected override void SetSocketOptions(ZebraDiscoSocket sock)
		{
			try
			{
				sock.SetTimeToLive(3);
			}
			catch (Exception exception)
			{
				throw new DiscoveryException(exception.Message);
			}
		}
	}
}