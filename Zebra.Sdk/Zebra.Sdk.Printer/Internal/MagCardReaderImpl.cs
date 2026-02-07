using System;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class MagCardReaderImpl : MagCardReader
	{
		protected Connection printerConnection;

		public MagCardReaderImpl(ZebraPrinter printer)
		{
			this.printerConnection = printer.Connection;
		}

		public string[] Read(int timeoutMS)
		{
			string[] strArrays = new string[] { "", "", "" };
			if (timeoutMS <= 0)
			{
				timeoutMS = 1000;
			}
			int num = timeoutMS * 8 / 1000;
			byte[] numArray = ((PrinterCommand)(new PrinterCommandImpl(string.Concat(new object[] { "! U1 MCR ", num, " T1 T2 T3", StringUtilities.CRLF })))).SendAndWaitForResponse(this.printerConnection, timeoutMS, this.printerConnection.TimeToWaitForMoreData);
			string str = Encoding.UTF8.GetString(numArray);
			string str1 = "T1:";
			int num1 = str.IndexOf(str1);
			int num2 = -1;
			if (num1 != -1)
			{
				num2 = str.IndexOf(StringUtilities.CRLF, num1);
			}
			if (num1 != -1 && num2 != -1)
			{
				strArrays[0] = str.Substring(num1 + str1.Length, num2 - (num1 + str1.Length));
			}
			str1 = "T2:";
			num1 = str.IndexOf(str1);
			num2 = -1;
			if (num1 != -1)
			{
				num2 = str.IndexOf(StringUtilities.CRLF, num1);
			}
			if (num1 != -1 && num2 != -1)
			{
				strArrays[1] = str.Substring(num1 + str1.Length, num2 - (num1 + str1.Length));
			}
			str1 = "T3:";
			num1 = str.IndexOf(str1);
			num2 = -1;
			if (num1 != -1)
			{
				num2 = str.IndexOf(StringUtilities.CRLF, num1);
			}
			if (num1 != -1 && num2 != -1)
			{
				strArrays[2] = str.Substring(num1 + str1.Length, num2 - (num1 + str1.Length));
			}
			return strArrays;
		}
	}
}