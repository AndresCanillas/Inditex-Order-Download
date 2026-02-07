using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class ImageStorer
	{
		private Connection connection;

		private PrinterLanguage language;

		private LinkOsInformation linkOsInformation;

		public ImageStorer(Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation)
		{
			this.connection = connection;
			this.language = language;
			this.linkOsInformation = linkOsInformation;
		}

		public void Execute(string deviceDriveAndFileName, ZebraImageI imageI, int width, int height)
		{
			using (ZebraImageInternal zebraImageInternal = (ZebraImageInternal)imageI)
			{
				if (zebraImageInternal == null)
				{
					throw new ZebraIllegalArgumentException("Invalid image file.");
				}
				zebraImageInternal.ScaleImage(width, height);
				try
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(deviceDriveAndFileName);
						string correctedFileName = ImageStorer.GetCorrectedFileName(printerFilePath);
						if (printerFilePath.Extension == null || !Regex.IsMatch(printerFilePath.Extension, ".PNG", RegexOptions.IgnoreCase))
						{
							using (DitheringStream ditheringStream = new DitheringStream(zebraImageInternal))
							{
								ditheringStream.CopyTo(memoryStream, 4096);
							}
						}
						else
						{
							int[] zebraSpecificPngHeader = Ditherer.GetZebraSpecificPngHeader(width, height);
							for (int i = 0; i < (int)zebraSpecificPngHeader.Length; i++)
							{
								memoryStream.WriteByte((byte)zebraSpecificPngHeader[i]);
							}
							zebraImageInternal.WriteDitheredPng(memoryStream);
						}
						List<PrinterFileDescriptor> printerFileDescriptors = new List<PrinterFileDescriptor>();
						memoryStream.Position = (long)0;
						printerFileDescriptors.Add(new PrinterFileDescriptor(memoryStream, correctedFileName, new PrinterFileMetadata(memoryStream)));
						(new FileStorer(printerFileDescriptors, this.connection, this.language, this.linkOsInformation)).Execute();
					}
				}
				catch (IOException oException)
				{
					throw new ConnectionException(oException.Message);
				}
			}
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
	}
}