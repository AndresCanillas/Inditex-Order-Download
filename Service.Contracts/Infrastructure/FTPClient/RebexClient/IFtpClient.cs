using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
namespace RebexFtpLib.Client
{
    public interface IFtpClient : IDisposable
    {

        /// <summary>
		/// Event fired when the ftp connection is stablished.
		/// </summary>
		event EventHandler OnConnect;

        /// <summary>
        /// Event fired when the ftp connection is closed (or lost due to an error).
        /// </summary>
        event EventHandler OnDisconnect;

		void Initialize(int id, string name, string server, int port, string user, string password, FTPMode mode, bool allowInvalidCerts, byte[] keyFile, string keyFilePassword, SFTPKeyAlgorithm keyAlgorithm, string privateKeyPath, string privateKeyPassword);
		void Disconnect();
        void Connect();
		List<FtpFileInfo> GetFileList();
		void ChangeDirectory(string directory);
		void CreateDirectory(string directory);
		void SendFile(byte[] fileContent, string destinationFileName);
		void SendFile(Stream sourceStream, string destinationFileName);
		void SendFile(string sourceFileName, string destinationFileName);
		byte[] GetFile(string filename);
		void GetFile(string sourceFileName, Stream destinationStream);
		void GetFile(string sourceFileName, string destinationFileName);
		void DeleteFile(string filePath);
		void Rename(string fromPath, string toPath);
		bool DirectoryExists(string path);
		bool FileExists(string path);
		Task ConnectAsync();
		Task<List<FtpFileInfo>> GetFileListAsync();
		Task ChangeDirectoryAsync(string directory);
		Task CreateDirectoryAsync(string directory);
		Task SendFileAsync(byte[] fileContent, string destinationFileName);
		Task SendFileAsync(Stream sourceStream, string destinationFileName);
		Task SendFileAsync(string sourceFileName, string destinationFileName);
		Task<byte[]> GetFileAsync(string filename);
		Task GetFileAsync(string sourceFileName, Stream destinationStream);
		Task GetFileAsync(string sourceFileName, string destinationFileName);
		Task DeleteFileAsync(string filePath);
		Task RenameAsync(string fromPath, string toPath);
		Task<bool> DirectoryExistsAsync(string path);
		Task<bool> FileExistsAsync(string path);
	}
}