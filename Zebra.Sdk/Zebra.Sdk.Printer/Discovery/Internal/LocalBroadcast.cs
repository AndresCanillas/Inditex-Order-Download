using System;
using System.Net;
using System.Threading.Tasks;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class LocalBroadcast : BroadcastA
	{
		private readonly string LOCAL_BROADCAST_ADDRESS = "255.255.255.255";

		public LocalBroadcast() : this(BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY)
		{
		}

		public LocalBroadcast(int waitForResponsesTimeout) : base(waitForResponsesTimeout)
		{
			try
			{
				Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(this.LOCAL_BROADCAST_ADDRESS);
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
		}
	}
}