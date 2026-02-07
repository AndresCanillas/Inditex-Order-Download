using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Signals that an error has occurred when attempting to communicate with SNMP.
	///       </summary>
	public class SnmpException : Exception
	{
		/// <summary>
		///       Constructs an <c>SnmpException</c> with <c>message</c> as the detailed error message.
		///       </summary>
		/// <param name="message">The error message.</param>
		public SnmpException(string message) : base(message)
		{
		}
	}
}