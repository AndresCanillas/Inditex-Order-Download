using System;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       Signals that an illegal argument was used.
	///       </summary>
	public class ZebraIllegalArgumentException : ArgumentException
	{
		/// <summary>
		///       Constructs a <c>ZebraIllegalArgumentException</c> with <c>message</c> as the detailed error message
		///       </summary>
		/// <param name="message">the error message</param>
		public ZebraIllegalArgumentException(string message) : base(message)
		{
		}

		/// <summary>
		///       Constructs a <c>ZebraIllegalArgumentException</c> with <c>message</c> as the detailed error message
		///       </summary>
		/// <param name="message">the error message</param>
		/// <param name="paramName">The name of the parameter that caused the exception.</param>
		public ZebraIllegalArgumentException(string message, string paramName) : base(message, paramName)
		{
		}

		/// <summary>
		///       Constructs a <c>ZebraIllegalArgumentException</c> with the <c>message</c> as the detailed error message and 
		///       <c>cause</c> as the source of the exception.
		///       </summary>
		/// <param name="message">The error message.</param>
		/// <param name="cause">The cause of the exception.</param>
		public ZebraIllegalArgumentException(string message, Exception cause) : base(message, cause)
		{
		}

		/// <summary>
		///       Constructs a <c>ZebraIllegalArgumentException</c> with the <c>message</c> as the detailed error message and 
		///       <c>cause</c> as the source of the exception.
		///       </summary>
		/// <param name="message">The error message.</param>
		/// <param name="paramName">The name of the parameter that caused the exception.</param>
		/// <param name="cause">The cause of the exception.</param>
		public ZebraIllegalArgumentException(string message, string paramName, Exception cause) : base(message, paramName, cause)
		{
		}
	}
}