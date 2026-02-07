using System;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingRangeIpV4Address : SettingRange
	{
		public SettingRangeIpV4Address(string range)
		{
		}

		public bool IsInRange(string value)
		{
			return IPAddressUtil.IpAddressIsValid(value);
		}
	}
}