using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Zebra.Sdk.Util.Internal
{
	internal class ZipUtil
	{
		private readonly string pathToZipFile;

		private FileStream fis;

		private ZipArchive zin;

		public ZipUtil(string pathToZipFile)
		{
			this.pathToZipFile = pathToZipFile;
		}

		public void AddEntry(string entryName, byte[] entryContents)
		{
			this.AddEntry(entryName, entryContents, null);
		}

		public void AddEntry(string entryName, byte[] entryContents, byte[] extraData)
		{
			if (string.IsNullOrEmpty(this.pathToZipFile) || string.IsNullOrEmpty(entryName) || entryContents == null)
			{
				throw new ArgumentException();
			}
			this.RemoveEntry(entryName);
			HashSet<ZipUtil.EntryData> entryDatas = this.ReadEntries();
			using (ZipUtil.EntryData entryDatum = new ZipUtil.EntryData(entryName, entryContents, extraData))
			{
				entryDatas.Add(entryDatum);
				this.WriteEntries(entryDatas);
			}
		}

		public void AddEntry(string entryName, FileStream file)
		{
			this.AddEntry(entryName, file, null);
		}

		public void AddEntry(string entryName, FileStream file, byte[] extraData)
		{
			if (string.IsNullOrEmpty(this.pathToZipFile) || string.IsNullOrEmpty(entryName) || file == null)
			{
				throw new ArgumentException();
			}
			this.RemoveEntry(entryName);
			HashSet<ZipUtil.EntryData> entryDatas = this.ReadEntries();
			using (ZipUtil.EntryData entryDatum = new ZipUtil.EntryData(entryName, file, extraData))
			{
				entryDatas.Add(entryDatum);
				this.WriteEntries(entryDatas);
			}
		}

		public void CloseStreams()
		{
			if (this.zin != null)
			{
				this.zin.Dispose();
				this.zin = null;
			}
			if (this.fis != null)
			{
				this.fis.Dispose();
				this.fis = null;
			}
		}

		public bool ContainsEntry(string entryName)
		{
			bool flag = false;
			try
			{
				try
				{
					this.OpenStreams();
					if (this.zin.GetEntry(entryName) != null)
					{
						flag = true;
					}
				}
				catch
				{
				}
			}
			finally
			{
				this.CloseStreams();
			}
			return flag;
		}

		public void ExtractEntry(string fullPathOfTargetFile, string entryName)
		{
			try
			{
				try
				{
					this.OpenStreams();
					ZipArchiveEntry entry = this.zin.GetEntry(entryName);
					if (entry != null)
					{
						entry.ExtractToFile(fullPathOfTargetFile, true);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					throw new IOException(exception.Message, exception);
				}
			}
			finally
			{
				this.CloseStreams();
			}
		}

		public byte[] ExtractEntry(string entryName)
		{
			byte[] numArray = new byte[0];
			try
			{
				try
				{
					this.OpenStreams();
					ZipArchiveEntry entry = this.zin.GetEntry(entryName);
					if (entry != null)
					{
						using (Stream stream = entry.Open())
						{
							Array.Resize<byte>(ref numArray, (int)entry.Length);
							stream.Read(numArray, 0, (int)numArray.Length);
						}
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					throw new IOException(exception.Message, exception);
				}
			}
			finally
			{
				this.CloseStreams();
			}
			return numArray;
		}

		public byte[] ExtractExtraFromEntry()
		{
			byte[] numArray = new byte[0];
			try
			{
				this.OpenStreams();
				ZipArchiveEntry entry = this.zin.GetEntry("firmwareFileUserSpecifiedName.txt");
				if (entry != null)
				{
					using (Stream stream = entry.Open())
					{
						Array.Resize<byte>(ref numArray, (int)entry.Length);
						stream.Read(numArray, 0, (int)numArray.Length);
					}
				}
			}
			finally
			{
				this.CloseStreams();
			}
			return numArray;
		}

		public string GetEntryContents(string fileNameInsideZipFile)
		{
			return Encoding.UTF8.GetString(this.ExtractEntry(fileNameInsideZipFile));
		}

		public string GetEntryExtraContent()
		{
			byte[] numArray = this.ExtractExtraFromEntry();
			if (numArray == null)
			{
				return "";
			}
			return Encoding.UTF8.GetString(numArray);
		}

		public List<string> GetEntryNames()
		{
			List<string> strs = new List<string>();
			try
			{
				try
				{
					this.OpenStreams();
					foreach (ZipArchiveEntry entry in this.zin.Entries)
					{
						strs.Add(entry.FullName);
					}
				}
				catch (Exception)
				{
				}
			}
			finally
			{
				this.CloseStreams();
			}
			return strs;
		}

		public Stream GetInputStreamToEntry(string entryName)
		{
			Stream stream;
			try
			{
				try
				{
					this.OpenStreams();
					ZipArchiveEntry entry = this.zin.GetEntry(entryName);
					if (entry == null)
					{
						return null;
					}
					else
					{
						stream = entry.Open();
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					throw new IOException(exception.Message, exception);
				}
			}
			finally
			{
				this.CloseStreams();
			}
			return stream;
		}

		private void OpenStreams()
		{
			this.CloseStreams();
			this.fis = new FileStream(this.pathToZipFile, FileMode.Open);
			this.zin = new ZipArchive(this.fis);
		}

		private HashSet<ZipUtil.EntryData> ReadEntries()
		{
			List<string> entryNames = this.GetEntryNames();
			HashSet<ZipUtil.EntryData> entryDatas = new HashSet<ZipUtil.EntryData>();
			foreach (string entryName in entryNames)
			{
				try
				{
					using (ZipUtil.EntryData entryDatum = new ZipUtil.EntryData(entryName, this.ExtractEntry(entryName)))
					{
						entryDatas.Add(entryDatum);
					}
				}
				catch (IOException oException)
				{
					Console.WriteLine(oException.StackTrace);
				}
			}
			return entryDatas;
		}

		public void RemoveEntry(string entryName)
		{
			if (string.IsNullOrEmpty(this.pathToZipFile) || string.IsNullOrEmpty(entryName))
			{
				throw new ArgumentException();
			}
			if (File.Exists(this.pathToZipFile))
			{
				using (ZipArchive zipArchive = ZipFile.Open(this.pathToZipFile, ZipArchiveMode.Update))
				{
					ZipArchiveEntry entry = zipArchive.GetEntry(entryName);
					if (entry != null)
					{
						entry.Delete();
					}
				}
			}
		}

		private void WriteEntries(HashSet<ZipUtil.EntryData> entries)
		{
			ZipArchiveMode zipArchiveMode = ZipArchiveMode.Create;
			if (File.Exists(this.pathToZipFile))
			{
				zipArchiveMode = ZipArchiveMode.Update;
			}
			using (ZipArchive zipArchive = ZipFile.Open(this.pathToZipFile, zipArchiveMode))
			{
				foreach (ZipUtil.EntryData entry in entries)
				{
					ZipArchiveEntry zipArchiveEntry = null;
					if (zipArchiveMode == ZipArchiveMode.Update)
					{
						zipArchiveEntry = zipArchive.GetEntry(entry.Name);
					}
					if (zipArchiveEntry == null)
					{
						zipArchiveEntry = zipArchive.CreateEntry(entry.Name);
					}
					using (Stream stream = zipArchiveEntry.Open())
					{
						if (entry.SrcData != null)
						{
							stream.Write(entry.SrcData, 0, (int)entry.SrcData.Length);
						}
						else if (entry.SrcFile != null)
						{
							using (FileStream srcFile = entry.SrcFile)
							{
								byte[] numArray = new byte[16384];
								int num = 0;
								while (true)
								{
									int num1 = srcFile.Read(numArray, 0, (int)numArray.Length);
									num = num1;
									if (num1 <= 0)
									{
										break;
									}
									stream.Write(numArray, 0, num);
								}
							}
						}
					}
					if (entry.ExtraData == null)
					{
						continue;
					}
					zipArchiveEntry = zipArchive.CreateEntry("firmwareFileUserSpecifiedName.txt");
					using (Stream stream1 = zipArchiveEntry.Open())
					{
						stream1.Write(entry.ExtraData, 0, (int)entry.ExtraData.Length);
					}
				}
			}
		}

		private class EntryData : IDisposable
		{
			private byte[] extraData;

			private string name;

			private FileStream srcFile;

			private byte[] srcData;

			private bool disposedValue;

			public byte[] ExtraData
			{
				get
				{
					return this.extraData;
				}
			}

			public string Name
			{
				get
				{
					return this.name;
				}
			}

			public byte[] SrcData
			{
				get
				{
					return this.srcData;
				}
			}

			public FileStream SrcFile
			{
				get
				{
					return this.srcFile;
				}
			}

			public EntryData(string name)
			{
				this.name = name;
			}

			public EntryData(string name, byte[] srcData)
			{
				this.name = name;
				this.srcData = srcData;
			}

			public EntryData(string name, byte[] srcData, byte[] extraData)
			{
				this.name = name;
				this.srcData = srcData;
				this.extraData = extraData;
			}

			public EntryData(string name, FileStream srcFile)
			{
				this.name = name;
				this.srcFile = srcFile;
			}

			public EntryData(string name, FileStream srcFile, byte[] extraData)
			{
				this.name = name;
				this.srcFile = srcFile;
				this.extraData = extraData;
			}

			protected virtual void Dispose(bool disposing)
			{
				if (!this.disposedValue && disposing)
				{
					if (this.srcFile != null)
					{
						this.srcFile.Dispose();
					}
					this.disposedValue = true;
				}
			}

			public void Dispose()
			{
				this.Dispose(true);
			}

			public override bool Equals(object obj)
			{
				return ((ZipUtil.EntryData)obj).name.Equals(this.name);
			}

			public override int GetHashCode()
			{
				return this.name.GetHashCode();
			}
		}
	}
}