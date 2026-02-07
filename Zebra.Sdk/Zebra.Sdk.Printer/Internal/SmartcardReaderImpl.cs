using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class SmartcardReaderImpl : SmartcardReader
	{
		protected Connection printerConnection;

		public SmartcardReaderImpl(ZebraPrinter printer)
		{
			this.printerConnection = printer.Connection;
		}

		public void Close()
		{
			string str = string.Concat("! U1 S-CARD CT_CLOSE", StringUtilities.CRLF);
			this.printerConnection.Write(Encoding.UTF8.GetBytes(str));
		}

		public byte[] DoCommand(string asciiHexData)
		{
			return ((PrinterCommand)(new PrinterCommandImpl(string.Concat(new object[] { "! U1 S-CARD CT_DATA ", asciiHexData.Length, " ", asciiHexData, StringUtilities.CRLF })))).SendAndWaitForResponse(this.printerConnection);
		}

		public byte[] GetATR()
		{
			return ((PrinterCommand)(new PrinterCommandImpl(string.Concat("! U1 S-CARD CT_ATR", StringUtilities.CRLF)))).SendAndWaitForResponse(this.printerConnection);
		}
	}
}