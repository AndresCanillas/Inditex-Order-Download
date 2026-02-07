//using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Comm.Internal
{
	internal class FTP : IDisposable
	{
		private const string DEFAULT_USER = "user";

		private const string DEFAULT_PASSWORD = "1234";

		public FTP(string server, string user, string password)
		{
			throw new NotImplementedException();
			//this.server = server;
			//this.user = user ?? "user";
			//this.password = password ?? "1234";
			//this.client = new FtpClient(this.server, this.user, this.password);
		}

		public void DeleteAllFilesAndSubDirectories(List<string> directories)
		{
			throw new NotImplementedException();
			//try
			//{
			//	try
			//	{
			//		this.client.Connect();
			//		this.client.DownloadDataType = FtpDataType.Binary;
			//		this.client.UploadDataType = FtpDataType.Binary;
			//		foreach (string directory in directories)
			//		{
			//			string str = directory;
			//			if (!directory.EndsWith("/"))
			//			{
			//				str = string.Concat(str, "/");
			//			}
			//			this.client.SetWorkingDirectory(directory);
			//			FtpListItem[] listing = this.client.GetListing();
			//			for (int i = 0; i < (int)listing.Length; i++)
			//			{
			//				this.DeleteFile(listing[i], this.client);
			//			}
			//		}
			//	}
			//	catch (Exception exception1)
			//	{
			//		Exception exception = exception1;
			//		throw new ConnectionException(exception.Message, exception);
			//	}
			//}
			//finally
			//{
			//	if (this.client.IsConnected)
			//	{
			//		this.client.Disconnect();
			//	}
			//}
		}

		//private void DeleteFile(FtpListItem ftpFile, FtpClient client)
		//{
		//	if (ftpFile.Type == FtpFileSystemObjectType.Directory)
		//	{
		//		client.SetWorkingDirectory(ftpFile.Name);
		//		FtpListItem[] listing = client.GetListing();
		//		for (int i = 0; i < (int)listing.Length; i++)
		//		{
		//			this.DeleteFile(listing[i], client);
		//		}
		//		client.SetWorkingDirectory("..");
		//		client.DeleteDirectory(ftpFile.Name);
		//	}
		//	client.DeleteFile(ftpFile.Name);
		//}

		protected virtual void Dispose(bool disposing)
		{
			throw new NotImplementedException();
			//if (!this.disposedValue)
			//{
			//	if (disposing && this.client != null)
			//	{
			//		this.client.Dispose();
			//	}
			//	this.disposedValue = true;
			//}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		public void GetFile(Stream destination, string fullPath)
		{
			throw new NotImplementedException();
			//try
			//{
			//	try
			//	{
			//		this.client.Connect();
			//		this.client.DownloadDataType = FtpDataType.Binary;
			//		this.client.UploadDataType = FtpDataType.Binary;
			//		using (Stream stream = this.client.OpenRead(fullPath))
			//		{
			//			stream.CopyTo(destination);
			//		}
			//		if (!this.client.GetReply().Success)
			//		{
			//			throw new ConnectionException("Could not connect to printer over FTP, make sure FTP is enabled");
			//		}
			//	}
			//	catch (ConnectionException connectionException)
			//	{
			//		throw;
			//	}
			//	catch (Exception exception1)
			//	{
			//		Exception exception = exception1;
			//		throw new ConnectionException(exception.Message, exception);
			//	}
			//}
			//finally
			//{
			//	if (this.client.IsConnected)
			//	{
			//		this.client.Disconnect();
			//	}
			//}
		}

		public void PutFile(string pathOnServer, string fileName, byte[] fileContents)
		{
			this.PutFile(pathOnServer, fileName, new MemoryStream(fileContents));
		}

		public void PutFile(string pathOnServer, string fileName, Stream stream)
		{
			this.PutFiles(new List<FtpFileHolder>(new FtpFileHolder[] { new FtpFileHolder(pathOnServer, fileName, stream) }));
		}

		public void PutFiles(List<FtpFileHolder> files)
		{
			throw new NotImplementedException();
			//try
			//{
			//	try
			//	{
			//		this.client.Connect();
			//		this.client.DownloadDataType = FtpDataType.Binary;
			//		this.client.UploadDataType = FtpDataType.Binary;
			//		foreach (FtpFileHolder file in files)
			//		{
			//			if (!file.pathOnServer.EndsWith("/"))
			//			{
			//				FtpFileHolder ftpFileHolder = file;
			//				ftpFileHolder.pathOnServer = string.Concat(ftpFileHolder.pathOnServer, "/");
			//			}
			//			this.client.CreateDirectory(file.pathOnServer);
			//			string str = file.fileName.Replace(":", "_");
			//			using (Stream stream = this.client.OpenWrite(string.Concat(file.pathOnServer, str)))
			//			{
			//				file.fileStream.CopyTo(stream);
			//			}
			//			if (this.client.GetReply().Success)
			//			{
			//				continue;
			//			}
			//			throw new ConnectionException("Could not connect to printer over FTP, make sure FTP is enabled");
			//		}
			//	}
			//	catch (ConnectionException connectionException)
			//	{
			//		throw;
			//	}
			//	catch (Exception exception1)
			//	{
			//		Exception exception = exception1;
			//		throw new ConnectionException(exception.Message, exception);
			//	}
			//}
			//finally
			//{
			//	if (this.client.IsConnected)
			//	{
			//		this.client.Disconnect();
			//	}
			//}
		}
	}
}