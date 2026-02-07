using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Util.Internal
{
	internal class IPAddressUtil
	{
		public IPAddressUtil()
		{
		}

		public static bool IpAddressIsValid(string ipAddress)
		{
			bool count = false;
			count = RegexUtil.GetMatches("^\\s*([\\d]{1,3}.[\\d]{1,3}.[\\d]{1,3}.[\\d]{1,3})\\s*$", ipAddress).Count > 0;
			if (!count)
			{
				count = RegexUtil.GetMatches("^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9\\-]*[a-zA-Z0-9])\\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\\-]*[A-Za-z0-9])$", ipAddress).Count > 0;
			}
			return count;
		}
	}
}