using System;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       A class used to determine if a base ZebraPrinter has Smartcard reader capabilities.
	///       </summary>
	public class SmartcardReaderFactory
	{
		private SmartcardReaderFactory()
		{
		}

		/// <summary>
		///       Creates an instance of a Smartcard reader, if available.
		///       </summary>
		/// <param name="printer">Base <c>ZebraPrinter</c> that may or may not have Smartcard reader capabilities.</param>
		/// <returns>An instance of a SmartcardReader object or null if the base printer does not have Smartcard reader
		///       hardware installed.</returns>
		public static SmartcardReader Create(ZebraPrinter printer)
		{
			if (!(printer is ZebraPrinterCpcl))
			{
				return null;
			}
			return new SmartcardReaderImpl(printer);
		}
	}
}