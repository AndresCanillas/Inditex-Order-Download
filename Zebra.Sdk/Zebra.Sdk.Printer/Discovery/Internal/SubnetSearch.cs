using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class SubnetSearch : BroadcastA
	{
		private readonly static int IP_SEARCH_RANGE_LOW;

		private readonly static int IP_SEARCH_RANGE_HIGH;

		private readonly static string EXCEPTION_STRING;

		static SubnetSearch()
		{
			SubnetSearch.IP_SEARCH_RANGE_LOW = 1;
			SubnetSearch.IP_SEARCH_RANGE_HIGH = 254;
			SubnetSearch.EXCEPTION_STRING = "Malformed subnet search address";
		}

		public SubnetSearch(string ipSearchRange) : this(ipSearchRange, BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY)
		{
		}

		public SubnetSearch(string ipSearchRange, int waitForResponsesTimeout) : base(waitForResponsesTimeout)
		{
			this.broadcastIpAddresses = SubnetSearch.GetAddressesToSearch(ipSearchRange);
		}

		private static IPAddress[] CreateSearchList(Match matcher)
		{
			string value = matcher.Groups[2].Value;
			if (string.IsNullOrEmpty(value))
			{
				throw new DiscoveryException(SubnetSearch.EXCEPTION_STRING);
			}
			int pSEARCHRANGELOW = SubnetSearch.IP_SEARCH_RANGE_LOW;
			int pSEARCHRANGEHIGH = SubnetSearch.IP_SEARCH_RANGE_HIGH;
			if (!value.Equals("*"))
			{
				pSEARCHRANGELOW = SubnetSearch.SetLowValue(matcher.Groups[4].Value);
				pSEARCHRANGEHIGH = SubnetSearch.SetHighValue(matcher.Groups[6].Value, pSEARCHRANGELOW);
				if (!SubnetSearch.IsRangeValid(pSEARCHRANGELOW, pSEARCHRANGEHIGH))
				{
					throw new DiscoveryException(SubnetSearch.EXCEPTION_STRING);
				}
			}
			List<IPAddress> pAddresses = new List<IPAddress>();
			for (int i = pSEARCHRANGELOW; i <= pSEARCHRANGEHIGH; i++)
			{
				try
				{
					pAddresses.Add(IPAddress.Parse(string.Concat(matcher.Groups[1], ".", i)));
				}
				catch (Exception)
				{
					throw new DiscoveryException(SubnetSearch.EXCEPTION_STRING);
				}
			}
			return pAddresses.ToArray<IPAddress>();
		}

		private static IPAddress[] GetAddressesToSearch(string searchString)
		{
			if (searchString == null)
			{
				throw new DiscoveryException(SubnetSearch.EXCEPTION_STRING);
			}
			Match match = (new Regex("^([0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3})\\.?((([0-9]{1,3})(\\-([0-9]{1,3}|\\*))?)|\\*)?$")).Match(searchString);
			if (!match.Success)
			{
				throw new DiscoveryException(SubnetSearch.EXCEPTION_STRING);
			}
			return SubnetSearch.CreateSearchList(match);
		}

		private static bool IsHighValueIsPresent(string highValueString)
		{
			if (highValueString == null)
			{
				return false;
			}
			return highValueString.Length > 0;
		}

		private static bool IsRangeValid(int low, int high)
		{
			if (low < SubnetSearch.IP_SEARCH_RANGE_LOW || low > SubnetSearch.IP_SEARCH_RANGE_HIGH || high < SubnetSearch.IP_SEARCH_RANGE_LOW || high > SubnetSearch.IP_SEARCH_RANGE_HIGH)
			{
				return false;
			}
			return low <= high;
		}

		private static int SetHighValue(string highValueString, int defaultValueIfNotPresent)
		{
			int num;
			num = (!SubnetSearch.IsHighValueIsPresent(highValueString) ? defaultValueIfNotPresent : SubnetSearch.SetHighValueWhichIsPresent(highValueString));
			return num;
		}

		private static int SetHighValueWhichIsPresent(string highValueString)
		{
			int num;
			num = (!highValueString.Equals("*") ? int.Parse(highValueString) : SubnetSearch.IP_SEARCH_RANGE_HIGH);
			return num;
		}

		private static int SetLowValue(string lowValueString)
		{
			if (lowValueString == null)
			{
				throw new DiscoveryException(SubnetSearch.EXCEPTION_STRING);
			}
			return int.Parse(lowValueString);
		}

		protected override void SetSocketOptions(ZebraDiscoSocket sock)
		{
		}
	}
}