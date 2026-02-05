using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	class RemoteFile : IRemoteFile
	{
		private RemoteFileStore owner;
		private IFileStoreServer server;
		private FSFileInfo fi;
		private List<UserMeta> userMeta;

		public RemoteFile(RemoteFileStore owner, FSFileInfo fi)
		{
			this.owner = owner;
			this.server = owner.server;
			this.fi = fi;
		}


		public Guid FileGUID { get => fi.GetFileGUID(); }

		public int StoreID { get => fi.StoreID; }

		public int FileID { get => fi.FileID; }

		public long FileSize { get => fi.FileSize; }

		public bool HasPhysicalCopy { get => fi.HasPhysicalCopy; }

		public string PhysicalPath { get => fi.PhysicalPath; }

		public DateTime CreatedDate { get => fi.CreateDate; }

		public DateTime UpdatedDate { get => fi.UpdateDate; }

		public string FileName
		{
			get => fi.FileName;
			set
			{
				if (value != fi.FileName)
				{
					FileStoreHelper.ValidateFileName(value);
					server.RenameFileAsync(fi.FileID, value).Wait();
					fi.FileName = value;
				}
			}
		}


		public async Task RenameAsync(string filename)
		{
			if (filename != fi.FileName)
			{
				FileStoreHelper.ValidateFileName(filename);
				await server.RenameFileAsync(fi.FileID, filename);
				fi.FileName = filename;
			}
		}

		public T GetMetadata<T>() where T : new()
		{
			if (userMeta == null)
				LoadMetadataAsync().Wait();

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				return JsonConvert.DeserializeObject<T>(typeMeta.Content);
			else
				return new T();
		}


		public async Task<T> GetMetadataAsync<T>() where T : new()
		{
			if (userMeta == null)
				await LoadMetadataAsync();

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				return JsonConvert.DeserializeObject<T>(typeMeta.Content);
			else
				return default(T);
		}


		public void SetMetadata<T>(T meta) where T : new()
		{
			if (userMeta == null)
				LoadMetadataAsync().Wait();

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				typeMeta.Content = JsonConvert.SerializeObject(meta);
			else
				userMeta.Add(new UserMeta() { MetaType = typeof(T).Name, Content = JsonConvert.SerializeObject(meta) });

			UpdateMetadataAsync().Wait();
		}


		public async Task SetMetadataAsync<T>(T meta) where T : new()
		{
			if (userMeta == null)
				await LoadMetadataAsync();

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				typeMeta.Content = JsonConvert.SerializeObject(meta);
			else
				userMeta.Add(new UserMeta() { MetaType = typeof(T).Name, Content = JsonConvert.SerializeObject(meta) });

			await UpdateMetadataAsync();
		}


		private async Task LoadMetadataAsync()
		{
			using (var stream = await server.DownloadFileMetadataAsync(fi.FileID))
			{
				var metaBytes = stream.LoadToMemoryAsync().Result;
				var json = Encoding.UTF8.GetString(metaBytes).Trim();
				if (json != null && json.Length > 0)
				{
					if (json[0] == 65279)
						json = json.Substring(1);
					userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
				}
				else userMeta = new List<UserMeta>();
			}
		}


		private async Task UpdateMetadataAsync()
		{
			var metaBytes = GetMetadataBytes();
			using (MemoryStream ms = new MemoryStream(metaBytes))
			{
				await server.UploadFileMetadataAsync(fi.FileID, ms);
			}
		}


		public byte[] GetMetadataBytes()
		{
			if (userMeta == null)
				LoadMetadataAsync().Wait();

			var json = JsonConvert.SerializeObject(userMeta);
			var metaBytes = Encoding.UTF8.GetBytes(json);
			return metaBytes;
		}


		public async Task<byte[]> GetMetadataBytesAsync()
		{
			if (userMeta == null)
				await LoadMetadataAsync();

			var json = JsonConvert.SerializeObject(userMeta);
			var metaBytes = Encoding.UTF8.GetBytes(json);
			return metaBytes;
		}


		public void SetMetadataBytes(byte[] metadata)
		{
			using (MemoryStream ms = new MemoryStream(metadata))
			{
				server.UploadFileMetadataAsync(fi.FileID, ms).Wait();
			}
			userMeta = null;
		}


		public async Task SetMetadataBytesAsync(byte[] metadata)
		{
			using (MemoryStream ms = new MemoryStream(metadata))
			{
				await server.UploadFileMetadataAsync(fi.FileID, ms);
			}
			userMeta = null;
		}


		public Stream GetContentAsStream()
		{
			return server.DownloadFileContentAsync(fi.FileID).Result;
		}


		public async Task<Stream> GetContentAsStreamAsync()
		{
			return await server.DownloadFileContentAsync(fi.FileID);
		}


		public byte[] GetContentAsBytes()
		{
			using (var stream = server.DownloadFileContentAsync(fi.FileID).Result)
			{
				return stream.LoadToMemoryAsync().Result;
			}
		}


		public async Task<byte[]> GetContentAsBytesAsync()
		{
			using (var stream = await server.DownloadFileContentAsync(fi.FileID))
			{
				return await stream.LoadToMemoryAsync();
			}
		}


		public void SetContent(string sourceFile)
		{
			using(var fs = File.OpenRead(sourceFile))
			{
				server.UploadFileContentAsync(fi.FileID, fs).Wait();
			}
		}


		public async Task SetContentAsync(string sourceFile)
		{
			using (var fs = File.OpenRead(sourceFile))
			{
				await server.UploadFileContentAsync(fi.FileID, fs);
			}
		}


		public void SetContent(Stream stream)
		{
			server.UploadFileContentAsync(fi.FileID, stream).Wait();
		}


		public async Task SetContentAsync(Stream stream)
		{
			await server.UploadFileContentAsync(fi.FileID, stream);
		}


		public void SetContent(byte[] fileContent)
		{
			using (var ms = new MemoryStream(fileContent))
			{
				server.UploadFileContentAsync(fi.FileID, ms).Wait();
			}
		}


		public void Copy(IFSFile source)
		{
			using (var stream = source.GetContentAsStream())
			{
				this.SetContent(stream);
			}
		}


		public async Task CopyAsync(IRemoteFile source)
		{
			using (var stream = await source.GetContentAsStreamAsync())
			{
				await SetContentAsync(stream);
			}
		}


		public async Task SetContentAsync(byte[] fileContent)
		{
			using (var ms = new MemoryStream(fileContent))
			{
				await server.UploadFileContentAsync(fi.FileID, ms);
			}
		}


		public async Task SetContentAsync(Guid fileguid)
		{
			await server.SetFileContentAsync(fi.FileID, fileguid);
		}


		public IEnumerable<string> AttachmentCategories
		{
			get
			{
				foreach (var cat in owner.categories)
					yield return cat.CategoryName;
			}
		}


		public IAttachmentCollection GetAttachmentCategory(string category)
		{
			var cat = owner.categories.FirstOrDefault(c => String.Compare(c.CategoryName, category, true) == 0);
			if (cat != null)
				return new RemoteAttachmentCollection(this.owner, fi, cat);
			else
				throw new InvalidOperationException($"Category {category} does not exist");
		}


		public Task<IRemoteAttachmentCollection> GetAttachmentCategoryAsync(string category)
		{
			var cat = owner.categories.FirstOrDefault(c => String.Compare(c.CategoryName, category, true) == 0);
			if (cat != null)
				return Task.FromResult<IRemoteAttachmentCollection>(new RemoteAttachmentCollection(this.owner, fi, cat));
			else
				throw new InvalidOperationException($"Category {category} does not exist");
		}


		public void Delete()
		{
			server.DeleteFileAsync(fi.FileID).Wait();
		}


		public async Task DeleteAsync()
		{
			await server.DeleteFileAsync(fi.FileID);
		}
	}
}
