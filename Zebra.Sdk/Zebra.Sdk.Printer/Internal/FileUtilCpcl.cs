using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FileUtilCpcl : FileUtilA
	{
		public FileUtilCpcl(Connection printerConnection) : base(printerConnection)
		{
		}

		public override string[] RetrieveFileNames()
		{
			return this.RetrieveFilePropertiesFromPrinter().GetFileNamesFromProperties();
		}

		public override string[] RetrieveFileNames(string[] extensions)
		{
			return this.RetrieveFilePropertiesFromPrinter().FilterByExtension(extensions).GetFileNamesFromProperties();
		}

		public override List<PrinterObjectProperties> RetrieveObjectsProperties()
		{
			return this.RetrieveFilePropertiesFromPrinter().GetObjectsProperties();
		}
	}
}