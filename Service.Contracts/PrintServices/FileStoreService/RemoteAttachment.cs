using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	class RemoteAttachment : IRemoteAttachment
	{
		private RemoteFileStore owner;
		private FSAttachmentInfo attinfo;
		private IFileStoreServer server;
		private List<UserMeta> userMeta;


		public RemoteAttachment(RemoteFileStore owner, FSAttachmentInfo attinfo)
		{
			this.owner = owner;
			this.attinfo = attinfo;
			server = owner.server;
		}

		public Guid FileGUID { get => attinfo.GetFileGUID(); }

		public int FileID { get => attinfo.FileID; }

		public int CategoryID { get => attinfo.CategoryID; }

		public int AttachmentID { get => attinfo.AttachmentID; }

		public long FileSize { get => attinfo.FileSize; }

		public bool HasPhysicalCopy { get => attinfo.HasPhysicalCopy; }

		public string PhysicalPath { get => attinfo.PhysicalPath; }

		public DateTime CreatedDate { get => attinfo.CreateDate; }

		public DateTime UpdatedDate { get => attinfo.UpdateDate; }

		public string FileName
		{
			get => attinfo.FileName;
			set
			{
				FileStoreHelper.ValidateFileName(value);
				server.RenameAttachmentAsync(attinfo, value).Wait();
				attinfo.FileName = value;
			}
		}


		public async Task RenameAsync(string filename)
		{
			if (filename != attinfo.FileName)
			{
				FileStoreHelper.ValidateFileName(filename);
				await server.RenameAttachmentAsync(attinfo, filename);
				attinfo.FileName = filename;
			}
		}


		public T GetMetadata<T>() where T : new()
		{
			return GetMetadataAsync<T>().Result;
		}


		public async Task<T> GetMetadataAsync<T>() where T : new()
		{
			if (userMeta == null)
				await LoadMetadataAsync();

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				return JsonConvert.DeserializeObject<T>(typeMeta.Content);
			else
				return new T();
		}


		public void SetMetadata<T>(T meta) where T : new()
		{
			SetMetadataAsync(meta).Wait();
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
			userMeta = new List<UserMeta>();
			using (var stream = await server.DownloadAttachmentMetadataAsync(attinfo))
			{
				var metaBytes = await stream.LoadToMemoryAsync();
				var json = Encoding.UTF8.GetString(metaBytes).Trim();
				if (json != null && json.Length > 0)
				{
					if (json[0] == 65279)
						json = json.Substring(1);
					try
					{
						userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
					}
					catch { }
				}
			}
		}


		private async Task UpdateMetadataAsync()
		{
			var metaBytes = await GetMetadataBytesAsync();
			using (MemoryStream ms = new MemoryStream(metaBytes))
			{
				await server.UploadAttachmentMetadataAsync(attinfo, ms);
			}
		}


		public byte[] GetMetadataBytes()
		{
			return GetMetadataBytesAsync().Result;
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
			SetMetadataBytesAsync(metadata).Wait();
		}


		public async Task SetMetadataBytesAsync(byte[] metadata)
		{
			using (MemoryStream ms = new MemoryStream(metadata))
			{
				await server.UploadAttachmentMetadataAsync(attinfo, ms);
			}
			userMeta = null;
		}


		public Stream GetContentAsStream()
		{
			return server.DownloadAttachmentContentAsync(attinfo).Result;
		}

		public async Task<Stream> GetContentAsStreamAsync()
		{
			return await server.DownloadAttachmentContentAsync(attinfo);
		}


		public byte[] GetContentAsBytes()
		{
			using (var stream = server.DownloadAttachmentContentAsync(attinfo).Result)
			{
				return stream.LoadToMemoryAsync().Result;
			}
		}

		public async Task<byte[]> GetContentAsBytesAsync()
		{
			using (var stream = await server.DownloadAttachmentContentAsync(attinfo))
			{
				return await stream.LoadToMemoryAsync();
			}
		}

		public void SetContent(string sourceFile)
		{
			using (var fs = File.OpenRead(sourceFile))
			{
				server.UploadAttachmentContentAsync(attinfo, fs).Wait();
			}
		}

		public async Task SetContentAsync(string sourceFile)
		{
			using (var fs = File.OpenRead(sourceFile))
			{
				await server.UploadAttachmentContentAsync(attinfo, fs);
			}
		}

		public void SetContent(Stream stream)
		{
			server.UploadAttachmentContentAsync(attinfo, stream).Wait();
		}

		public async Task SetContentAsync(Stream stream)
		{
			await server.UploadAttachmentContentAsync(attinfo, stream);
		}

		public void SetContent(byte[] fileContent)
		{
			using (var ms = new MemoryStream(fileContent))
			{
				server.UploadAttachmentContentAsync(attinfo, ms).Wait();
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
				await server.UploadAttachmentContentAsync(attinfo, ms);
			}
		}

		public async Task SetContentAsync(Guid fileguid)
		{
			await server.SetAttachmentContentAsync(attinfo, fileguid);
		}

		public void Delete()
		{
			server.DeleteAttachmentAsync(attinfo).Wait();
		}

		public async Task DeleteAsync()
		{
			await server.DeleteAttachmentAsync(attinfo);
		}
	}
}
