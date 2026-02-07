using System;
using System.Net;
using System.Threading.Tasks;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class MulticastBroadcast : BroadcastA
	{
		private readonly string ESI_REGISTERED_MULTICAST_GROUP_ADDRESS = "224.0.1.55";

		private int hops;

		public MulticastBroadcast(int hops) : this(hops, BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY)
		{
		}

		public MulticastBroadcast(int hops, int waitForResponsesTimeout) : base(waitForResponsesTimeout)
		{
			if (hops < 0)
			{
				throw new DiscoveryException(string.Concat(hops, " is an invalid multicast hop argument"));
			}
			this.hops = hops;
			try
			{
				Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(this.ESI_REGISTERED_MULTICAST_GROUP_ADDRESS);
				hostAddressesAsync.Wait();
				this.broadcastIpAddresses = hostAddressesAsync.Result;
			}
			catch (Exception exception)
			{
				throw new DiscoveryException(exception.Message);
			}
		}

		protected override void SetSocketOptions(ZebraDiscoSocket sock)
		{
			try
			{
				sock.JoinGroup(this.ESI_REGISTERED_MULTICAST_GROUP_ADDRESS);
				sock.SetTimeToLive(this.hops);
			}
			catch (Exception exception)
			{
				throw new DiscoveryException(exception.Message);
			}
		}
	}
}