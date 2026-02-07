using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class FormatUtilA : FormatUtil
	{
		protected Connection printerConnection;

		public FormatUtilA(Connection printerConnection)
		{
			this.printerConnection = printerConnection;
		}

		public abstract FieldDescriptionData[] GetVariableFields(string formatString);

		public void PrintStoredFormat(string formatPathOnPrinter, string[] vars)
		{
			try
			{
				this.PrintStoredFormat(formatPathOnPrinter, vars, Encoding.GetEncoding(0).WebName);
			}
			catch (ArgumentException)
			{
			}
			catch (NotSupportedException)
			{
			}
		}

		public void PrintStoredFormat(string formatPathOnPrinter, string[] vars, string encoding)
		{
			Dictionary<int, string> nums = new Dictionary<int, string>();
			for (int i = 0; i < (int)vars.Length; i++)
			{
				if (vars[i] != null)
				{
					nums.Add(i + 2, vars[i]);
				}
			}
			this.PrintStoredFormat(formatPathOnPrinter, nums, encoding);
		}

		public abstract void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars);

		public abstract void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding);

		public abstract byte[] RetrieveFormatFromPrinter(string formatPathOnPrinter);

		public abstract void RetrieveFormatFromPrinter(Stream formatData, string formatPathOnPrinter);
	}
}