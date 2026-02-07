using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Zebra.Sdk.Device.Internal
{
	internal class ProfileAssembler
	{
		private ZipArchive zos;

		public ProfileAssembler()
		{
		}

		public void AddAlerts(Stream sourceStream)
		{
			this.AddEntry("alerts.json", sourceStream);
		}

		private void AddEntry(string entryName, Stream sourceStream)
		{
			try
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(this.zos.CreateEntry(entryName).Open()))
				{
					byte[] numArray = null;
					using (BinaryReader binaryReader = new BinaryReader(sourceStream))
					{
						while (binaryReader.PeekChar() != -1)
						{
							long position = binaryReader.BaseStream.Position;
							numArray = binaryReader.ReadBytes(16384);
							binaryWriter.Write(numArray, 0, (int)numArray.Length);
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

		public void AddFiles(Dictionary<string, Stream> files)
		{
			foreach (string key in files.Keys)
			{
				this.AddEntry(key, files[key]);
			}
		}

		public void AddFirmware(string firmwareFileName, Stream sourceStream)
		{
			this.AddEntry("firmwareFileUserSpecifiedName.txt", new MemoryStream(Encoding.UTF8.GetBytes(firmwareFileName)));
			this.AddEntry("firmwareFile.txt", sourceStream);
		}

		public void AddSettings(Stream sourceStream)
		{
			this.AddEntry("settings.json", sourceStream);
		}

		public void AddSupplement(Stream sourceStream)
		{
			this.AddEntry("profileSupplement.txt", sourceStream);
		}

		public void Begin(Stream destinationStream)
		{
			try
			{
				this.zos = new ZipArchive(destinationStream, ZipArchiveMode.Create, true);
			}
			catch (InvalidDataException)
			{
			}
		}

		public void End()
		{
			this.zos.Dispose();
		}
	}
}