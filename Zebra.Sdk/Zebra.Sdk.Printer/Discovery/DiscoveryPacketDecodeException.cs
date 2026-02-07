using System;

namespace Zebra.Sdk.Printer.Discovery
{
	/// <summary>
	///       Signals that there was an error during discovery packet decoding
	///       </summary>
	public class DiscoveryPacketDecodeException : Exception
	{
		/// <summary>
		///       Constructs a <c>DiscoveryPacketDecodeException</c> with <c>message</c> as the detailed error message.
		///       </summary>
		/// <param name="message">The error message.</param>
		public DiscoveryPacketDecodeException(string message) : base(message)
		{
		}
	}
}