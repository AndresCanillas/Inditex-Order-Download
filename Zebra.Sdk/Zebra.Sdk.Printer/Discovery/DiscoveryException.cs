using System;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Signals that there was an error during discovery.
	///       </summary>
	public class DiscoveryException : Exception
	{
		/// <summary>
		///       Constructs a <c>DiscoveryException</c> with <c>message</c> as the detailed error message.
		///       </summary>
		/// <param name="message">The error message.</param>
		public DiscoveryException(string message) : base(message)
		{
		}
	}
}