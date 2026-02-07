using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class FileStorer : PrinterOperationCaresAboutLinkOsVersion<List<PrinterObjectProperties>>
	{
		private List<PrinterFileDescriptor> fileDescriptors;

		public FileStorer(List<PrinterFileDescriptor> fileDescriptors, Connection connection, PrinterLanguage language, LinkOsInformation linkOsInformation) : base(connection, language, linkOsInformation)
		{
			this.fileDescriptors = fileDescriptors;
		}

		private List<PrinterObjectProperties> CreatePrinterFilePropertiesFromFileDescriptors()
		{
			List<PrinterObjectProperties> printerObjectProperties = new List<PrinterObjectProperties>();
			foreach (PrinterFileDescriptor fileDescriptor in this.fileDescriptors)
			{
				try
				{
					PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(fileDescriptor.Name);
					string str = string.Concat(printerFilePath.Drive.Substring(0, 1), ":");
					string fileName = printerFilePath.FileName;
					string str1 = printerFilePath.Extension.Substring(1);
					long fileSize = fileDescriptor.FileSize;
					printerObjectProperties.Add(new PrinterFilePropertiesCisdf(str, fileName, str1, fileSize));
				}
				catch (ZebraIllegalArgumentException)
				{
				}
			}
			return printerObjectProperties;
		}

		public override List<PrinterObjectProperties> Execute()
		{
			this.SelectProperChannel();
			this.IsOkToProceed();
			return this.StoreFile();
		}

		private void IsOkToProceed()
		{
			if (!base.IsLinkOs2_5_OrHigher() && (this.connection is StatusConnection || !this.connection.Connected))
			{
				throw new ConnectionException("Cannot store objects over status channel on this version of firmware");
			}
		}

		private bool ShouldSendMultipartForm()
		{
			if (!base.IsLinkOs2_5_OrHigher())
			{
				return false;
			}
			if (this.printerLanguage != PrinterLanguage.LINE_PRINT)
			{
				return true;
			}
			return this.connection is StatusConnection;
		}

		private List<PrinterObjectProperties> StoreFile()
		{
			List<PrinterObjectProperties> printerObjectProperties = new List<PrinterObjectProperties>();
			if (!this.ShouldSendMultipartForm())
			{
				CisdfFileSender.Send(this.connection, this.fileDescriptors);
				printerObjectProperties = this.CreatePrinterFilePropertiesFromFileDescriptors();
			}
			else
			{
				printerObjectProperties = MultipartFileSender.Send(this.connection, this.fileDescriptors);
			}
			return printerObjectProperties;
		}
	}
}