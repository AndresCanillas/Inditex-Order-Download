using System;
using System.IO;

namespace Zebra.Sdk.Util.Internal
{
	internal class PrinterFileDescriptor : IDisposable
	{
		private string name;

		private Stream sourceStream;

		private PrinterFileMetadata metaData;

		private bool disposedValue;

		public string CheckSum
		{
			get
			{
				return this.metaData.CheckSum;
			}
		}

		public string Crc16
		{
			get
			{
				return this.metaData.Crc16;
			}
		}

		public long FileSize
		{
			get
			{
				return this.metaData.FileSize;
			}
		}

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public Stream SourceStream
		{
			get
			{
				return this.sourceStream;
			}
		}

		public PrinterFileDescriptor(Stream sourceStream, string name, PrinterFileMetadata metaData)
		{
			if (name == null || 2 >= name.Length)
			{
				throw new ArgumentException("File name not provided");
			}
			if (name[1] != ':')
			{
				throw new ArgumentException("Drive letter not specified");
			}
			this.name = name;
			Stream stream = sourceStream;
			if (stream == null)
			{
				throw new ArgumentException("Source stream is null");
			}
			this.sourceStream = stream;
			this.metaData = metaData;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing && this.sourceStream != null)
				{
					this.sourceStream.Dispose();
				}
				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}
	}
}