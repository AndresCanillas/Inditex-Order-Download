using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Signals that a Link-OS operation has been attempted on a non-Link-OS printer.
	///       </summary>
	public class NotALinkOsPrinterException : Exception
	{
		/// <summary>
		///       Constructs a <c>NotALinkOsPrinterException</c> with <c>"This is not a Link-OS printer"</c> as
		///       the detailed error message.
		///       </summary>
		public NotALinkOsPrinterException() : base("This is not a Link-OS printer")
		{
		}
	}
}