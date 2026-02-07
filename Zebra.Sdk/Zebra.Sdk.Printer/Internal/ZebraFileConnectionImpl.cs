using System;
using System.IO;

namespace Zebra.Sdk.Printer.Internal
{
	internal class ZebraFileConnectionImpl : ZebraFileConnection
	{
		private FileInfo fi;

		public override int FileSize
		{
			get
			{
				return (int)this.fi.Length;
			}
		}

		public ZebraFileConnectionImpl(string filePath)
		{
			if (!string.IsNullOrEmpty(filePath))
			{
				this.fi = new FileInfo(filePath);
			}
		}

		public override void Close()
		{
		}

		public override Stream OpenInputStream()
		{
			Stream fileStream;
			try
			{
				fileStream = new FileStream(this.fi.FullName, FileMode.Open);
			}
			catch (FileNotFoundException fileNotFoundException1)
			{
				FileNotFoundException fileNotFoundException = fileNotFoundException1;
				throw new IOException(fileNotFoundException.Message, fileNotFoundException);
			}
			return fileStream;
		}
	}
}