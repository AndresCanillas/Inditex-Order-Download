using System;

namespace Zebra.Sdk.Comm.Internal
{
	internal class ConnectionAttributes
	{
		public string snmpSetCommunityName = "public";

		public string snmpGetCommunityName = "public";

		public int snmpMaxRetries = 4;

		public int snmpTimeoutGet = 5000;

		public int snmpTimeoutSet = 5000;

		public ConnectionAttributes()
		{
		}
	}
}