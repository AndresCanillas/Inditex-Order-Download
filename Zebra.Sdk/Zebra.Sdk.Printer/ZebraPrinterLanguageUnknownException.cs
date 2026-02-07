using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Signals that an error has occurred when determining the printer language.
	///       </summary>
	public class ZebraPrinterLanguageUnknownException : Exception
	{
		/// <summary>
		///       Constructs a <c>ZebraPrinterLanguageUnknownException</c> with <c>message</c> as the detailed error message
		///       </summary>
		/// <param name="message">The error message</param>
		public ZebraPrinterLanguageUnknownException(string message) : base(message)
		{
		}

		/// <summary>
		///       Constructs a <c>ZebraPrinterLanguageUnknownException</c> with <c>"Unknown printer language"</c> as
		///       the detailed error message.
		///       </summary>
		public ZebraPrinterLanguageUnknownException() : base("Unknown printer language")
		{
		}
	}
}