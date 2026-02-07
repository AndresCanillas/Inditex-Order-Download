using System;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Signals that an error has occurred on the connection.
	///       </summary>
	public class ConnectionException : Exception
	{
		/// <summary>
		///       onstructs a <c>ConnectionException</c> with <c>message</c> as the detailed error message.
		///       </summary>
		/// <param name="message">The error message.</param>
		public ConnectionException(string message) : base(message)
		{
		}

		/// <summary>
		///       Constructs a <c>ConnectionException</c> with <c>cause</c> as the source of the exception.
		///       </summary>
		/// <param name="cause">The cause of the exception.</param>
		public ConnectionException(Exception cause) : base("", cause)
		{
		}

		/// <summary>
		///       Constructs a <c>ConnectionException</c> with the <c>message</c> as the detailed error message and 
		///       <c>cause</c> as the source of the exception.
		///       </summary>
		/// <param name="message">The error message.</param>
		/// <param name="cause">The cause of the exception.</param>
		public ConnectionException(string message, Exception cause) : base(message, cause)
		{
		}
	}
}