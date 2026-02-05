using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public interface IFileStore
	{
		bool TryGetFile(int id, out IFileData file);
		IFileData CreateFile(int id, string filename);
		IFileData GetOrCreateFile(int id, string filename);
		void DeleteFile(int id);
		IEnumerable<string> Categories { get; }
	}


	public interface ILocalFileStore : IFileStore
	{
		void Configure(string configKey);
		void Configure(string workDir, string categories, int containerLevels);
		string BaseDirectory { get; }
	}


	public interface IFSFile
	{
		Guid FileGUID { get; }
		string FileName { get; set; }
		long FileSize { get; }
		bool HasPhysicalCopy { get; }
		string PhysicalPath { get; }
		DateTime CreatedDate { get; }
		DateTime UpdatedDate { get; }
		T GetMetadata<T>() where T : new();
		void SetMetadata<T>(T meta) where T : new();
		byte[] GetMetadataBytes();
		void SetMetadataBytes(byte[] metadata);
		Stream GetContentAsStream();
		byte[] GetContentAsBytes();
		void SetContent(string sourceFile);
		void SetContent(Stream stream);
		void SetContent(byte[] fileContent);
		void Copy(IFSFile source);
		void Delete();
	}


	public interface IFileData : IFSFile
	{
		int FileID { get; }
		IEnumerable<string> AttachmentCategories { get; }
		IAttachmentCollection GetAttachmentCategory(string category);
	}


	public interface IAttachmentCollection : IEnumerable<IAttachmentData>
	{
		string CategoryName { get; }
		bool HasPhysicalCopy { get; }
		string PhysicalPath { get; }
		int Count { get; }
		bool TryGetAttachment(string filename, out IAttachmentData attachment);
		IAttachmentData CreateAttachment(string filename);
		IAttachmentData GetOrCreateAttachment(string fileName);
		void DeleteAttachment(IAttachmentData attachment);
		void Clear();
	}

	
	public interface IAttachmentData : IFSFile
	{
	}
}
