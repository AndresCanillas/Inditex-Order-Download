using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;

namespace Zebra.Sdk.Util.Internal
{
	internal class FileUtilities
	{
		public FileUtilities()
		{
		}

		public static string ChangeExtension(string filePath, string newExtension)
		{
			if (!newExtension.Contains("."))
			{
				newExtension = string.Concat(".", newExtension);
			}
			if (filePath.Trim().EndsWith(newExtension))
			{
				return filePath;
			}
			int num = filePath.LastIndexOf('.');
			if (num < 0)
			{
				return string.Concat(filePath, newExtension);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(filePath.Substring(0, num));
			stringBuilder.Append(newExtension);
			return stringBuilder.ToString();
		}

		public static string GetFileNameOnPrinter(string filePath)
		{
			if (filePath == null)
			{
				throw new ZebraIllegalArgumentException("Invalid file path");
			}
			string name = (new FileInfo(filePath)).Name;
			if (name.Equals(""))
			{
				throw new ZebraIllegalArgumentException("Invalid file path");
			}
			return string.Concat("E:", StringUtilities.ConvertTo8dot3(name).ToUpper());
		}

		public static PrinterFilePath ParseDriveAndExtension(string printerDriveAndFileName)
		{
			string value = "";
			string str = "";
			string value1 = "";
			Regex regex = new Regex("^(([A-Za-z]+):)?([^.:]+)(\\.[^.]{0,3})?$");
			if (printerDriveAndFileName == null || printerDriveAndFileName.Length <= 0)
			{
				throw new ZebraIllegalArgumentException("Incorrect file name : ");
			}
			Match match = regex.Match(printerDriveAndFileName);
			if (!match.Success)
			{
				throw new ZebraIllegalArgumentException(string.Concat("Incorrect file name : ", printerDriveAndFileName));
			}
			value = match.Groups[2].Value;
			str = FileUtilities.TruncateAndReplaceSpaces(match.Groups[3].Value);
			value1 = match.Groups[4].Value;
			return new PrinterFilePath(value, str, value1);
		}

		public static void SendFileContentsInChunks(Connection connection, Stream inputStream)
		{
			if (!connection.Connected)
			{
				throw new ConnectionException("Connection is not open.");
			}
			try
			{
				byte[] numArray = new byte[4096];
				for (int i = inputStream.Read(numArray, 0, (int)numArray.Length); i > 0; i = inputStream.Read(numArray, 0, (int)numArray.Length))
				{
					connection.Write(numArray, 0, i);
				}
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
		}

		public static void SendFileContentsInChunks(Connection connection, ProgressMonitor handler, Stream inputStream, int fileSize)
		{
			if (!connection.Connected)
			{
				throw new ConnectionException("Connection is not open.");
			}
			try
			{
				int num = 4096;
				int num1 = fileSize;
				while (num1 > 0)
				{
					byte[] numArray = new byte[(num > num1 ? num1 : num)];
					int num2 = inputStream.Read(numArray, 0, (int)numArray.Length);
					connection.Write(numArray, 0, num2);
					num1 -= num2;
					handler.updateProgress(fileSize - num1, fileSize);
				}
			}
			catch (IOException oException)
			{
				throw new ConnectionException(oException.Message);
			}
		}

		private static string TruncateAndReplaceSpaces(string fileName)
		{
			return StringUtilities.ConvertTo16dot3(fileName).Replace(" ", "_");
		}
	}
}