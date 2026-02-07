using System;

namespace Zebra.Sdk.Comm.Snmp.Internal
{
	internal class SnmpTimeoutException : Exception
	{
		public SnmpTimeoutException()
		{
		}

		public SnmpTimeoutException(string message) : base(message)
		{
		}
	}
}