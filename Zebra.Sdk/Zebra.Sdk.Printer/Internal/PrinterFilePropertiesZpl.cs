using System;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterFilePropertiesZpl : PrinterObjectProperties
	{
		public PrinterFilePropertiesZpl(string drivePrefix, string fileName, string extension, long size)
		{
			this.drivePrefix = drivePrefix;
			this.fileName = fileName;
			this.extension = extension;
			this.fileSize = size;
		}

		public PrinterFilePropertiesZpl(string drivePrefix, string fileName, string extension, long size, long crc32)
		{
			this.drivePrefix = drivePrefix;
			this.fileName = fileName;
			this.extension = extension;
			this.fileSize = size;
			this.crc32 = crc32;
		}
	}
}