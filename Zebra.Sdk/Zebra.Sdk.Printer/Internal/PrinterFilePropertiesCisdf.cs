using System;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class PrinterFilePropertiesCisdf : PrinterObjectProperties
	{
		public PrinterFilePropertiesCisdf(string drivePrefix, string fileName, string extension, long size)
		{
			this.drivePrefix = drivePrefix;
			this.fileName = fileName;
			this.extension = extension;
			this.fileSize = size;
		}
	}
}