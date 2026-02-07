using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Util.Internal
{
	internal class ZPLUtilities
	{
		public static byte ZPL_INTERNAL_FORMAT_PREFIX_CHAR;

		public static byte ZPL_INTERNAL_COMMAND_PREFIX_CHAR;

		public static byte ZPL_INTERNAL_DELIMITER_CHAR;

		public static string ZPL_INTERNAL_FORMAT_PREFIX;

		public static string ZPL_INTERNAL_COMMAND_PREFIX;

		public static string ZPL_INTERNAL_DELIMITER;

		public static string PRINTER_INFO;

		public static string PRINTER_STATUS;

		public static string PRINTER_CONFIG_LABEL;

		public static string PRINTER_DIRECTORY_LABEL;

		public static string PRINTER_NETWORK_CONFIG_LABEL;

		public static string PRINTER_CALIBRATE;

		public static string PRINTER_RESET;

		public static string PRINTER_RESET_NETWORK;

		public static string PRINTER_RESTORE_DEFAULTS;

		public static string PRINTER_GET_SUPER_HOST_STATUS;

		public static string PRINTER_GET_STORAGE_INFO_COMMAND;

		public static string FILE_DRIVE_INFO_SETTING_NAME;

		public static string FILE_DRIVE_LISTING_SETTING_NAME;

		static ZPLUtilities()
		{
			ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR = 30;
			ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR = 16;
			ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR = 31;
			ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR });
			ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR });
			ZPLUtilities.ZPL_INTERNAL_DELIMITER = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR });
			ZPLUtilities.PRINTER_INFO = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 72, 73 });
			ZPLUtilities.PRINTER_STATUS = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 72, 83 });
			ZPLUtilities.PRINTER_CONFIG_LABEL = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 87, 67 });
			Encoding uTF8 = Encoding.UTF8;
			byte[] zPLINTERNALFORMATPREFIXCHAR = new byte[] { 0, 88, 65, 0, 87, 68, 42, 58, 42, 46, 42, 0, 88, 90 };
			zPLINTERNALFORMATPREFIXCHAR[0] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			zPLINTERNALFORMATPREFIXCHAR[3] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			zPLINTERNALFORMATPREFIXCHAR[11] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			ZPLUtilities.PRINTER_DIRECTORY_LABEL = uTF8.GetString(zPLINTERNALFORMATPREFIXCHAR);
			ZPLUtilities.PRINTER_NETWORK_CONFIG_LABEL = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 87, 76 });
			ZPLUtilities.PRINTER_CALIBRATE = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 74, 67 });
			ZPLUtilities.PRINTER_RESET = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 74, 82 });
			ZPLUtilities.PRINTER_RESET_NETWORK = Encoding.UTF8.GetString(new byte[] { ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR, 87, 82 });
			Encoding encoding = Encoding.UTF8;
			byte[] numArray = new byte[] { 0, 88, 65, 0, 74, 85, 70, 0, 88, 90 };
			numArray[0] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			numArray[3] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			numArray[7] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			ZPLUtilities.PRINTER_RESTORE_DEFAULTS = encoding.GetString(numArray);
			Encoding uTF81 = Encoding.UTF8;
			byte[] zPLINTERNALFORMATPREFIXCHAR1 = new byte[] { 0, 88, 65, 0, 72, 90, 65, 0, 88, 90 };
			zPLINTERNALFORMATPREFIXCHAR1[0] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			zPLINTERNALFORMATPREFIXCHAR1[3] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			zPLINTERNALFORMATPREFIXCHAR1[7] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			ZPLUtilities.PRINTER_GET_SUPER_HOST_STATUS = uTF81.GetString(zPLINTERNALFORMATPREFIXCHAR1);
			Encoding encoding1 = Encoding.UTF8;
			byte[] numArray1 = new byte[] { 0, 88, 65, 0, 72, 87, 42, 58, 88, 88, 88, 88, 46, 81, 81, 81, 0, 88, 90 };
			numArray1[0] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			numArray1[3] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			numArray1[16] = ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR;
			ZPLUtilities.PRINTER_GET_STORAGE_INFO_COMMAND = encoding1.GetString(numArray1);
			ZPLUtilities.FILE_DRIVE_INFO_SETTING_NAME = "file.drive_info";
			ZPLUtilities.FILE_DRIVE_LISTING_SETTING_NAME = "file.drive_listing";
		}

		public ZPLUtilities()
		{
		}

		private static string CreateFileNameRegex(Match matcher)
		{
			string str = "([A-Za-z]{1}:)?";
			string str1 = "[A-Za-z0-9\\-_]*";
			string value = matcher.Groups[1].Value;
			value = (string.IsNullOrEmpty(value) ? str : Regex.Replace(value, "\\*:", str));
			string str2 = Regex.Replace(matcher.Groups[3].Value, "\\*", str1);
			string str3 = Regex.Replace(matcher.Groups[4].Value, "\\*", str1);
			return string.Concat(value, str2, "\\.", str3);
		}

		public static string DecorateWithCommandPrefix(string command)
		{
			if (command == null)
			{
				return null;
			}
			if (command.IndexOf("~") == -1)
			{
				return string.Concat(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, command);
			}
			return command.Replace('~', (char)ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR);
		}

		public static string DecorateWithFormatPrefix(string format)
		{
			if (format == null)
			{
				return null;
			}
			if (format.IndexOf("^") == -1)
			{
				return string.Concat(ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX, format);
			}
			return format.Replace('\u005E', (char)ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR);
		}

		public static string[] FilterFileList(string[] fileList, string filter)
		{
			if (string.IsNullOrEmpty(filter))
			{
				filter = "*:*.*";
			}
			Regex regex = new Regex("^(([A-Za-z\\*]{1}):)?([A-Za-z0-9\\-_\\*]+)\\.([A-Za-z0-9\\-_\\*]+)$");
			regex.Matches(filter);
			Match match = regex.Match(filter);
			if (!match.Success)
			{
				return new string[0];
			}
			Regex regex1 = new Regex(ZPLUtilities.CreateFileNameRegex(match));
			List<string> strs = new List<string>();
			string[] strArrays = fileList;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				if (regex1.Match(str).Success)
				{
					strs.Add(str);
				}
			}
			return strs.ToArray();
		}

		public static int GetDpmm(string tildeHiResponse)
		{
			int num;
			string[] strArrays = tildeHiResponse.Split(new char[] { ',' });
			if ((int)strArrays.Length < 3)
			{
				return -1;
			}
			try
			{
				num = int.Parse(strArrays[2]);
			}
			catch (Exception)
			{
				num = -1;
			}
			return num;
		}

		public static string GetDYPrefix(char driveLetter, string fileName, char format, string extension, int numberOfBytesInFile, string bytesPerRow)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX).Append("DY").Append(driveLetter).Append(':').Append(fileName).Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER).Append(format);
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER).Append(extension).Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER).Append(Convert.ToString(numberOfBytesInFile)).Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER).Append(bytesPerRow).Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER);
			return stringBuilder.ToString();
		}

		public static string GetHZO(string printerDriveAndPath)
		{
			PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(printerDriveAndPath);
			string str = printerDriveAndPath.Substring(printerDriveAndPath.LastIndexOf('.'));
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX);
			stringBuilder.Append("XA");
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX);
			stringBuilder.Append("HZO");
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER);
			stringBuilder.Append(printerFilePath.Drive);
			stringBuilder.Append(':');
			stringBuilder.Append(printerFilePath.FileName);
			stringBuilder.Append(str);
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX);
			stringBuilder.Append("XZ");
			return stringBuilder.ToString();
		}

		public static bool IsValidZplFirmware(string fwVersion)
		{
			if (fwVersion == null)
			{
				return false;
			}
			return (new Regex("^[Vv][\\w-]+\\.[\\w-]+\\.[\\w-]+$")).Match(fwVersion).Success;
		}

		public static List<StorageInfo> ParseFileDriveInfoJson(string fileDriveInfoResponse)
		{
			List<StorageInfo> storageInfos = new List<StorageInfo>();
			foreach (KeyValuePair<string, ZPLUtilities.FileObjectDetails> item in JObject.Parse(fileDriveInfoResponse).ToObject<Dictionary<string, Dictionary<string, ZPLUtilities.FileObjectDetails>>>()[ZPLUtilities.FILE_DRIVE_INFO_SETTING_NAME])
			{
				StorageInfo storageInfo = new StorageInfo()
				{
					driveLetter = item.Key[0],
					bytesFree = item.Value.Free,
					driveType = Zebra.Sdk.Printer.DriveType.UNKNOWN,
					isPersistent = true
				};
				string storage = item.Value.Storage;
				if (Regex.IsMatch(storage, "RAM", RegexOptions.IgnoreCase))
				{
					storageInfo.driveType = Zebra.Sdk.Printer.DriveType.RAM;
					storageInfo.isPersistent = false;
				}
				else if (Regex.IsMatch(storage, "READ ONLY", RegexOptions.IgnoreCase))
				{
					storageInfo.driveType = Zebra.Sdk.Printer.DriveType.READ_ONLY;
					storageInfo.isPersistent = true;
				}
				else if (Regex.IsMatch(storage, "ONBOARD FLASH", RegexOptions.IgnoreCase))
				{
					storageInfo.driveType = Zebra.Sdk.Printer.DriveType.FLASH;
					storageInfo.isPersistent = true;
				}
				storageInfos.Add(storageInfo);
			}
			return storageInfos;
		}

		public static List<StorageInfo> ParseHWCommand(string hwResponse)
		{
			List<StorageInfo> storageInfos = new List<StorageInfo>();
			Regex regex = new Regex("(\\d+).+([A-Z]): ([\\w\\s]+)");
			string[] strArrays = hwResponse.Split(new char[] { '\n' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				Match match = regex.Match(strArrays[i].Trim());
				if (match.Length > 0 && match.Groups.Count >= 3)
				{
					StorageInfo storageInfo = new StorageInfo()
					{
						bytesFree = long.Parse(match.Groups[1].Value),
						driveLetter = match.Groups[2].Value[0]
					};
					string value = match.Groups[3].Value;
					storageInfo.driveType = Zebra.Sdk.Printer.DriveType.UNKNOWN;
					storageInfo.isPersistent = true;
					if (Regex.IsMatch(value, "RAM", RegexOptions.IgnoreCase))
					{
						storageInfo.driveType = Zebra.Sdk.Printer.DriveType.RAM;
						storageInfo.isPersistent = false;
					}
					else if (Regex.IsMatch(value, "OPTION MEMORY"))
					{
						storageInfo.driveType = Zebra.Sdk.Printer.DriveType.MASS_STORAGE;
						storageInfo.isPersistent = true;
					}
					else if (Regex.IsMatch(value, "MEMORY CARD"))
					{
						storageInfo.driveType = Zebra.Sdk.Printer.DriveType.MASS_STORAGE;
						storageInfo.isPersistent = true;
					}
					else if (Regex.IsMatch(value, "ONBOARD FLASH"))
					{
						storageInfo.driveType = Zebra.Sdk.Printer.DriveType.FLASH;
						storageInfo.isPersistent = true;
					}
					storageInfos.Add(storageInfo);
				}
			}
			return storageInfos;
		}

		public static string ReplaceAllWithInternalCharacters(string format)
		{
			if (format == null)
			{
				return null;
			}
			return ZPLUtilities.ReplaceAllWithInternalDelimeter(format).Replace("^", ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX).Replace("~", ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX);
		}

		public static string ReplaceAllWithInternalDelimeter(string format)
		{
			if (format == null)
			{
				return null;
			}
			return format.Replace(",", ZPLUtilities.ZPL_INTERNAL_DELIMITER);
		}

		public static string ReplaceInternalCharactersWithReadableCharacters(string zpl)
		{
			return zpl.Replace(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, "~").Replace(ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX, "^").Replace(ZPLUtilities.ZPL_INTERNAL_DELIMITER, ",");
		}

		public static byte[] ReplaceInternalCharactersWithReadableCharacters(byte[] zplBytes)
		{
			for (int i = 0; i < (int)zplBytes.Length; i++)
			{
				if (zplBytes[i] == ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR)
				{
					zplBytes[i] = 126;
				}
				else if (zplBytes[i] == ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR)
				{
					zplBytes[i] = 44;
				}
				else if (zplBytes[i] == ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR)
				{
					zplBytes[i] = 94;
				}
			}
			return zplBytes;
		}

		public static void ReplaceInternalCharactersWithReadableCharacters(Stream destination, Stream zplBytes)
		{
			using (ZPLUtilities.InternalCharacterFilteringOutputStream internalCharacterFilteringOutputStream = new ZPLUtilities.InternalCharacterFilteringOutputStream(destination))
			{
				byte[] numArray = new byte[16384];
				try
				{
					while (true)
					{
						int num = (new BinaryReader(zplBytes)).Read(numArray, 0, (int)numArray.Length);
						int num1 = num;
						if (num <= 0)
						{
							break;
						}
						internalCharacterFilteringOutputStream.Write(numArray, 0, num1);
					}
				}
				catch (IOException oException)
				{
					throw new ZebraIllegalArgumentException(oException.Message);
				}
			}
		}

		[JsonObject]
		private class FileObjectDetails
		{
			private string access;

			private string storage;

			private long size;

			private long free;

			[JsonProperty(PropertyName="access")]
			public string Access
			{
				get
				{
					return this.access;
				}
				set
				{
					this.access = value;
				}
			}

			[JsonProperty(PropertyName="free")]
			public long Free
			{
				get
				{
					return this.free;
				}
				set
				{
					this.free = value;
				}
			}

			[JsonProperty(PropertyName="size")]
			public long Size
			{
				get
				{
					return this.size;
				}
				set
				{
					this.size = value;
				}
			}

			[JsonProperty(PropertyName="storage")]
			public string Storage
			{
				get
				{
					return this.storage;
				}
				set
				{
					this.storage = value;
				}
			}

			public FileObjectDetails()
			{
			}
		}

		private class InternalCharacterFilteringOutputStream : BinaryWriter
		{
			public InternalCharacterFilteringOutputStream(Stream stream) : base(stream)
			{
			}

			public override void Write(byte[] buffer, int index, int count)
			{
				for (int i = 0; i < count; i++)
				{
					this.Write(buffer[index + i]);
				}
			}

			public override void Write(byte value)
			{
				if (value == ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR)
				{
					value = 126;
				}
				else if (value == ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR)
				{
					value = 44;
				}
				else if (value == ZPLUtilities.ZPL_INTERNAL_FORMAT_PREFIX_CHAR)
				{
					value = 94;
				}
				base.Write(value);
			}
		}
	}
}