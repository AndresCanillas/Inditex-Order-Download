using System;

namespace Zebra.Sdk.Weblink
{
	/// <summary>
	///       Signals that an error occured while configuring weblink.
	///       </summary>
	public class ZebraWeblinkException : Exception
	{
		/// <summary>
		///       Constructs a <c>ZebraWeblinkException</c> with a base Exception.
		///       </summary>
		/// <param name="e">The base exception.</param>
		public ZebraWeblinkException(Exception e) : base(e.Message, e)
		{
		}

		/// <summary>
		///       Constructs a <c>ZebraWeblinkException</c> with a custom detailed error message.
		///       </summary>
		/// <param name="message">The custom error message.</param>
		public ZebraWeblinkException(string message) : base(message)
		{
		}
	}
}