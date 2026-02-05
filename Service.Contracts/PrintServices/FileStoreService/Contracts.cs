using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IFileStoreConfigService
	{
		Task<List<FSStoreInfo>> GetAllAsync();
		Task<FSStoreInfo> GetStoreConfigAsync(string storeName);
	}

	public interface IFileStoreServer
    {
        Task<List<FSCategoryInfo>> GetCategoriesAsync();
        Task<FSFileCategoryInfo> GetFileCategoryAsync(int fileid, int categoryid);
		Task<List<byte[]>> GetFileCategoryMetadataAsync(int fileid, int categoryid);

		Task<FSFileInfo> GetFileAsync(int fileid);
		Task<FSFileInfo> GetOrCreateFileAsync(int fileid, string filename);
		Task<FSFileInfo> CreateFileAsync(int fileid, string filename);
		Task<FSFileInfo> CreateFileAsync(string filename);
		Task RenameFileAsync(int fileid, string filename);
		Task<Stream> DownloadFileContentAsync(int fileid);
		Task UploadFileContentAsync(int fileid, Stream content);
		Task SetFileContentAsync(int fileid, Guid fileguid);
		Task<Stream> DownloadFileMetadataAsync(int fileid);
		Task UploadFileMetadataAsync(int fileid, Stream metadata);
		Task DeleteFileAsync(int fileid);
		Task<FSAttachmentInfo> GetFileAttachmentAsync(int fileid, string category, string filename);


		Task<FSAttachmentInfo> GetAttachmentAsync(FSAttachmentInfo info);
		Task<FSAttachmentInfo> CreateAttachmentAsync(FSAttachmentInfo info);
		Task<FSAttachmentInfo> GetOrCreateAttachmentAsync(FSAttachmentInfo info);
		Task RenameAttachmentAsync(FSAttachmentInfo info, string newfilename);
		Task<Stream> DownloadAttachmentContentAsync(FSAttachmentInfo info);
		Task UploadAttachmentContentAsync(FSAttachmentInfo info, Stream content);
		Task SetAttachmentContentAsync(FSAttachmentInfo info, Guid fileguid);
		Task<Stream> DownloadAttachmentMetadataAsync(FSAttachmentInfo info);
		Task UploadAttachmentMetadataAsync(FSAttachmentInfo info, Stream metadata);
		Task DeleteAttachmentAsync(FSAttachmentInfo info);
		Task ClearFileAttachmentCategoryAsync(int fileid, int categoryid);
	}


	public class FSStoreInfo
	{
		public int StoreID;
		public string StoreName;
		public string Host;
		public int Port;
		public string Categories;
		public int ContainerLevels;
		public bool HasPhysicalCopy;
		public string PhysicalPath;
		public bool IsTemporalFileStore;
		public int RetentionTimeInHours;
		public bool AutomaticFileIDGeneration;
		public string Database;
		public string InitialCatalog;
		public FSMigrationConfig Migration;
	}


	public class FSMigrationConfig
	{
		public bool Enabled;
		public string PhysicalPath;
		public int ContainerLevels;
	}


	public class FSCategoryInfo
    {
        public int CategoryID;
        public string CategoryName;
    }


    public class FSFileInfo
    {
		public int StoreID;
        public int FileID;
        public string FileName;
        public int FileSize;
		public bool HasPhysicalCopy;
		public string PhysicalPath;
        public DateTime CreateDate;
        public DateTime UpdateDate;
    }


    public class FSFileCategoryInfo
    {
        public int FileID;
        public int CategoryID;
        public string CategoryName;
        public List<FSAttachmentInfo> Attachments;
    }


    public class FSAttachmentInfo
    {
		public int StoreID;
		public int FileID;
        public int CategoryID;
        public int AttachmentID;
        public string FileName;
        public int FileSize;
		public bool HasPhysicalCopy;
		public string PhysicalPath;
        public DateTime CreateDate;
        public DateTime UpdateDate;
    }


	public class FSFileReference
	{
		public int StoreID { get; private set; }
		public FSFileReferenceType Type { get; private set; }
		public int FileID { get; private set; }
		public int CategoryID { get; private set; }
		public int AttachmentID { get; private set; }

		public static FSFileReference FromGuid(Guid fileguid)
		{
			var reference = new FSFileReference();
			var sb = new SerializationBuffer(null, fileguid.ToByteArray(), 0, 16);
			var version = sb.PeekByte(0);
			if (version != 1)
				throw new NotImplementedException($"FileGuid version {version} is not implemented");

			reference.StoreID = sb.PeekByte(1);

			var rtype = sb.PeekByte(2);
			if (rtype == 1)
				reference.Type = FSFileReferenceType.File;
			else if(rtype == 2)
				reference.Type = FSFileReferenceType.Attachment;
			else
				throw new NotImplementedException($"FileGuid reference type {rtype} is not implemented");

			reference.CategoryID = sb.PeekByte(3);
			reference.FileID = sb.PeekInt32(4);
			reference.AttachmentID = sb.PeekInt32(8);
			return reference;
		}

		public override string ToString()
		{
			return $" StoreID [{StoreID}] -  CategoryID [{CategoryID}]  -  FileID [{FileID}] -  AttachmentID  [{AttachmentID}] - Type [{Type}]";
		}
	}


	public enum FSFileReferenceType
	{
		File,
		Attachment
	}


	/* =============================================================================
	 * FileGUID identifies a file across the entire system. Passing a FileGUID from
	 * one service to another is the easiest way of "sharing" a file between services,
	 * the receiving end simply needs to use the FileStoreManager to rehydrate the
	 * referenced file without having to transfer the real file between services.
	 * 
	 * Like all Guids the FileGUID is a 128bit (16 bytes) value, however unlike pure
	 * Guids that incorporate random elements to ensure uniqueness worldwide, a FileGUID
	 * is unique only within the context of a given system, as it is composed of the
	 * following elements:
	 *					
	 * FileGuidVersion	Byte 0		Set to 1 for version 1, future versions might change
	 *								how the GUID bytes are interpreted.
	 * 
	 * FileStoreID		Byte 1		The ID of the file store where the file can be found,
	 *								There can only be 255 stores configured per system.
	 *								
	 * FileType			Byte 2		1 = File, 2 = Attachment
	 * 
	 * CategoryID		Byte 3		The id of the category (maximum 255 categories per store),
	 *								only used if FileType is 2 (Attachment). Set to 0 when
	 *								FileType is 1.
	 *								
	 * 
	 * FileID			Bytes 4-7	The id of the file
	 * 
	 * AttachmentID		Bytes 8-11	The id of the attachment, only used if FileType is 2
	 *								(Attachment)
	 *								
	 * Unused			Bytes 12-15 For this version of FileGUIDs, these last 4 bytes are
	 *								set to 0
	 * =============================================================================*/
	public static class FileGuidExtensions
	{
		public static Guid GetFileGUID(this FSFileInfo fi)
		{
			SerializationBuffer sb = new SerializationBuffer(null, 16);
			sb.SetByte(0, 1);					// FileGUID version
			sb.SetByte(1, (byte)fi.StoreID);    // StoreID
			sb.SetByte(2, 1);                   // FileType = File
			sb.SetByte(3, 0);                   // CategoryID = 0
			sb.SetInt32(4, fi.FileID);          // FileID
			sb.SetInt32(8, 0);                  // AttachmentID
			sb.SetInt32(12, 0);                 // Unused
			return new Guid(sb.buffer);
		}

		public static Guid GetFileGUID(this FSAttachmentInfo ai)
		{
			SerializationBuffer sb = new SerializationBuffer(null, 16);
			sb.SetByte(0, 1);                   // FileGUID version
			sb.SetByte(1, (byte)ai.StoreID);    // StoreID
			sb.SetByte(2, 2);                   // FileType = File
			sb.SetByte(3, (byte)ai.CategoryID); // CategoryID = 0
			sb.SetInt32(4, ai.FileID);          // FileID
			sb.SetInt32(8, ai.AttachmentID);    // AttachmentID
			sb.SetInt32(12, 0);                 // Unused
			return new Guid(sb.buffer);
		}
	}


	public interface IFileStoreManager
	{
		Task<List<FSStoreInfo>> GetAllStoresAsync();
		Task<FSStoreInfo> GetStoreConfigAsync(string storename);
		Task<FSStoreInfo> GetStoreConfigAsync(int storeid);

		IRemoteFileStore OpenStore(string storeName);
		IRemoteFileStore OpenStore(int storeid);

		IFSFile GetFile(Guid fileguid);
		Task<IFSFile> GetFileAsync(Guid fileguid);
		void DeleteFile(Guid fileguid);
		Task DeleteFileAsync(Guid fileguid);
		void CopyFile(Guid sourceFile, Guid targetFile);
		Task CopyFileAsync(Guid sourceFile, Guid targetFile);
	}


	public interface IRemoteFileStore : IFileStore, IDisposable
	{
		int StoreID { get; }
		string StoreName { get; }
		void Configure(int storeid);
		void Configure(string storename);
		bool Disposed { get; }
		FSCategoryInfo GetCategoryByID(int categoryid);
		Task<IRemoteFile> TryGetFileAsync(int fileid);
		Task<IRemoteFile> TryGetFileAsync(Guid fileguid);
		Task<IRemoteFile> GetOrCreateFileAsync(int fileid, string filename);
		Task<IRemoteFile> CreateFileAsync(int fileid, string filename);
		IRemoteFile CreateFile(string filename);
		Task<IRemoteFile> CreateFileAsync(string filename);
		Task DeleteFileAsync(int fileid);
		Task<IRemoteAttachment> GetFileAttachmentAsync(int fileid, string category, string filename);
	}


	public interface IRemoteFile : IFileData
	{
		int StoreID{ get; }
		Task<IRemoteAttachmentCollection> GetAttachmentCategoryAsync(string category);
		Task RenameAsync(string filename);
		Task<T> GetMetadataAsync<T>() where T : new();
		Task SetMetadataAsync<T>(T meta) where T : new();
		Task<byte[]> GetMetadataBytesAsync();
		Task SetMetadataBytesAsync(byte[] metadata);
		Task<Stream> GetContentAsStreamAsync();
		Task<byte[]> GetContentAsBytesAsync();
		Task SetContentAsync(string sourceFile);
		Task SetContentAsync(Stream stream);
		Task SetContentAsync(byte[] fileContent);
		Task SetContentAsync(Guid fileguid);
		Task CopyAsync(IRemoteFile source);
		Task DeleteAsync();
	}


	public interface IRemoteAttachmentCollection : IAttachmentCollection
	{
		Task<IRemoteAttachment> TryGetAttachmentAsync(string filename);
		Task<IRemoteAttachment> TryGetAttachmentAsync(int attachmentid);
		Task<IRemoteAttachment> CreateAttachmentAsync(string filename);
		Task<IRemoteAttachment> GetOrCreateAttachmentAsync(string fileName);
		List<T> GetAllAttachmentMetadata<T>() where T : class, new();
		Task<List<T>> GetAllAttachmentMetadataAsync<T>() where T : class, new();
		Task DeleteAttachmentAsync(IRemoteAttachment attachment);
		Task ClearAsync();
	}

	public interface IRemoteAttachment : IAttachmentData
	{
		int FileID { get; }
		int CategoryID { get; }
		int AttachmentID { get; }
		Task RenameAsync(string filename);
		Task<T> GetMetadataAsync<T>() where T : new();
		Task SetMetadataAsync<T>(T meta) where T : new();
		Task<byte[]> GetMetadataBytesAsync();
		Task SetMetadataBytesAsync(byte[] metadata);
		Task<Stream> GetContentAsStreamAsync();
		Task<byte[]> GetContentAsBytesAsync();
		Task SetContentAsync(string sourceFile);
		Task SetContentAsync(Stream stream);
		Task SetContentAsync(byte[] fileContent);
		Task SetContentAsync(Guid fileguid);
		Task DeleteAsync();
	}
}
