using System;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Device
{
	/// <summary>
	///       A class used to determine if a base <see cref="T:Zebra.Sdk.Printer.ZebraPrinter" /> has MagCard reader capabilities.  
	///       Not all Zebra printers are available with built-in readers.
	///       </summary>
	public class MagCardReaderFactory
	{
		private MagCardReaderFactory()
		{
		}

		/// <summary>
		///       Creates an instance of a Magcard reader, if available.
		///       </summary>
		/// <param name="printer">Base Zebra Printer that may or may not have MagCard reader capabilities.</param>
		/// <returns>An instance of a MagCardReader object or null if the base printer does not have MagCard reader hardware installed.</returns>
		public static MagCardReader Create(ZebraPrinter printer)
		{
			if (!(printer is ZebraPrinterCpcl))
			{
				return null;
			}
			return new MagCardReaderImpl(printer);
		}
	}
}