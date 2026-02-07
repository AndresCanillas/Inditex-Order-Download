using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FormatUtilCpcl : FormatUtilA
	{
		public FormatUtilCpcl(Connection printerConnection) : base(printerConnection)
		{
		}

		protected int CountVariableFields(string formatString)
		{
			return StringUtilities.CountSubstringOccurences(formatString, "\\\\");
		}

		public override FieldDescriptionData[] GetVariableFields(string formatString)
		{
			FieldDescriptionData[] fieldDescriptionDatum = new FieldDescriptionData[this.CountVariableFields(formatString)];
			for (int i = 0; i < (int)fieldDescriptionDatum.Length; i++)
			{
				fieldDescriptionDatum[i] = new FieldDescriptionData(i + 1, null);
			}
			return fieldDescriptionDatum;
		}

		public override void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars)
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

		public override void PrintStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars, string encoding)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("! UF ");
			stringBuilder.Append(formatPathOnPrinter);
			stringBuilder.Append(StringUtilities.CRLF);
			Dictionary<int, string>.KeyCollection.Enumerator enumerator = vars.Keys.GetEnumerator();
			while (enumerator.MoveNext())
			{
				stringBuilder.Append(vars[enumerator.Current]);
				stringBuilder.Append(StringUtilities.CRLF);
			}
			this.printerConnection.Write(Encoding.GetEncoding(encoding).GetBytes(stringBuilder.ToString()));
		}

		public override byte[] RetrieveFormatFromPrinter(string filePathOnPrinter)
		{
			return Encoding.UTF8.GetBytes(SGD.DO("file.type", filePathOnPrinter, this.printerConnection));
		}

		public override void RetrieveFormatFromPrinter(Stream formatData, string formatPathOnPrinter)
		{
			SGD.DO(formatData, "file.type", formatPathOnPrinter, this.printerConnection);
		}
	}
}