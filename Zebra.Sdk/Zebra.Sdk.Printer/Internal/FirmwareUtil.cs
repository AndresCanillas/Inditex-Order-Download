using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FirmwareUtil
	{
		public FirmwareUtil()
		{
		}

		public static string ExtractFirmwareVersion(Stream firmwareFileContents)
		{
			int num;
			StreamReader streamReader = new StreamReader(firmwareFileContents);
			string item = "";
			int num1 = 5;
			try
			{
				string str = null;
				StringBuilder stringBuilder = new StringBuilder();
				int num2 = 0;
				do
				{
					string str1 = streamReader.ReadLine();
					str = str1;
					if (str1 == null)
					{
						break;
					}
					stringBuilder.Append(str);
					num = num2 + 1;
					num2 = num;
				}
				while (num < num1);
				List<string> matches = RegexUtil.GetMatches("^\\s*! PROGRAM\\s*(.*?)\\s*~D[C|I]", stringBuilder.ToString());
				if (matches.Count != 2)
				{
					throw new ZebraIllegalArgumentException("Invalid firmware file.");
				}
				item = matches[1];
			}
			catch (IOException)
			{
			}
			return item;
		}

		public static bool FirmwareVersionsDontMatch(Stream firmwareFileContents, Connection connection)
		{
			string lower = FirmwareUtil.GetFWVersionFromPrinterConnection(connection).Trim().ToLower();
			return FirmwareUtil.FirmwareVersionsDontMatch(firmwareFileContents, lower);
		}

		public static bool FirmwareVersionsDontMatch(Stream firmwareFileContents, string versionFromPrinter)
		{
			return !FirmwareUtil.FirmwareVersionsMatch(FirmwareUtil.ExtractFirmwareVersion(firmwareFileContents).Trim(), versionFromPrinter);
		}

		public static bool FirmwareVersionsDontMatch(string versionFromFirmware, Connection connection)
		{
			string lower = FirmwareUtil.GetFWVersionFromPrinterConnection(connection).Trim().ToLower();
			return !FirmwareUtil.FirmwareVersionsMatch(versionFromFirmware, lower);
		}

		public static bool FirmwareVersionsMatch(string version1, string version2)
		{
			string lower = version1.Trim().ToLower();
			string str = version2.Trim().ToLower();
			if (str.Equals(lower))
			{
				return true;
			}
			return FirmwareUtil.MatchIgnoringZ(lower, str);
		}

		public static string GetFWVersionFromPrinterConnection(Connection connection)
		{
			return SGD.GET("appl.name", connection);
		}

		private static bool MatchIgnoringZ(string versionFromFirmware, string versionFromPrinter)
		{
			return versionFromPrinter.Replace("z", "").Equals(versionFromFirmware.Replace("z", ""));
		}
	}
}