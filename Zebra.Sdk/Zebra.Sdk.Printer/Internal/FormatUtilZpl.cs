using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FormatUtilZpl : FormatUtilA
	{
		public FormatUtilZpl(Connection printerConnection) : base(printerConnection)
		{
		}

		public static string ExtractDFName(string formatString)
		{
			string str = "^";
			int index = 0;
			while (index > -1)
			{
				FormatUtilZpl.IndexAndCommandType indexAndCommandType = FormatUtilZpl.FindNextCommand(str, index, formatString, FormatUtilZpl.CommandType.DfCommand);
				index = indexAndCommandType.Index;
				FormatUtilZpl.CommandType command = indexAndCommandType.Command;
				if (index <= -1)
				{
					continue;
				}
				int num = index + 3;
				if (command == FormatUtilZpl.CommandType.CcCommand)
				{
					str = formatString.Substring(num, 1);
				}
				else if (command == FormatUtilZpl.CommandType.DfCommand)
				{
					int num1 = StringUtilities.IndexOf(formatString, new string[] { str, ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX }, num);
					if (num1 > -1)
					{
						return formatString.Substring(num, num1 - num);
					}
				}
				index = num;
			}
			return null;
		}

		private static FormatUtilZpl.IndexAndCommandType FindNext(string caret, string tilde, int searchIx, string formatString, FormatUtilZpl.CommandType commandType)
		{
			string str = string.Concat(caret, commandType.Id);
			string str1 = string.Concat(caret, FormatUtilZpl.CommandType.CcCommand.Id);
			string str2 = string.Concat(tilde, FormatUtilZpl.CommandType.CcCommand.Id);
			string str3 = string.Concat(caret, FormatUtilZpl.CommandType.XgCommand.Id, str);
			return FormatUtilZpl.FindSpecifiedCommand(searchIx, formatString, new string[] { str, str1, str2, str3 });
		}

		private static FormatUtilZpl.IndexAndCommandType FindNextCommand(string caret, int searchIx, string formatString, FormatUtilZpl.CommandType commandType)
		{
			FormatUtilZpl.IndexAndCommandType indexAndCommandType = FormatUtilZpl.FindNext(caret, "~", searchIx, formatString, commandType);
			if (indexAndCommandType.Index == -1)
			{
				indexAndCommandType = FormatUtilZpl.FindNext(ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX, ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, searchIx, formatString, commandType);
			}
			return indexAndCommandType;
		}

		private static int FindOccurance(List<FieldDescriptionData> fnFields, FieldDescriptionData descriptorData)
		{
			int num = -1;
			int num1 = 0;
			while (num1 < fnFields.Count)
			{
				if (fnFields[num1].FieldNumber != descriptorData.FieldNumber)
				{
					num1++;
				}
				else
				{
					num = num1;
					break;
				}
			}
			return num;
		}

		private static FormatUtilZpl.IndexAndCommandType FindSpecifiedCommand(int searchIx, string formatString, string[] commandStrings)
		{
			FormatUtilZpl.CommandType commandType;
			int num = StringUtilities.IndexOf(formatString.ToLower(), commandStrings, searchIx);
			commandType = (num <= -1 ? FormatUtilZpl.CommandType.UnknownCommand : FormatUtilZpl.CommandType.GetCommand(formatString.Substring(num + 1, 2)));
			return new FormatUtilZpl.IndexAndCommandType(num, commandType);
		}

		public static string GenerateStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars, string quantity)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("^XA\r\n");
			stringBuilder.Append("^XF");
			stringBuilder.Append(formatPathOnPrinter);
			stringBuilder.Append("^FS\r\n");
			Dictionary<int, string>.Enumerator enumerator = vars.GetEnumerator();
			while (enumerator.MoveNext())
			{
				KeyValuePair<int, string> current = enumerator.Current;
				stringBuilder.Append("^FN");
				stringBuilder.Append(current.Key);
				stringBuilder.Append("^FD");
				stringBuilder.Append(current.Value);
				stringBuilder.Append("^FS\r\n");
			}
			if (!string.IsNullOrEmpty(quantity))
			{
				stringBuilder.Append(string.Concat("^PQ", quantity, "\r\n"));
			}
			stringBuilder.Append("^XZ");
			return ZPLUtilities.DecorateWithFormatPrefix(stringBuilder.ToString());
		}

		public static string GenerateStoredFormat(string formatPathOnPrinter, Dictionary<int, string> vars)
		{
			return FormatUtilZpl.GenerateStoredFormat(formatPathOnPrinter, vars, null);
		}

		public override FieldDescriptionData[] GetVariableFields(string formatString)
		{
			return FormatUtilZpl.GetVariableFieldsS(formatString);
		}

		public static FieldDescriptionData[] GetVariableFieldsS(string formatString)
		{
			List<FieldDescriptionData> fieldDescriptionDatas = new List<FieldDescriptionData>();
			string str = "^";
			string str1 = ",";
			int index = 0;
			while (index > -1)
			{
				FormatUtilZpl.IndexAndCommandType indexAndCommandType = FormatUtilZpl.FindNextCommand(str, index, formatString, FormatUtilZpl.CommandType.FnCommand);
				index = indexAndCommandType.Index;
				FormatUtilZpl.CommandType command = indexAndCommandType.Command;
				if (index <= -1)
				{
					continue;
				}
				int num = 3;
				int num1 = index + num;
				if (command == FormatUtilZpl.CommandType.CcCommand)
				{
					str = formatString.Substring(num1, 1);
				}
				else if (command == FormatUtilZpl.CommandType.FnCommand)
				{
					int num2 = StringUtilities.IndexOf(formatString, new string[] { str, ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX }, num1);
					if (num2 > -1)
					{
						string str2 = formatString.Substring(num1, num2 - num1);
						try
						{
							FieldDescriptionData fieldDescriptionDatum = FormatUtilZpl.ParseFnCommand(str2);
							int num3 = FormatUtilZpl.FindOccurance(fieldDescriptionDatas, fieldDescriptionDatum);
							if (num3 == -1)
							{
								fieldDescriptionDatas.Add(fieldDescriptionDatum);
							}
							else if (fieldDescriptionDatum.FieldName != null)
							{
								fieldDescriptionDatas.RemoveAt(num3);
								fieldDescriptionDatas.Add(fieldDescriptionDatum);
							}
						}
						catch (MalformedFormatException)
						{
						}
					}
				}
				else if (command == FormatUtilZpl.CommandType.XgCommand)
				{
					num1 += 3;
					int num4 = index + num * 2;
					int num5 = StringUtilities.IndexOf(formatString, new string[] { str1, ZPLUtilities.ZPL_INTERNAL_DELIMITER }, num4);
					if (num5 > -1)
					{
						string str3 = formatString.Substring(num4, num5 - num4);
						try
						{
							FieldDescriptionData fieldDescriptionDatum1 = FormatUtilZpl.ParseFnCommand(str3);
							int num6 = FormatUtilZpl.FindOccurance(fieldDescriptionDatas, fieldDescriptionDatum1);
							if (num6 == -1)
							{
								fieldDescriptionDatas.Add(fieldDescriptionDatum1);
							}
							else if (fieldDescriptionDatum1.FieldName != null)
							{
								fieldDescriptionDatas.RemoveAt(num6);
								fieldDescriptionDatas.Add(fieldDescriptionDatum1);
							}
						}
						catch (MalformedFormatException)
						{
						}
					}
				}
				index = num1;
			}
			return fieldDescriptionDatas.ToArray();
		}

		private bool IsOnlySettingsChannelOpen(MultichannelConnection multiChannelConnection)
		{
			if (!multiChannelConnection.StatusChannel.Connected)
			{
				return false;
			}
			return !multiChannelConnection.PrintingChannel.Connected;
		}

		private static FieldDescriptionData ParseFnCommand(string fnCommand)
		{
			FieldDescriptionData fieldDescriptionDatum = null;
			int num = 0;
			int num1 = fnCommand.IndexOf('\"', num);
			if (num1 == -1)
			{
				try
				{
					int num2 = int.Parse(fnCommand.Trim());
					if (num2 < 1 || num2 > 9999)
					{
						throw new MalformedFormatException("'^FN' integer must be between 1 and 9999");
					}
					fieldDescriptionDatum = new FieldDescriptionData(num2, null);
				}
				catch (Exception exception)
				{
					if (!(exception is MalformedFormatException))
					{
						throw new MalformedFormatException("'^FN' must be followed by an integer");
					}
					throw;
				}
			}
			else if (num1 + 1 < fnCommand.Length)
			{
				try
				{
					string str = fnCommand.Substring(num1).Trim();
					str = StringUtilities.StripQuotes(str);
					int num3 = int.Parse(fnCommand.Substring(num, num1 - num).Trim());
					if (num3 < 1 || num3 > 9999)
					{
						throw new MalformedFormatException("'^FN' integer must be between 1 and 9999");
					}
					fieldDescriptionDatum = new FieldDescriptionData(num3, str);
				}
				catch (Exception exception1)
				{
					if (!(exception1 is MalformedFormatException))
					{
						throw new MalformedFormatException("'^FN' must be followed by an integer");
					}
					throw;
				}
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
			this.ThrowExceptionStatusOnly();
			this.printerConnection.Write(Encoding.GetEncoding(encoding).GetBytes(FormatUtilZpl.GenerateStoredFormat(formatPathOnPrinter, vars)));
		}

		public override byte[] RetrieveFormatFromPrinter(string filePathOnPrinter)
		{
			if (string.IsNullOrEmpty(filePathOnPrinter))
			{
				return null;
			}
			return ((PrinterCommand)(new PrinterCommandImpl(ZPLUtilities.DecorateWithFormatPrefix(string.Concat("^XA^HF", filePathOnPrinter, "^XZ"))))).SendAndWaitForResponse(this.printerConnection);
		}

		public override void RetrieveFormatFromPrinter(Stream formatData, string formatPathOnPrinter)
		{
			if (string.IsNullOrEmpty(formatPathOnPrinter))
			{
				return;
			}
			((PrinterCommand)(new PrinterCommandImpl(ZPLUtilities.DecorateWithFormatPrefix(string.Concat("^XA^HF", formatPathOnPrinter, "^XZ"))))).SendAndWaitForResponse(new BinaryWriter(formatData), this.printerConnection);
		}

		private void ThrowExceptionStatusOnly()
		{
			MultichannelConnection multichannelConnection = this.printerConnection as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection != null)
			{
				if (this.IsOnlySettingsChannelOpen(multichannelConnection1))
				{
					throw new ConnectionException("Operation cannot be performed with only the status channel open");
				}
			}
			else if (this.printerConnection is StatusConnection)
			{
				throw new ConnectionException("Operation cannot be performed over the status channel");
			}
		}

		internal class CommandType
		{
			private string id;

			internal static FormatUtilZpl.CommandType FnCommand;

			internal static FormatUtilZpl.CommandType CcCommand;

			internal static FormatUtilZpl.CommandType XgCommand;

			internal static FormatUtilZpl.CommandType DfCommand;

			internal static FormatUtilZpl.CommandType XaCommand;

			internal static FormatUtilZpl.CommandType XzCommand;

			internal static FormatUtilZpl.CommandType UnknownCommand;

			internal string Id
			{
				get
				{
					return this.id.ToLower();
				}
			}

			static CommandType()
			{
				FormatUtilZpl.CommandType.FnCommand = new FormatUtilZpl.CommandType("FN");
				FormatUtilZpl.CommandType.CcCommand = new FormatUtilZpl.CommandType("CC");
				FormatUtilZpl.CommandType.XgCommand = new FormatUtilZpl.CommandType("XG");
				FormatUtilZpl.CommandType.DfCommand = new FormatUtilZpl.CommandType("DF");
				FormatUtilZpl.CommandType.XaCommand = new FormatUtilZpl.CommandType("XA");
				FormatUtilZpl.CommandType.XzCommand = new FormatUtilZpl.CommandType("XZ");
				FormatUtilZpl.CommandType.UnknownCommand = new FormatUtilZpl.CommandType("unknown");
			}

			private CommandType(string id)
			{
				this.id = id;
			}

			internal static FormatUtilZpl.CommandType GetCommand(string twoCharacterCommand)
			{
				FormatUtilZpl.CommandType unknownCommand = FormatUtilZpl.CommandType.UnknownCommand;
				if (Regex.IsMatch(twoCharacterCommand, FormatUtilZpl.CommandType.FnCommand.Id, RegexOptions.IgnoreCase))
				{
					unknownCommand = FormatUtilZpl.CommandType.FnCommand;
				}
				else if (Regex.IsMatch(twoCharacterCommand, FormatUtilZpl.CommandType.CcCommand.Id, RegexOptions.IgnoreCase))
				{
					unknownCommand = FormatUtilZpl.CommandType.CcCommand;
				}
				else if (Regex.IsMatch(twoCharacterCommand, FormatUtilZpl.CommandType.DfCommand.Id, RegexOptions.IgnoreCase))
				{
					unknownCommand = FormatUtilZpl.CommandType.DfCommand;
				}
				else if (Regex.IsMatch(twoCharacterCommand, FormatUtilZpl.CommandType.XaCommand.Id, RegexOptions.IgnoreCase))
				{
					unknownCommand = FormatUtilZpl.CommandType.XaCommand;
				}
				else if (Regex.IsMatch(twoCharacterCommand, FormatUtilZpl.CommandType.XzCommand.Id, RegexOptions.IgnoreCase))
				{
					unknownCommand = FormatUtilZpl.CommandType.XzCommand;
				}
				else if (Regex.IsMatch(twoCharacterCommand, FormatUtilZpl.CommandType.XgCommand.Id, RegexOptions.IgnoreCase))
				{
					unknownCommand = FormatUtilZpl.CommandType.XgCommand;
				}
				return unknownCommand;
			}
		}

		internal class IndexAndCommandType
		{
			private int index;

			private FormatUtilZpl.CommandType command;

			internal FormatUtilZpl.CommandType Command
			{
				get
				{
					return this.command;
				}
			}

			internal int Index
			{
				get
				{
					return this.index;
				}
			}

			internal IndexAndCommandType(int index, FormatUtilZpl.CommandType command)
			{
				this.index = index;
				this.command = command;
			}
		}
	}
}