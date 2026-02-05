using Rebex.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

/*
 * IMPORTANT: Rebex.Net.Ftp library launches exceptions that are serializable but of a data type that is unknown to the application main domain...
 *            this cuases the application to not be able to process the exception correctly. Instead of letting the unknown exception type
 *            to propagate accross domains, we change the exception type to the base "System.Exception".
 * 
 *				That is why you will see the following pattern in each method:
 *				try
 *				{
 *					...
 *				}
 *				catch(Exception ex)
 *				{
 *					throw new Exception(ex.Message);
 *				}
 */

namespace RebexFtpLib.Client
{
	class FTPImplementation : IFtpClient
	{
        protected string server;
		protected int port;
        protected string user;
        protected string password;
        protected Ftp ftp;
		protected FTPMode mode;

		/// <summary>
		/// Event fired when the ftp connection is stablished.
		/// </summary>
		public event EventHandler OnConnect;

		/// <summary>
		/// Event fired when the ftp connection is closed (or lost due to an error).
		/// </summary>
		public event EventHandler OnDisconnect;
		
		/// <summary>
        /// Initializes a new instance of this class with the provided data.
        /// </summary>
        /// <param name="server">The target server name or IP address</param>
        /// <param name="user">User name</param>
        /// <param name="password">Password</param>
        public FTPImplementation(string server, int port, string user, string password)
        {
			try
			{
				this.mode = FTPMode.FTP;
				this.server = server;
				this.port = port;
				this.user = user;
				this.password = password;
				ftp = new Ftp();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
        }


		public void Initialize(int id, string name, string server, int port, string user, string password, FTPMode mode, bool allowInvalidCerts, byte[] keyFile, string keyFilePassword, SFTPKeyAlgorithm keyAlgorithm, string privateKeyPath, string privateKeyPassword)
		{
			// empty on purpose
		}

		/// <summary>
		/// Gets the name of the ftp server as it was setup by the user in the ACRS configuration system.
		/// </summary>
		public string Server { get; set; }

		/// <summary>
		/// Gets the TCP Port as it was setup by the user in the ACRS configuration system.
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Gets the username as setup by the user in the ACRS configuration system.
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Gets the password as it was setup by the user in the ACRS configuration system.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Gets the FTP mode setup by the user in the ACRS configuration system.
		/// </summary>
		public FTPMode Mode { get; set; }

		/// <summary>
		/// Indicates if invalid certificates should be acepted when working on FTPS mode.
		/// </summary>
		public bool AllowInvalidCertificates { get; set; }

		/// <summary>
		/// Gets the filename (might include path information) of the file containing the key required to stablish an SFTP connection. Used only when working on SFTP mode.
		/// </summary>
		public byte[] KeyFile { get; set; }

		/// <summary>
		/// Gets the password used to protect the key file. Can be left empty if the key file is not encrypted. Used only when working on SFTP mode.
		/// </summary>
		public string KeyFilePassword { get; set; }

		/// <summary>
		/// Gets the algorithm used for key exchange.
		/// </summary>
		public SFTPKeyAlgorithm KeyAlgorithm { get; set; }

		/// <summary>
		/// Attempts to connect to the ftp server. This method will throw an exception if the connection cannot be established.
		/// </summary>
		public virtual void Connect()
		{
			try
			{
				ftp.Timeout = 600000;
				ftp.Connect(server, port);
				ftp.Login(user, password);
				ftp.TransferType = FtpTransferType.Binary;
				RaiseOnConnect();
			}
			catch (FtpException ftpe)
			{
				throw new Exception(String.Format("Connection cannot be established. Error Message: {0}", ftpe.Message));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}

		/// <summary>
		/// Attempts to connect to the ftp server. This method will throw an exception if the connection cannot be established.
		/// </summary>
		public virtual async Task ConnectAsync()
		{
			try
			{
				ftp.Timeout = 600000;
				await ftp.ConnectAsync(server, port);
				await ftp.LoginAsync(user, password);
				ftp.TransferType = FtpTransferType.Binary;
				RaiseOnConnect();
			}
			catch (FtpException ftpe)
			{
				throw new Exception(String.Format("Connection cannot be established. Error Message: {0}", ftpe.Message));
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}

		/// <summary>
		/// Attempts to get a list of all files and directories in the server. For simplicity only filenames are retrieved.
		/// </summary>
		/// <returns></returns>
		public List<FtpFileInfo> GetFileList()
        {
			try
			{
				if (ftp.State == FtpState.Disconnected)
					throw new Exception("Connection lost");

				List<FtpFileInfo> FilesList = new List<FtpFileInfo>();
				FtpItemCollection list = ftp.GetList();
				foreach (FtpItem item in list)
				{
					FtpFileInfo _File = new FtpFileInfo();
					_File.FileName = item.Name;
					_File.FileSize = item.Length;
					_File.FileType = (item.IsFile) ? FtpFileType.File : FtpFileType.Directory;
					_File.LastUpdated = item.Modified;
					FilesList.Add(_File);
				}
				return FilesList;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a list of all files and directories in the server. For simplicity only filenames are retrieved.
		/// </summary>
		/// <returns></returns>
		public async Task<List<FtpFileInfo>> GetFileListAsync()
		{
			try
			{
				if (ftp.State == FtpState.Disconnected)
					throw new Exception("Connection lost");

				List<FtpFileInfo> FilesList = new List<FtpFileInfo>();
				FtpItemCollection list = await ftp.GetListAsync();
				foreach (FtpItem item in list)
				{
					FtpFileInfo _File = new FtpFileInfo();
					_File.FileName = item.Name;
					_File.FileSize = item.Length;
					_File.FileType = (item.IsFile) ? FtpFileType.File : FtpFileType.Directory;
					_File.LastUpdated = item.Modified;
					FilesList.Add(_File);
				}
				return FilesList;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to change the current work directory. This method will throw an exception if the connection is lost, or if the directory is invalid.
		/// </summary>
		/// <param name="directory">The name of the new directory</param>
		public void ChangeDirectory(string directory)
        {
			try
			{
				if (ftp.State == FtpState.Disconnected)
					throw new Exception("Connection lost");
				ftp.ChangeDirectory(directory);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to change the current work directory. This method will throw an exception if the connection is lost, or if the directory is invalid.
		/// </summary>
		/// <param name="directory">The name of the new directory</param>
		public async Task ChangeDirectoryAsync(string directory)
		{
			try
			{
				if (ftp.State == FtpState.Disconnected)
					throw new Exception("Connection lost");
				await ftp.ChangeDirectoryAsync(directory);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Creates a new directory under the current directory.
		/// </summary>
		/// <param name="directory">The name of the directory to create</param>
		public void CreateDirectory(string directory)
		{
			try
			{
				if (ftp.State == FtpState.Disconnected)
					throw new Exception("Connection lost");
				ftp.CreateDirectory(directory);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Creates a new directory under the current directory.
		/// </summary>
		/// <param name="directory">The name of the directory to create</param>
		public async Task CreateDirectoryAsync(string directory)
		{
			try
			{
				if (ftp.State == FtpState.Disconnected)
					throw new Exception("Connection lost");
				await ftp.CreateDirectoryAsync(directory);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="destinationFileName">The name of the file to be created.</param>
		/// <param name="fileContent">The content of the file.</param>
		public void SendFile(byte[] fileContent, string destinationFileName)
        {
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				System.IO.MemoryStream ms = new System.IO.MemoryStream(fileContent);
				ftp.PutFile(ms, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
        }


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="destinationFileName">The name of the file to be created.</param>
		/// <param name="fileContent">The content of the file.</param>
		public async Task SendFileAsync(byte[] fileContent, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				System.IO.MemoryStream ms = new System.IO.MemoryStream(fileContent);
				await ftp.PutFileAsync(ms, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceStream">The stream from which to read the data of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public void SendFile(Stream sourceStream, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.PutFile(sourceStream, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceStream">The stream from which to read the data of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public async Task SendFileAsync(Stream sourceStream, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				await ftp.PutFileAsync(sourceStream, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public void SendFile(string sourceFileName, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.PutFile(sourceFileName, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public async Task SendFileAsync(string sourceFileName, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				await ftp.PutFileAsync(sourceFileName, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a file from an ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: File will be retrieved from the current work directory. To change the current work directory, use the ChangeDirectory method.
		/// NOTE: Files are always retrieved using "Image" mode.
		/// </summary>
		/// <param name="filename">The name of the file to retrieve. This should not include any path information, only the file name.</param>
		/// <returns>Returns an array of bytes with the content of the retrieved file.</returns>
		public byte[] GetFile(string filename)
        {
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				System.IO.MemoryStream ms = new System.IO.MemoryStream();
				ftp.TransferType = FtpTransferType.Binary;
				ftp.GetFile(filename, ms);
				ms.Seek(0L, SeekOrigin.Begin);
				byte[] FileContent = new byte[ms.Length];
				ms.Read(FileContent, 0, (int)ms.Length);
				return FileContent;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a file from an ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: File will be retrieved from the current work directory. To change the current work directory, use the ChangeDirectory method.
		/// NOTE: Files are always retrieved using "Image" mode.
		/// </summary>
		/// <param name="filename">The name of the file to retrieve. This should not include any path information, only the file name.</param>
		/// <returns>Returns an array of bytes with the content of the retrieved file.</returns>
		public async Task<byte[]> GetFileAsync(string filename)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				System.IO.MemoryStream ms = new System.IO.MemoryStream();
				ftp.TransferType = FtpTransferType.Binary;
				await ftp.GetFileAsync(filename, ms);
				ms.Seek(0L, SeekOrigin.Begin);
				byte[] FileContent = new byte[ms.Length];
				ms.Read(FileContent, 0, (int)ms.Length);
				return FileContent;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a file from an ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: File will be retrieved from the current work directory. To change the current work directory, use the ChangeDirectory method.
		/// NOTE: Files are always retrieved using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to retrieve from the FTP server. The file is retreived from the current directory; must NOT include any path information.</param>
		/// <param name="destinationStream">The stream in which to write the data of the source file.</param>
		public void GetFile(string sourceFileName, Stream destinationStream)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.TransferType = FtpTransferType.Binary;
				ftp.GetFile(sourceFileName, destinationStream);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a file from an ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: File will be retrieved from the current work directory. To change the current work directory, use the ChangeDirectory method.
		/// NOTE: Files are always retrieved using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to retrieve from the FTP server. The file is retreived from the current directory; must NOT include any path information.</param>
		/// <param name="destinationStream">The stream in which to write the data of the source file.</param>
		public async Task GetFileAsync(string sourceFileName, Stream destinationStream)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.TransferType = FtpTransferType.Binary;
				await ftp.GetFileAsync(sourceFileName, destinationStream);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a file from an ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: File will be retrieved from the current work directory. To change the current work directory, use the ChangeDirectory method.
		/// NOTE: Files are always retrieved using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to retrieve from the FTP server. The file is retreived from the current directory; must NOT include any path information.</param>
		/// <param name="destinationFileName">The name of the file to be created in the local system.</param>
		public void GetFile(string sourceFileName, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.TransferType = FtpTransferType.Binary;
				ftp.GetFile(sourceFileName, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Attempts to get a file from an ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: File will be retrieved from the current work directory. To change the current work directory, use the ChangeDirectory method.
		/// NOTE: Files are always retrieved using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to retrieve from the FTP server. The file is retreived from the current directory; must NOT include any path information.</param>
		/// <param name="destinationFileName">The name of the file to be created in the local system.</param>
		public async Task GetFileAsync(string sourceFileName, string destinationFileName)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.TransferType = FtpTransferType.Binary;
				await ftp.GetFileAsync(sourceFileName, destinationFileName);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Deletes a file from the FTP server.
		/// </summary>
		/// <param name="filePath">The path to the file to be deleted</param>
		public void DeleteFile(string filePath)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.DeleteFile(filePath);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Deletes a file from the FTP server.
		/// </summary>
		/// <param name="filePath">The path to the file to be deleted</param>
		public async Task DeleteFileAsync(string filePath)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				await ftp.DeleteFileAsync(filePath);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Renames a file or directory.
		/// </summary>
		/// <param name="fromPath">The name of the file or firectory to rename.</param>
		/// <param name="toPath">The name the file or directory is going to be renamed to.</param>
		public void Rename(string fromPath, string toPath)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				ftp.Rename(fromPath, toPath);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Renames a file or directory.
		/// </summary>
		/// <param name="fromPath">The name of the file or firectory to rename.</param>
		/// <param name="toPath">The name the file or directory is going to be renamed to.</param>
		public async Task RenameAsync(string fromPath, string toPath)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				await ftp.RenameAsync(fromPath, toPath);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Gets a value indicating if the specified directory exists.
		/// </summary>
		/// <param name="path">The name of the directory to check.</param>
		public bool DirectoryExists(string path)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				return ftp.DirectoryExists(path);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Gets a value indicating if the specified directory exists.
		/// </summary>
		/// <param name="path">The name of the directory to check.</param>
		public async Task<bool> DirectoryExistsAsync(string path)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				return await ftp.DirectoryExistsAsync(path);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}

		/// <summary>
		/// Gets a value indicating if the specified file exists.
		/// </summary>
		/// <param name="path">The name of the file to check.</param>
		public bool FileExists(string path)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				return ftp.FileExists(path);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		/// <summary>
		/// Gets a value indicating if the specified file exists.
		/// </summary>
		/// <param name="path">The name of the file to check.</param>
		public async Task<bool> FileExistsAsync(string path)
		{
			if (ftp.State == FtpState.Disconnected)
				throw new Exception("Connection lost");
			try
			{
				return await ftp.FileExistsAsync(path);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}

		/// <summary>
		/// Closes the connection with the ftp server.
		/// </summary>
		public void Disconnect()
        {
			try
			{
				ftp.Disconnect();
				if (OnDisconnect != null)
					OnDisconnect(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
        }

        /// <summary>
        /// Releases any resources being held by this object.
        /// </summary>
        public void Dispose()
        {
            ftp.Dispose();
        }


		/// <summary>
		/// Allows to create a copy of this object that uses the same configuration as the original.
		/// </summary>
		public IFtpClient Clone()
		{
			return new FTPImplementation(server, port, user, password);
		}


		protected void RaiseOnConnect()
		{
			if (OnConnect != null)
				OnConnect(this, EventArgs.Empty);
		}
	}
}
