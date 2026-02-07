using System;

namespace Zebra.Sdk.Printer.Internal
{
	internal class UseDefaultMappingException : Exception
	{
		public UseDefaultMappingException(string message) : base(message)
		{
		}

		public UseDefaultMappingException(string message, Exception cause) : base(message, cause)
		{
		}
	}
}