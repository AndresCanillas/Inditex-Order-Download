using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer.Internal;

namespace Zebra.Sdk.Util.Internal
{
	internal class FileWrapper
	{
		public const int SIZE_OF_DZ_HEADER = 16;

		public const int SIZE_OF_LINKOS_DZ_HEADER = 24;

		public const string CPCL_CRC16_FILE_HEADER_FOR_FLASH = "! CISDFCRC16";

		public const string CPCL_CRC16_FILE_HEADER_FOR_RAM = "! CISDFRCRC16";

		private readonly static HashSet<string> validExtensionsToGetHzo;

		public static byte[] CisdfTrailer
		{
			get
			{
				return Encoding.UTF8.GetBytes("\r\n\r\n");
			}
		}

		static FileWrapper()
		{
			FileWrapper.validExtensionsToGetHzo = new HashSet<string>(new string[] { "FNT", "ZPL", "GRF", "DAT", "STO", "PNG" });
		}

		private FileWrapper()
		{
		}

		private static bool AlreadyContainsHeader(byte[] fileContents)
		{
			string str = Encoding.UTF8.GetString(fileContents).Trim();
			if (str.StartsWith("! CISDFCRC16"))
			{
				return true;
			}
			return str.StartsWith("! CISDFRCRC16");
		}

		private static string ConvertTo16dot3(string fileNameOnPrinter)
		{
			string str;
			int num;
			try
			{
				PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(fileNameOnPrinter);
				num = (printerFilePath.FileName.Length > 16 ? 16 : printerFilePath.FileName.Length);
				string str1 = printerFilePath.FileName.Substring(0, num);
				str = (new PrinterFilePath(printerFilePath.Drive, str1, printerFilePath.Extension)).ToString();
			}
			catch (ZebraIllegalArgumentException)
			{
				str = "";
			}
			return str;
		}

		public static string CreateCisdfHeader(PrinterFileDescriptor fileDescriptor)
		{
			string headerString = FileWrapper.GetHeaderString(fileDescriptor.Name);
			string str = FileWrapper.ConvertTo16dot3(fileDescriptor.Name);
			long fileSize = fileDescriptor.FileSize;
			string upper = StringUtilities.StringPadToPlaces(8, "0", fileSize.ToString("X4"), false).ToUpper();
			return string.Concat(new string[] { headerString, "\r\n", fileDescriptor.Crc16, "\r\n", str, "\r\n", upper, "\r\n", fileDescriptor.CheckSum, "\r\n" });
		}

		public static byte[] DecodeHZOData(string data)
		{
			data.IndexOf("Z64:");
			int num = data.IndexOf("64:") + 3;
			int num1 = data.LastIndexOf(":");
			if (num < 0 || num1 <= num)
			{
				return null;
			}
			return Convert.FromBase64String(data.Substring(num, num1 - num));
		}

		private static int FindStartOfNextLine(string fileAsString, int currentPosition)
		{
			bool flag = false;
			int num = 0;
			int num1 = currentPosition;
			while (num1 < fileAsString.Length)
			{
				char chr = fileAsString[num1];
				if (!flag || chr == '\r' || chr == '\n')
				{
					if (chr == '\r' || chr == '\n')
					{
						flag = true;
					}
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

		public static string GetFileName(FileStream file)
		{
			string name = file.Name;
			string str = "";
			if (name.Contains("."))
			{
				str = name.Substring(name.LastIndexOf('.') + 1);
				if (str.Length > 3)
				{
					str = str.Substring(0, 3);
				}
				name = name.Substring(0, name.LastIndexOf('.'));
			}
			if (name.Length > 8)
			{
				name = name.Substring(0, 8);
			}
			return string.Concat(name.ToUpper(), (str.Equals("") ? "" : string.Concat(".", str.ToUpper())));
		}

		private static string GetHeaderString(string fileNameOnPrinter)
		{
			string str = "! CISDFCRC16";
			try
			{
				string drive = FileUtilities.ParseDriveAndExtension(fileNameOnPrinter).Drive;
				if (drive != null && drive.ToUpper().StartsWith("R"))
				{
					str = "! CISDFRCRC16";
				}
			}
			catch (ZebraIllegalArgumentException)
			{
			}
			return str;
		}

		public static bool IsHzoExtension(string ext)
		{
			return FileWrapper.validExtensionsToGetHzo.Contains(ext);
		}

		public static byte[] StripDzHeaderFromObjectData(string filePath, byte[] decodedBytes)
		{
			byte[] numArray;
			byte num = 0;
			int num1 = 16;
			if (decodedBytes[8] != num)
			{
				num1 = 24;
			}
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				binaryWriter.Write(decodedBytes, num1, (int)decodedBytes.Length - num1);
				byte[] array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
				numArray = (!Regex.IsMatch(filePath.Substring(filePath.LastIndexOf(".") + 1), "ZPL", RegexOptions.IgnoreCase) ? array : ZPLUtilities.ReplaceInternalCharactersWithReadableCharacters(array));
			}
			return numArray;
		}

		public static void StripDzHeaderFromObjectData(Stream destinationStream, string filePath, byte[] decodedBytes)
		{
			byte num = 0;
			int num1 = 16;
			if (decodedBytes[8] != num)
			{
				num1 = 24;
			}
			if (!Regex.IsMatch(filePath.Substring(filePath.LastIndexOf('.') + 1), "ZPL", RegexOptions.IgnoreCase))
			{
				try
				{
					destinationStream.Write(decodedBytes, num1, (int)decodedBytes.Length - num1);
				}
				catch (IOException oException)
				{
					throw new ZebraIllegalArgumentException(oException.Message);
				}
			}
			else
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream(decodedBytes, num1, (int)decodedBytes.Length - num1)))
				{
					ZPLUtilities.ReplaceInternalCharactersWithReadableCharacters(destinationStream, binaryWriter.BaseStream);
				}
			}
		}

		public static byte[] StripOffCISDFWrapper(byte[] wrappedFileContents)
		{
			string str = Encoding.UTF8.GetString(wrappedFileContents);
			if (!str.StartsWith("! CISDFCRC16") && !str.StartsWith("! CISDFRCRC16"))
			{
				return wrappedFileContents;
			}
			int num = 0;
			for (int i = 0; i < 5; i++)
			{
				num = FileWrapper.FindStartOfNextLine(str, num);
			}
			string str1 = str.Substring(num);
			return Encoding.UTF8.GetBytes(str1.Trim());
		}

		public static void UnwrapHZOResult(Stream destinationStream, string filePath, string fileOverHZO)
		{
			byte[] numArray = FileWrapper.DecodeHZOData(fileOverHZO);
			if (numArray == null || (int)numArray.Length <= 16)
			{
				throw new ZebraIllegalArgumentException(string.Concat("Malformed response from printer for ", filePath));
			}
			FileWrapper.StripDzHeaderFromObjectData(destinationStream, filePath, numArray);
		}

		public static string WrapFile(FileStream sourceFile, string fileNameOnPrinter, bool hidden, bool persistent)
		{
			return FileWrapper.WrapFile(FileReader.ToByteArray((new FileInfo(sourceFile.Name)).FullName), fileNameOnPrinter, hidden, persistent);
		}

		public static string WrapFile(byte[] fileContents, string fileNameOnPrinter, bool hidden, bool persistent)
		{
			byte[] header = null;
			PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(fileNameOnPrinter);
			string str = fileNameOnPrinter.Substring(fileNameOnPrinter.LastIndexOf('.'));
			header = DZHeader.GetHeader(fileContents, fileNameOnPrinter, hidden, persistent);
			byte[] array = null;
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				try
				{
					binaryWriter.Write(header);
					binaryWriter.Write(fileContents);
				}
				catch (IOException)
				{
				}
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			string base64String = Convert.ToBase64String(array);
			string cRC16ForZpl = ZCRC16.GetCRC16ForZpl(base64String);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX);
			stringBuilder.Append("DZ");
			stringBuilder.Append(printerFilePath.Drive);
			stringBuilder.Append(":");
			stringBuilder.Append(printerFilePath.FileName);
			stringBuilder.Append(str);
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER);
			stringBuilder.Append((int)array.Length);
			stringBuilder.Append(ZPLUtilities.ZPL_INTERNAL_DELIMITER);
			stringBuilder.Append(":B64:");
			stringBuilder.Append(base64String);
			stringBuilder.Append(":");
			stringBuilder.Append(cRC16ForZpl);
			return stringBuilder.ToString();
		}

		public static byte[] WrapFileWithCisdfHeader(byte[] fileContents, string fileNameOnPrinter)
		{
			byte[] array;
			if (FileWrapper.AlreadyContainsHeader(fileContents))
			{
				return fileContents;
			}
			string headerString = FileWrapper.GetHeaderString(fileNameOnPrinter);
			string upper = ZCRC16.GetCRC16ForCisdfHeader(fileContents).ToUpper();
			string str = FileWrapper.ConvertTo16dot3(fileNameOnPrinter);
			int length = (int)fileContents.Length;
			string upper1 = StringUtilities.StringPadToPlaces(8, "0", length.ToString("X4"), false).ToUpper();
			string str1 = ZCRC16.GetWChecksum(fileContents).ToUpper();
			string str2 = string.Concat(new string[] { headerString, "\r\n", upper, "\r\n", str, "\r\n", upper1, "\r\n", str1, "\r\n" });
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				try
				{
					binaryWriter.Write(Encoding.UTF8.GetBytes(str2));
					binaryWriter.Write(fileContents);
					binaryWriter.Write(FileWrapper.CisdfTrailer);
				}
				catch (IOException)
				{
				}
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}
	}
}