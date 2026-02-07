using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class DirectedBroadcast : BroadcastA
	{
		public DirectedBroadcast(string directedIpAddress) : this(directedIpAddress, BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY)
		{
		}

		public DirectedBroadcast(string directedIpAddress, int waitForResponsesTimeout) : base(waitForResponsesTimeout)
		{
			try
			{
				Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(DirectedBroadcast.GetDirectedBroadcastAddress(directedIpAddress));
				hostAddressesAsync.Wait();
				this.broadcastIpAddresses = hostAddressesAsync.Result;
			}
			catch (Exception)
			{
				throw new DiscoveryException("Malformed directed broadcast address");
			}
		}

		private static string GetDirectedBroadcastAddress(string ipAddress)
		{
			if (ipAddress == null)
			{
				throw new DiscoveryException("Malformed directed broadcast address");
			}
			Match match = (new Regex("^((\\d|[1-9]\\d|1\\d\\d|2([0-4]\\d|5[0-5]))\\.(\\d|[1-9]\\d|1\\d\\d|2([0-4]\\d|5[0-5]))\\.(\\d|[1-9]\\d|1\\d\\d|2([0-4]\\d|5[0-5])))(\\.?|\\.(\\d|[1-9]\\d|1\\d\\d|2([0-4]\\d|5[0-5])))?$")).Match(ipAddress);
			if (!match.Success || match.Groups.Count <= 1)
			{
				throw new DiscoveryException("Malformed directed broadcast address");
			}
			return string.Concat(match.Groups[1].Value, ".255");
		}

		protected override void SetSocketOptions(ZebraDiscoSocket sock)
		{
		}

		public override string ToString()
		{
			return this.broadcastIpAddresses[0].ToString();
		}
	}
}