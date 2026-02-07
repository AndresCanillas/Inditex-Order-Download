using System;

namespace Zebra.Sdk.Printer.Internal
{
	internal class MalformedFormatException : Exception
	{
		public MalformedFormatException(string message) : base(message)
		{
		}
	}
}