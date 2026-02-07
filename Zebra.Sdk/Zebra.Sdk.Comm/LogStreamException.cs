using System;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Signals that an error has occurred while writing to the connections log stream.
	///       </summary>
	public class LogStreamException : ConnectionException
	{
		/// <summary>
		///       Constructs a <c>LogStreamException</c> with <c>message</c> as the detailed error message.
		///       </summary>
		/// <param name="message">the error message.</param>
		public LogStreamException(string message) : base(message)
		{
		}
	}
}