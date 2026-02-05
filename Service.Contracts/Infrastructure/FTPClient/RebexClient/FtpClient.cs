using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RebexFtpLib.Client
{
	/// <summary>
    /// Class used to execute FTP operations
    /// </summary>
    public class FtpClient : MarshalByRefObject, IFtpClient
    {
		private int id;
		private string name;
        private string server;
		private int port;
        private string user;
        private string password;
		private FTPMode mode;


		private bool allowInvalidCerts;
		private byte[] keyFile;
		private string keyFilePassword;
		private SFTPKeyAlgorithm keyAlgorithm;
		private IFtpClient client;
        private string privateKeyPath;
        private string privateKeyPassword; 


		/// <summary>
		/// Event fired when the ftp connection is stablished.
		/// </summary>
		public event EventHandler OnConnect;

		/// <summary>
		/// Event fired when the ftp connection is closed (or lost due to an error).
		/// </summary>
		public event EventHandler OnDisconnect;

		public FtpClient() {
			Rebex.Licensing.Key = "==FiKu+kNzoCW8yrl/irJYT/LiUAZSSCbe4uCMP04UcuVuuuVgIaFqtv4irOhfT6AlXM4rt==";
		}

        /// <summary>
        /// Initializes a new instance of this class with the provided data.
        /// </summary>
        /// <param name="server">The target server name or IP address</param>
        /// <param name="user">User name</param>
        /// <param name="password">Password</param>
        /// <param name="useFTPS">Indicates if connection should be secured.</param>
		/// <param name="allowInvalidCerts"></param>
		/// <param name="keyFile">Allows to provide a path to a keyfile (And automatically changes the component to work in SFTP mode). If left null, no key file will be used an we will be working on regular FTP/FTPS mode.</param>
        public void Initialize(int id, string name, string server, int port, string user, string password, FTPMode mode, bool allowInvalidCerts, byte[] keyFile, string keyFilePassword, SFTPKeyAlgorithm keyAlgorithm, string privateKeyPath = null, string privateKeyPassword= null)
        {
			try
			{
				this.id = id;
				this.name = name;
				this.server = server;
				this.port = port;
				this.user = user;
				this.password = password;
				this.mode = mode;
				this.allowInvalidCerts = allowInvalidCerts;
				this.keyFile = keyFile;
				this.keyFilePassword = keyFilePassword;
				this.keyAlgorithm = keyAlgorithm;
                this.privateKeyPath = privateKeyPath; 
                this.privateKeyPassword = privateKeyPassword;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		private void CreateFTPInstance()
		{
			if (client != null)
				throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
			try
			{
				switch (mode)
				{
					case FTPMode.FTP:
						client = new FTPImplementation(server, port, user, password);
						break;
					case FTPMode.FTPS:
						client = new FTPSImplementation(server, port, user, password, allowInvalidCerts);
						break;
					case FTPMode.SFTP:
						client = new SFTPImplementation(server, port, user, password, keyFile, keyFilePassword, keyAlgorithm, privateKeyPath, privateKeyPassword);
						break;
					default:
						throw new InvalidOperationException("Unsupported FTP Mode: " + mode.ToString());
				}
				client.OnConnect += new EventHandler(client_OnConnect);
				client.OnDisconnect += new EventHandler(client_OnDisconnect);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message); // NOTE: Do not remove: See important note at the top of this file.
			}
		}


		private void DisposeFTPInstance()
		{
			if (client != null)
			{
				client.Disconnect();
				client.OnConnect -= new EventHandler(client_OnConnect);
				client.OnDisconnect -= new EventHandler(client_OnDisconnect);
				client.Dispose();
				client = null;
			}
		}


		private void client_OnConnect(object sender, EventArgs e)
		{
			if (OnConnect != null)
				OnConnect(this, e);
		}


		private void client_OnDisconnect(object sender, EventArgs e)
		{
			if (OnDisconnect != null)
				OnDisconnect(this, e);
		}

        /// <summary>
        /// Prevents the proxy from being collected.
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

		public int ID { get { return id; } }

		public string Name { get { return name; } }

		/// <summary>
		/// Gets the name of the ftp server as it was setup by the user in the ACRS configuration system.
		/// </summary>
		public string Server
		{
			get { return server; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				server = value;
			}
		}

		/// <summary>
		/// Gets the TCP Port as it was setup by the user in the ACRS configuration system.
		/// </summary>
		public int Port
		{
			get { return port; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				port = value;
			}
		}

		/// <summary>
		/// Gets the username as setup by the user in the ACRS configuration system.
		/// </summary>
		public string UserName
		{
			get { return user; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				user = value;
			}
		}

		/// <summary>
		/// Gets the password as it was setup by the user in the ACRS configuration system.
		/// </summary>
		public string Password
		{
			get { return password; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				password = value;
			}
		}

		/// <summary>
		/// Gets the FTP mode setup by the user in the ACRS configuration system.
		/// </summary>
		public FTPMode Mode
		{
			get { return mode; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				mode = value;
			}
		}

		/// <summary>
		/// Indicates if invalid certificates should be acepted when working on FTPS mode.
		/// </summary>
		public bool AllowInvalidCertificates
		{
			get { return allowInvalidCerts; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				allowInvalidCerts = value;
			}
		}

		/// <summary>
		/// Gets the filename (might include path information) of the file containing the key required to stablish an SFTP connection. Used only when working on SFTP mode.
		/// </summary>
		public byte[] KeyFile
		{
			get { return keyFile; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				keyFile = value;
			}
		}

		/// <summary>
		/// Gets the password used to protect the key file. Can be left empty if the key file is not encrypted. Used only when working on SFTP mode.
		/// </summary>
		public string KeyFilePassword
		{
			get { return keyFilePassword; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				keyFilePassword = value;
			}
		}

		/// <summary>
		/// Gets the algorithm used for key exchange.
		/// </summary>
		public SFTPKeyAlgorithm KeyAlgorithm
		{
			get { return keyAlgorithm; }
			set
			{
				if (client != null)
					throw new InvalidOperationException("Current FTP instance is connected, call Disconnect before trying to make changes to the FTP object.");
				keyAlgorithm = value;
			}
		}

		/// <summary>
		/// Attempts to connect to the ftp server. This method will throw an exception if the connection cannot be established.
		/// </summary>
		public void Connect()
		{
			if (client == null)
				CreateFTPInstance();
			client.Connect();
		}


		/// <summary>
		/// Attempts to connect to the ftp server. This method will throw an exception if the connection cannot be established.
		/// </summary>
		public async Task ConnectAsync()
		{
			if (client == null)
				CreateFTPInstance();
			await client.ConnectAsync();
		}


		/// <summary>
		/// Attempts to get a list of all files and directories in the server. For simplicity only filenames are retrieved.
		/// </summary>
		/// <returns></returns>
		public List<FtpFileInfo> GetFileList()
        {
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return client.GetFileList();
		}


		/// <summary>
		/// Attempts to get a list of all files and directories in the server. For simplicity only filenames are retrieved.
		/// </summary>
		/// <returns></returns>
		public async Task<List<FtpFileInfo>> GetFileListAsync()
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return await client.GetFileListAsync();
		}


		/// <summary>
		/// Attempts to change the current work directory. This method will throw an exception if the connection is lost, or if the directory is invalid.
		/// </summary>
		/// <param name="directory">The name of the new directory</param>
		public void ChangeDirectory(string directory)
        {
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.ChangeDirectory(directory);
		}


		/// <summary>
		/// Attempts to change the current work directory. This method will throw an exception if the connection is lost, or if the directory is invalid.
		/// </summary>
		/// <param name="directory">The name of the new directory</param>
		public async Task ChangeDirectoryAsync(string directory)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.ChangeDirectoryAsync(directory);
		}


		/// <summary>
		/// Creates a new directory under the current directory.
		/// </summary>
		/// <param name="directory">The name of the directory to create</param>
		public void CreateDirectory(string directory)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.CreateDirectory(directory);
		}


		/// <summary>
		/// Creates a new directory under the current directory.
		/// </summary>
		/// <param name="directory">The name of the directory to create</param>
		public async Task CreateDirectoryAsync(string directory)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.CreateDirectoryAsync(directory);
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="destinationFileName">The name of the file to be created.</param>
		/// <param name="fileContent">The content of the file.</param>
		public void SendFile(byte[] fileContent, string destinationFileName)
        {
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.SendFile(fileContent, destinationFileName);
        }


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="destinationFileName">The name of the file to be created.</param>
		/// <param name="fileContent">The content of the file.</param>
		public async Task SendFileAsync(byte[] fileContent, string destinationFileName)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.SendFileAsync(fileContent, destinationFileName);
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceStream">The stream from which to read the data of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public void SendFile(Stream sourceStream, string destinationFileName)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.SendFile(sourceStream, destinationFileName);
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceStream">The stream from which to read the data of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public async Task SendFileAsync(Stream sourceStream, string destinationFileName)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.SendFileAsync(sourceStream, destinationFileName);
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public void SendFile(string sourceFileName, string destinationFileName)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.SendFile(sourceFileName, destinationFileName);
		}


		/// <summary>
		/// Attempts to send a file to the ftp server. This method will throw an exception if the connection is lost, or if the server reports any other error.
		/// NOTE: Files are always sent using "Image" mode.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to be sent.</param>
		/// <param name="destinationFileName">The name of the file to be created on the ftp server in the current directory (MUST not include path information).</param>
		public async Task SendFileAsync(string sourceFileName, string destinationFileName)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.SendFileAsync(sourceFileName, destinationFileName);
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
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return client.GetFile(filename);
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
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return await client.GetFileAsync(filename);
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
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.GetFile(sourceFileName, destinationStream);
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
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.GetFileAsync(sourceFileName, destinationStream);
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
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.GetFile(sourceFileName, destinationFileName);
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
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.GetFileAsync(sourceFileName, destinationFileName);
		}

		/// <summary>
		/// Deletes a file from the FTP server.
		/// </summary>
		/// <param name="filePath">The path to the file to be deleted</param>
		public void DeleteFile(string filePath)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.DeleteFile(filePath);
		}

		
		/// <summary>
		/// Deletes a file from the FTP server.
		/// </summary>
		/// <param name="filePath">The path to the file to be deleted</param>
		public async Task DeleteFileAsync(string filePath)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.DeleteFileAsync(filePath);
		}


		/// <summary>
		/// Renames a file or directory.
		/// </summary>
		/// <param name="fromPath">The name of the file or firectory to rename.</param>
		/// <param name="toPath">The name the file or directory is going to be renamed to.</param>
		public void Rename(string fromPath, string toPath)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			client.Rename(fromPath, toPath);
		}


		/// <summary>
		/// Renames a file or directory.
		/// </summary>
		/// <param name="fromPath">The name of the file or firectory to rename.</param>
		/// <param name="toPath">The name the file or directory is going to be renamed to.</param>
		public async Task RenameAsync(string fromPath, string toPath)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			await client.RenameAsync(fromPath, toPath);
		}


		/// <summary>
		/// Gets a value indicating if the specified directory exists.
		/// </summary>
		/// <param name="path">The name of the directory to check.</param>
		public bool DirectoryExists(string path)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return client.DirectoryExists(path);
		}


		/// <summary>
		/// Gets a value indicating if the specified directory exists.
		/// </summary>
		/// <param name="path">The name of the directory to check.</param>
		public async Task<bool> DirectoryExistsAsync(string path)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return await client.DirectoryExistsAsync(path);
		}


		/// <summary>
		/// Gets a value indicating if the specified file exists.
		/// </summary>
		/// <param name="path">The name of the file to check.</param>
		public bool FileExists(string path)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return client.FileExists(path);
		}


		/// <summary>
		/// Gets a value indicating if the specified file exists.
		/// </summary>
		/// <param name="path">The name of the file to check.</param>
		public async Task<bool> FileExistsAsync(string path)
		{
			if (client == null)
				throw new InvalidOperationException("FTP Instance is not initialized, you have to call Connect() before making calls to any other methods.");
			return await client.FileExistsAsync(path);
		}


		/// <summary>
		/// Closes the connection with the ftp server.
		/// </summary>
		public void Disconnect()
        {
			DisposeFTPInstance();
		}

		/// <summary>
		/// Releases any resources being held by this object.
		/// </summary>
		public void Dispose()
        {
			DisposeFTPInstance();
		}

		/// <summary>
		/// Allows to create a copy of this object that uses the same configuration as the original.
		/// </summary>
		//public IFtpClient Clone()
		//{
		//	return ACRSFactory.GetFtpClient(id, name, server, port, user, password, (int)mode, allowInvalidCerts, keyFile, keyFilePassword, (int)keyAlgorithm) as IFtpClient;
		//}

		//public void SerializeInstance(SerializationBuffer buffer)
		//{
		//	buffer.AddInt32(id);
		//}

		//public object DeserializeInstance(SerializationBuffer buffer)
		//{
		//	int id = buffer.GetInt32();
		//	var factory = Services.ConfigBox.ObjectFactories.GetFactory(ACRS.API.ObjReferenceType.FtpClient);
		//	return factory.GetObjectInstance(id);
		//////}		//public IFtpClient Clone()
		//////{
		//////	return ACRSFactory.GetFtpClient(id, name, server, port, user, password, (int)mode, allowInvalidCerts, keyFile, keyFilePassword, (int)keyAlgorithm) as IFtpClient;
		//////}

		//////public void SerializeInstance(SerializationBuffer buffer)
		//////{
		//////	buffer.AddInt32(id);
		//////}

		//////public object DeserializeInstance(SerializationBuffer buffer)
		//////{
		//////	int id = buffer.GetInt32();
		//////	var factory = Services.ConfigBox.ObjectFactories.GetFactory(ACRS.API.ObjReferenceType.FtpClient);
		//////	return factory.GetObjectInstance(id);
		//////}
	}
}
