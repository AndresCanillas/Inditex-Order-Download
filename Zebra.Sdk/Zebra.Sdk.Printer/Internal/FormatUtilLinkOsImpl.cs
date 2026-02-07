using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FormatUtilLinkOsImpl : FormatUtilLinkOs
	{
		private ZebraPrinterLinkOs printer;

		public FormatUtilLinkOsImpl(ZebraPrinterLinkOs zebraPrinterLinkOs)
		{
			this.printer = zebraPrinterLinkOs;
		}

		private int GetVariableNumber(string format, int indexOfFn, int indexOfNextCaret, int indexOfNextQuote)
		{
			int num;
			num = (indexOfNextQuote == -1 ? int.Parse(format.Substring(indexOfFn + 3, indexOfNextCaret - (indexOfFn + 3))) : int.Parse(format.Substring(indexOfFn + 3, (indexOfNextCaret < indexOfNextQuote ? indexOfNextCaret - (indexOfFn + 3) : indexOfNextQuote - (indexOfFn + 3)))));
			return num;
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, string> vars)
		{
			this.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, vars, Encoding.GetEncoding(0).WebName);
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, ZebraImageI> imgVars, Dictionary<int, string> vars)
		{
			this.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, imgVars, vars, Encoding.GetEncoding(0).WebName);
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding)
		{
			int num;
			try
			{
				string str = formatPathOnPrinter.Substring(formatPathOnPrinter.IndexOf(":") + 1, formatPathOnPrinter.LastIndexOf(".") - (formatPathOnPrinter.IndexOf(":") + 1));
				string str1 = Encoding.UTF8.GetString(this.printer.RetrieveFormatFromPrinter(formatPathOnPrinter));
				str1 = ZPLUtilities.ReplaceInternalCharactersWithReadableCharacters(str1);
				str1 = str1.Replace(string.Concat("^DF", str, ","), "");
				str1 = str1.Replace(string.Concat("^DF", formatPathOnPrinter, "^FS"), "");
				int num1 = 0;
				while (true)
				{
					int num2 = str1.IndexOf("^XG^FN", num1 + 1);
					num1 = num2;
					if (num2 == -1)
					{
						break;
					}
					int num3 = str1.IndexOf(",", num1);
					int num4 = str1.IndexOf("\"", num1);
					int num5 = num1 + 6;
					if (num4 == -1)
					{
						num = int.Parse(str1.Substring(num5, num3 - num5));
					}
					else
					{
						num = int.Parse(str1.Substring(num5, (num3 < num4 ? num3 - num5 : num4 - num5)));
						int num6 = str1.IndexOf("\"", num4 + 1);
						string str2 = str1.Substring(num4, num6 + 1 - num4);
						str1 = str1.Replace(str2, "");
					}
					string str3 = string.Concat("^FN", num);
					int num7 = str1.IndexOf(str3);
					if (num7 != -1)
					{
						str1 = str1.Remove(num7, str3.Length).Insert(num7, vars[num]);
					}
				}
				int num8 = 0;
				while (true)
				{
					int num9 = str1.IndexOf("^FN");
					num8 = num9;
					if (num9 == -1)
					{
						break;
					}
					int num10 = str1.IndexOf("^", num8 + 1);
					int num11 = str1.IndexOf("\"", num8 + 1);
					string str4 = str1.Substring(num8, num10 - num8);
					if (str1.IndexOf("^FD") != num10)
					{
						int variableNumber = this.GetVariableNumber(str1, num8, num10, num11);
						str1 = str1.Replace(str4, string.Concat("^FD", vars[variableNumber]));
					}
					else
					{
						int num12 = str1.IndexOf("^", num10 + 1);
						string str5 = str1.Substring(num10, num12 - num10);
						str1 = str1.Replace(str5, "");
						str1 = str1.Replace(str4, str5);
					}
				}
				this.printer.Connection.Write(Encoding.GetEncoding(encoding).GetBytes(ZPLUtilities.ReplaceAllWithInternalCharacters(str1)));
			}
			catch (ArgumentException)
			{
			}
		}

		public void PrintStoredFormatWithVarGraphics(string formatPathOnPrinter, Dictionary<int, ZebraImageI> imgVars, Dictionary<int, string> vars, string encoding)
		{
			GraphicsUtilZpl graphicsUtilZpl = new GraphicsUtilZpl(this.printer.Connection);
			int num = 1;
			foreach (int key in imgVars.Keys)
			{
				try
				{
					int num1 = num;
					num = num1 + 1;
					string str = string.Format("{0:00}", num1);
					string str1 = string.Concat("R:SDK", str, ".GRF");
					graphicsUtilZpl.StoreImage(str1, imgVars[key], imgVars[key].Width, imgVars[key].Height);
					vars.Add(key, str1);
				}
				catch (ZebraIllegalArgumentException)
				{
				}
			}
			this.PrintStoredFormatWithVarGraphics(formatPathOnPrinter, vars, encoding);
		}
	}
}