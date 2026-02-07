using System;
using System.IO;
using System.Text;

namespace Zebra.Sdk.Util.Internal
{
	internal class FileReader
	{
		public FileReader()
		{
		}

		public static byte[] ToByteArray(string fullPath, int maxSize)
		{
			byte[] numArray;
			if (string.IsNullOrEmpty(fullPath))
			{
				return null;
			}
			byte[] numArray1 = null;
			try
			{
				using (FileStream fileStream = File.OpenRead(fullPath))
				{
					long length = fileStream.Length;
					int num = 0;
					if (length >= (long)maxSize)
					{
						numArray = null;
						return numArray;
					}
					else
					{
						num = (int)length;
						numArray1 = new byte[num];
						try
						{
							if (num != fileStream.Read(numArray1, 0, (int)numArray1.Length))
							{
								numArray = null;
								return numArray;
							}
						}
						catch
						{
							numArray = null;
							return numArray;
						}
					}
				}
				return numArray1;
			}
			catch (FileNotFoundException)
			{
				numArray = null;
			}
			catch (DirectoryNotFoundException)
			{
				numArray = null;
			}
			return numArray;
		}

		public static byte[] ToByteArray(string fullPath)
		{
			return FileReader.ToByteArray(fullPath, 2147483647);
		}

		public static byte[] ToByteArray(FileInfo fullPath)
		{
			return FileReader.ToByteArray(fullPath.FullName);
		}

		public static string ToString(string fullPath)
		{
			return FileReader.ToString(fullPath, 2147483647);
		}

		public static string ToString(string fullPath, int maxSize)
		{
			byte[] byteArray = FileReader.ToByteArray(fullPath, maxSize);
			if (byteArray == null)
			{
				return "";
			}
			return Encoding.GetEncoding(0).GetString(byteArray);
		}

		public static string ToString(FileInfo font)
		{
			return FileReader.ToString(font.FullName);
		}

		public static string ToUtf8String(string fullPath)
		{
			return FileReader.ToUtf8String(fullPath, 2147483647);
		}

		public static string ToUtf8String(string fullPath, int maxSize)
		{
			string str;
			byte[] byteArray = FileReader.ToByteArray(fullPath, maxSize);
			if (byteArray == null)
			{
				return "";
			}
			try
			{
				str = Encoding.UTF8.GetString(byteArray);
			}
			catch (Exception)
			{
				str = null;
			}
			return str;
		}
	}
}