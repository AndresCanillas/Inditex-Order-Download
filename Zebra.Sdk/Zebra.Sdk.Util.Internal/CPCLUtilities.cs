using System;
using System.Text;

namespace Zebra.Sdk.Util.Internal
{
	internal class CPCLUtilities
	{
		private readonly static byte CPCL_ESC;

		private readonly static byte ASCII_FF;

		private readonly static byte ASCII_H;

		private readonly static byte ASCII_V;

		public readonly static string[] VERSION_PREFIXES;

		public readonly static string PRINTER_STATUS;

		public readonly static string PRINTER_CONFIG_LABEL;

		public readonly static string PRINTER_FORM_FEED;

		static CPCLUtilities()
		{
			CPCLUtilities.CPCL_ESC = 27;
			CPCLUtilities.ASCII_FF = 12;
			CPCLUtilities.ASCII_H = 104;
			CPCLUtilities.ASCII_V = 86;
			CPCLUtilities.VERSION_PREFIXES = new string[] { "SH", "H8", "C" };
			CPCLUtilities.PRINTER_STATUS = Encoding.UTF8.GetString(new byte[] { CPCLUtilities.CPCL_ESC, CPCLUtilities.ASCII_H });
			CPCLUtilities.PRINTER_CONFIG_LABEL = Encoding.UTF8.GetString(new byte[] { CPCLUtilities.CPCL_ESC, CPCLUtilities.ASCII_V });
			CPCLUtilities.PRINTER_FORM_FEED = Encoding.UTF8.GetString(new byte[] { CPCLUtilities.ASCII_FF });
		}

		public CPCLUtilities()
		{
		}
	}
}