using System;

namespace Zebra.Sdk.Comm.Internal
{
	internal class NotMyConnectionDataException : Exception
	{
		public NotMyConnectionDataException(string message) : base(message)
		{
		}
	}
}