using System;
using System.IO;

namespace Zebra.Sdk.Comm.Internal
{
	internal class FtpFileHolder
	{
		public string pathOnServer;

		public string fileName;

		public Stream fileStream;

		public FtpFileHolder(string pathOnServer, string fileName, Stream fileStream)
		{
			this.pathOnServer = pathOnServer;
			this.fileName = fileName;
			this.fileStream = fileStream;
		}
	}
}