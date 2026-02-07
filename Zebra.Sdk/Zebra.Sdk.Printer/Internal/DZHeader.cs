using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class DZHeader
	{
		private const byte blank = 0;

		private DZHeader()
		{
		}

		private static byte GetFlags(bool hidden, bool persistent, string fileName)
		{
			byte num = 0;
			if (hidden)
			{
				num = (byte)(num | 64);
			}
			if (persistent)
			{
				num = (byte)(num | 32);
			}
			if (fileName.Length > 8)
			{
				num = (byte)(num | 8);
			}
			return num;
		}

		public static byte[] GetHeader(byte[] fileContents, string fileNameOnPrinter, bool hidden, bool persistent)
		{
			return DZHeader.GetHeaderHelper(DZHeader.GetLength(fileContents), fileNameOnPrinter, hidden, persistent);
		}

		private static byte[] GetHeaderHelper(byte[] length, string fileNameOnPrinter, bool hidden, bool persistent)
		{
			byte[] array;
			string fileName = FileUtilities.ParseDriveAndExtension(fileNameOnPrinter).FileName;
			string places = StringUtilities.StringPadToPlaces((fileName.Length > 8 ? 17 : 9), '\0', fileName, true);
			byte flags = DZHeader.GetFlags(hidden, persistent, fileName);
			byte type = DZHeader.GetType(fileNameOnPrinter);
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				try
				{
					binaryWriter.Write(Encoding.UTF8.GetBytes(places));
					binaryWriter.Write(flags);
					binaryWriter.Write(type);
					binaryWriter.Write((byte)0);
					binaryWriter.Write(length);
				}
				catch (IOException)
				{
				}
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		private static byte[] GetLength(byte[] fileContents)
		{
			return DZHeader.GetLength((int)fileContents.Length);
		}

		private static byte[] GetLength(int fileDataLength)
		{
			byte[] bytes = BitConverter.GetBytes(fileDataLength);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			return StringUtilities.ByteArrayPadToPlaces(4, bytes);
		}

		private static byte GetType(string fileName)
		{
			string str = "";
			if (fileName.Contains("."))
			{
				str = fileName.Substring(fileName.LastIndexOf('.') + 1);
			}
			return (byte)Extension.GetTypeValue(str);
		}
	}
}