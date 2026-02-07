using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Device;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class GraphicsConversionUtilZpl : GraphicsConversionUtil
	{
		public GraphicsConversionUtilZpl()
		{
		}

		private static string GetCorrectedFileName(PrinterFilePath parsedPath)
		{
			string drive = parsedPath.Drive;
			string fileName = parsedPath.FileName;
			string extension = parsedPath.Extension;
			if (extension == null || !Regex.IsMatch(extension, ".PNG", RegexOptions.IgnoreCase))
			{
				extension = ".GRF";
			}
			if (drive == null || drive.Length == 0)
			{
				drive = "E";
			}
			else if (drive.Length > 1)
			{
				throw new ZebraIllegalArgumentException(string.Concat("Invalid drive specified : ", drive));
			}
			return string.Concat(drive, ":", fileName, extension);
		}

		public override void SendImageToStream(string deviceDriveAndFileName, ZebraImageInternal image, int width, int height, Stream graphicOutputStream)
		{
			if (image == null)
			{
				throw new ZebraIllegalArgumentException("Invalid image file.");
			}
			image.ScaleImage(width, height);
			int num = (image.Width + 7) / 8;
			int num1 = num * image.Height;
			PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(deviceDriveAndFileName);
			StringBuilder stringBuilder = new StringBuilder();
			string correctedFileName = GraphicsConversionUtilZpl.GetCorrectedFileName(printerFilePath);
			if (printerFilePath.Extension == null || !Regex.IsMatch(printerFilePath.Extension, ".PNG", RegexOptions.IgnoreCase))
			{
				stringBuilder.Append("~DG");
				stringBuilder.Append(correctedFileName);
				stringBuilder.Append(",");
				stringBuilder.Append(num1);
				stringBuilder.Append(",");
				stringBuilder.Append(num);
				stringBuilder.Append(",");
				string str = ZPLUtilities.ReplaceAllWithInternalCharacters(stringBuilder.ToString());
				graphicOutputStream.Write(Encoding.UTF8.GetBytes(str), 0, str.Length);
				this.WriteImageDataToStream(image, graphicOutputStream);
				return;
			}
			byte[] ditheredB64EncodedPng = image.GetDitheredB64EncodedPng();
			stringBuilder.Append("~DY");
			stringBuilder.Append(correctedFileName.Substring(0, correctedFileName.IndexOf('.')));
			stringBuilder.Append(",p,p,");
			stringBuilder.Append((int)ditheredB64EncodedPng.Length);
			stringBuilder.Append(",");
			stringBuilder.Append(",:B64:");
			string str1 = ZPLUtilities.ReplaceAllWithInternalCharacters(stringBuilder.ToString());
			graphicOutputStream.Write(Encoding.UTF8.GetBytes(str1), 0, str1.Length);
			graphicOutputStream.Write(ditheredB64EncodedPng, 0, (int)ditheredB64EncodedPng.Length);
			byte[] numArray = new byte[] { 58 };
			graphicOutputStream.Write(numArray, 0, (int)numArray.Length);
			string cRC16ForZpl = ZCRC16.GetCRC16ForZpl(Encoding.UTF8.GetString(ditheredB64EncodedPng));
			graphicOutputStream.Write(Encoding.UTF8.GetBytes(cRC16ForZpl), 0, cRC16ForZpl.Length);
		}

		public void WriteImageDataToStream(ZebraImageInternal scaledImage, Stream graphicOutputStream)
		{
			Stream compressedBitmapOutputStreamZpl = null;
			try
			{
				compressedBitmapOutputStreamZpl = new CompressedBitmapOutputStreamZpl(graphicOutputStream);
				DitheredImageProvider.GetDitheredImage(scaledImage, compressedBitmapOutputStreamZpl);
			}
			finally
			{
				if (compressedBitmapOutputStreamZpl != null)
				{
					try
					{
						compressedBitmapOutputStreamZpl.Dispose();
					}
					catch (IOException)
					{
					}
				}
			}
		}
	}
}