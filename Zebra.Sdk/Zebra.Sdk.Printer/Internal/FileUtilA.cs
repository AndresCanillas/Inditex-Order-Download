using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Device.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class FileUtilA : FileUtil
	{
		protected Connection printerConnection;

		private const string LEGACY_FILE_DIR_KEYWORD = "Directory";

		private const string EPOCH_FILE_DIR_KEYWORD = "DIR";

		public FileUtilA(Connection printerConnection)
		{
			this.printerConnection = printerConnection;
		}

		public virtual PrinterFilePropertiesList ExtractFilePropertiesFromDirResult(string dirResult)
		{
			if (dirResult == null)
			{
				throw new ZebraIllegalArgumentException("No files found.");
			}
			PrinterFilePropertiesList printerFilePropertiesList = new PrinterFilePropertiesList();
			if (!dirResult.Contains("DIR"))
			{
				if (!dirResult.Contains("Directory"))
				{
					throw new ZebraIllegalArgumentException("No files found.");
				}
				foreach (object obj in (new Regex("\\s+([^\\s]+)\\s+\\.([^\\s]+)\\s+(\\d+)", RegexOptions.Multiline)).Matches(dirResult))
				{
					GroupCollection groups = ((Match)obj).Groups;
					printerFilePropertiesList.Add(new PrinterFilePropertiesZpl("", groups[1].ToString(), groups[2].ToString(), long.Parse(groups[3].ToString())));
				}
			}
			else
			{
				foreach (object obj1 in (new Regex("\\*\\s+([^\\s]+\\:)([^\\s]+)\\.([^\\s]+)\\s+(\\d+)", RegexOptions.Multiline)).Matches(dirResult))
				{
					GroupCollection groupCollections = ((Match)obj1).Groups;
					printerFilePropertiesList.Add(new PrinterFilePropertiesZpl(groupCollections[1].ToString(), groupCollections[2].ToString(), groupCollections[3].ToString(), long.Parse(groupCollections[4].ToString())));
				}
			}
			return printerFilePropertiesList;
		}

		protected virtual ZebraFileConnection GetFileConnection(string filePath)
		{
			return new ZebraFileConnectionImpl(filePath);
		}

		public abstract string[] RetrieveFileNames();

		public abstract string[] RetrieveFileNames(string[] extensions);

		public virtual PrinterFilePropertiesList RetrieveFilePropertiesFromPrinter()
		{
			return this.ExtractFilePropertiesFromDirResult(SGD.DO("file.dir", "", this.printerConnection));
		}

		public abstract List<PrinterObjectProperties> RetrieveObjectsProperties();

		public void SendFileContents(string filePath)
		{
			this.SendFileContents(filePath, new NullProgressMonitor());
		}

		public void SendFileContents(string filePath, ProgressMonitor handler)
		{
			try
			{
				ZebraFileConnection fileConnection = this.GetFileConnection(filePath);
				using (Stream stream = ((ZebraFileConnectionImpl)fileConnection).OpenInputStream())
				{
					int fileSize = fileConnection.FileSize;
					FileUtilities.SendFileContentsInChunks(this.printerConnection, handler, stream, fileSize);
				}
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new ConnectionException(oException.Message, oException);
			}
		}
	}
}