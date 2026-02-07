using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class PrinterFileType
	{
		private List<string> descriptions = new List<string>();

		public static PrinterFileType UNSUPPORTED;

		public static PrinterFileType PRINTER_PNG;

		public static PrinterFileType PRINTER_GRF;

		public static PrinterFileType FONT;

		public static PrinterFileType ZPL;

		public static PrinterFileType NRD;

		public static PrinterFileType PAC;

		public static PrinterFileType FIRMWARE;

		public static PrinterFileType PCX;

		public static PrinterFileType BMP;

		private static List<PrinterFileType> extensions;

		static PrinterFileType()
		{
			PrinterFileType.UNSUPPORTED = new PrinterFileType();
			PrinterFileType.PRINTER_PNG = new PrinterFileType(".PNG", "~DY_P");
			PrinterFileType.PRINTER_GRF = new PrinterFileType(".GRF", "~DY_G");
			PrinterFileType.FONT = new PrinterFileType(".TTF", ".TTE", ".FNT", "~DY_E", "~DY_T");
			PrinterFileType.ZPL = new PrinterFileType(".ZPL");
			PrinterFileType.NRD = new PrinterFileType(".NRD", "~DY_NRD");
			PrinterFileType.PAC = new PrinterFileType(".PAC", "~DY_PAC");
			PrinterFileType.FIRMWARE = new PrinterFileType();
			PrinterFileType.PCX = new PrinterFileType(".PCX", "~DY_X");
			PrinterFileType.BMP = new PrinterFileType(".BMP", "~DY_B");
			PrinterFileType.extensions = new List<PrinterFileType>()
			{
				PrinterFileType.UNSUPPORTED,
				PrinterFileType.PRINTER_PNG,
				PrinterFileType.PRINTER_GRF,
				PrinterFileType.FONT,
				PrinterFileType.ZPL,
				PrinterFileType.NRD,
				PrinterFileType.PAC,
				PrinterFileType.FIRMWARE,
				PrinterFileType.PCX,
				PrinterFileType.BMP
			};
		}

		public PrinterFileType()
		{
		}

		public PrinterFileType(string ext)
		{
			this.descriptions.Add(ext);
		}

		public PrinterFileType(string ext, string ext1)
		{
			this.descriptions.Add(ext);
			this.descriptions.Add(ext1);
		}

		public PrinterFileType(string ext, string ext1, string ext2, string ext3, string ext4)
		{
			this.descriptions.Add(ext);
			this.descriptions.Add(ext1);
			this.descriptions.Add(ext2);
			this.descriptions.Add(ext3);
			this.descriptions.Add(ext4);
		}

		public static PrinterFileType GetUnwrappedType(string extension)
		{
			PrinterFileType printerFileType;
			List<PrinterFileType>.Enumerator enumerator = PrinterFileType.extensions.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					PrinterFileType current = enumerator.Current;
					if (!current.descriptions.Contains(extension.ToUpper()) && !current.descriptions.Contains(string.Concat(".", extension.ToUpper())))
					{
						continue;
					}
					printerFileType = current;
					return printerFileType;
				}
				return PrinterFileType.UNSUPPORTED;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
		}
	}
}