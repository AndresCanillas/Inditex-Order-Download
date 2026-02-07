using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Zebra.Sdk.Device.Internal
{
	internal class ProfileDisassembler
	{
		public ProfileDisassembler()
		{
		}

		public void Disassemble(Stream sourceStream, ProfileComponentHandler handler)
		{
			try
			{
				using (ZipArchive zipArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read))
				{
					foreach (ZipArchiveEntry entry in zipArchive.Entries)
					{
						string name = entry.Name;
						if (name == "settings.json")
						{
							handler.settingsHandler(entry.Open());
						}
						else if (name == "alerts.json")
						{
							handler.alertsHandler(entry.Open());
						}
						else if (name == "profileSupplement.txt")
						{
							handler.supplementHandler(entry.Open());
						}
						else if (name == "firmwareFile.txt")
						{
							handler.firmwareHandler(entry.Open());
						}
						else if (name == "firmwareFileUserSpecifiedName.txt")
						{
							handler.firmwareDisplayNameHandler(entry.Open());
						}
						else
						{
							handler.fileHandler(name, entry.Open());
						}
					}
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new IOException(exception.Message, exception.InnerException);
			}
		}
	}
}