using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer;

namespace Zebra.Sdk.Printer.Operations.Internal
{
	internal class FileRemover : PrinterOperationBase<object>
	{
		private List<string> fullPathsToFiles;

		public FileRemover(List<string> fullPathsToFiles, Connection connection, PrinterLanguage language) : base(connection, language)
		{
			this.fullPathsToFiles = fullPathsToFiles;
		}

		private void DeleteFiles()
		{
			foreach (string fullPathsToFile in this.fullPathsToFiles)
			{
				if (!base.IsPrintingChannelInLineMode())
				{
					(new PrinterCommandImpl(string.Concat("{}{\"file.delete\" : \"", fullPathsToFile, "\"}"))).SendAndWaitForValidJsonResponse(this.connection);
				}
				else
				{
					SGD.SET("file.delete", fullPathsToFile, this.connection);
				}
			}
		}

		public override object Execute()
		{
			this.SelectStatusChannelIfOpen();
			this.DeleteFiles();
			return null;
		}
	}
}