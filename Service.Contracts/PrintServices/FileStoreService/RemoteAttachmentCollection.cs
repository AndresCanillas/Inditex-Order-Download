using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	class RemoteAttachmentCollection : IRemoteAttachmentCollection
	{
		private RemoteFileStore owner;
		private FSFileInfo file;
		private FSCategoryInfo category;
		private FSFileCategoryInfo collectionInfo;
		private IFileStoreServer server;

		public RemoteAttachmentCollection(RemoteFileStore owner, FSFileInfo file, FSCategoryInfo category)
		{
			this.owner = owner;
			this.file = file;
			this.category = category;
			this.server = owner.server;
			collectionInfo = server.GetFileCategoryAsync(file.FileID, category.CategoryID).Result;
		}


		public string CategoryName { get => category.CategoryName; }

		public bool HasPhysicalCopy { get => owner.storeInfo.HasPhysicalCopy; }

		public string PhysicalPath { get => Path.Combine(file.PhysicalPath, category.CategoryName); }


		public int Count
		{
			get
			{
				return collectionInfo.Attachments.Count;
			}
		}


		public bool TryGetAttachment(string filename, out IAttachmentData attachment)
		{
			var attinfo = collectionInfo.Attachments.FirstOrDefault(a => String.Compare(a.FileName, filename, true) == 0);
			if(attinfo == null)
			{
				attachment = null;
				return false;
			}
			else
			{
				attachment = new RemoteAttachment(owner, attinfo);
				return true;
			}
		}


		public Task<IRemoteAttachment> TryGetAttachmentAsync(string filename)
		{
			var attinfo = collectionInfo.Attachments.FirstOrDefault(a => String.Compare(a.FileName, filename, true) == 0);
			if (attinfo == null)
			{
				return Task.FromResult<IRemoteAttachment>(null);
			}
			else
			{
				return Task.FromResult<IRemoteAttachment>(new RemoteAttachment(owner, attinfo));
			}
		}


		public Task<IRemoteAttachment> TryGetAttachmentAsync(int attachmentid)
		{
			var attinfo = collectionInfo.Attachments.FirstOrDefault(a => a.AttachmentID == attachmentid);
			if (attinfo == null)
			{
				return Task.FromResult<IRemoteAttachment>(null);
			}
			else
			{
				return Task.FromResult<IRemoteAttachment>(new RemoteAttachment(owner, attinfo));
			}
		}


		public bool TryGetAttachmentByID(int attachmentid, out IAttachmentData attachment)
		{
			var attinfo = collectionInfo.Attachments.FirstOrDefault(a => a.AttachmentID == attachmentid);
			if (attinfo == null)
			{
				attachment = null;
				return false;
			}
			else
			{
				attachment = new RemoteAttachment(owner, attinfo);
				return true;
			}
		}


		public Task<IRemoteAttachment> TryGetAttachmentByID(int attachmentid)
		{
			var attinfo = collectionInfo.Attachments.FirstOrDefault(a => a.AttachmentID == attachmentid);
			if (attinfo == null)
			{
				return Task.FromResult<IRemoteAttachment>(null);
			}
			else
			{
				return Task.FromResult<IRemoteAttachment>(new RemoteAttachment(owner, attinfo));
			}
		}


		public IAttachmentData CreateAttachment(string filename)
		{
			var attinfo = server.CreateAttachmentAsync(new FSAttachmentInfo() {
				FileID = file.FileID,
				CategoryID = category.CategoryID,
				FileName = filename
			}).Result;
			return new RemoteAttachment(owner, attinfo);
		}


		public async Task<IRemoteAttachment> CreateAttachmentAsync(string filename)
		{
			var attinfo = await server.CreateAttachmentAsync(new FSAttachmentInfo()
			{
				FileID = file.FileID,
				CategoryID = category.CategoryID,
				FileName = filename
			});
			return new RemoteAttachment(owner, attinfo);
		}


		public IAttachmentData GetOrCreateAttachment(string filename)
		{
			var attinfo = server.GetOrCreateAttachmentAsync(new FSAttachmentInfo()
			{
				FileID = file.FileID,
				CategoryID = category.CategoryID,
				FileName = filename
			}).Result;
			return new RemoteAttachment(owner, attinfo);
		}


		public async Task<IRemoteAttachment> GetOrCreateAttachmentAsync(string filename)
		{
			var attinfo = await server.GetOrCreateAttachmentAsync(new FSAttachmentInfo()
			{
				FileID = file.FileID,
				CategoryID = category.CategoryID,
				FileName = filename
			});
			return new RemoteAttachment(owner, attinfo);
		}


		public List<T> GetAllAttachmentMetadata<T>() where T : class, new()
		{
			var meta = GetAllAttachmentMetadataAsync<T>().Result;
			return meta;
		}


		public async Task<List<T>> GetAllAttachmentMetadataAsync<T>() where T : class, new()
		{
			List<UserMeta> userMeta;
			var result = new List<T>();
			var metadata = await server.GetFileCategoryMetadataAsync(file.FileID, category.CategoryID);
			foreach (var elm in metadata)
			{
				if (elm != null)
				{
					var json = Encoding.UTF8.GetString(elm).Trim();
					if (json != null && json.Length > 0)
					{
						if (json[0] == 65279)
							json = json.Substring(1);
						userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
					}
					else
					{
						userMeta = new List<UserMeta>();
					}

					var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
					if (typeMeta != null)
						result.Add(JsonConvert.DeserializeObject<T>(typeMeta.Content));
					else
						result.Add(default(T));
				}
				else result.Add(default(T));
			}
			return result;
		}


		public void DeleteAttachment(IAttachmentData attachment)
		{
			var remoteAtt = (attachment as IRemoteAttachment);
			if (remoteAtt == null || remoteAtt.FileID != file.FileID || remoteAtt.CategoryID != category.CategoryID)
				throw new InvalidOperationException("The specified attachment does not belong to this collection.");

			server.DeleteAttachmentAsync(new FSAttachmentInfo()
			{
				FileID = file.FileID,
				CategoryID = category.CategoryID,
				AttachmentID = (attachment as IRemoteAttachment).AttachmentID
			}).Wait();
		}


		public async Task DeleteAttachmentAsync(IRemoteAttachment attachment)
		{
			if (attachment == null || attachment.FileID != file.FileID || attachment.CategoryID != category.CategoryID)
				throw new InvalidOperationException("The specified attachment does not belong to this collection.");

			await server.DeleteAttachmentAsync(new FSAttachmentInfo()
			{
				FileID = file.FileID,
				CategoryID = category.CategoryID,
				AttachmentID = attachment.AttachmentID
			});
		}


		public void Clear()
		{
			server.ClearFileAttachmentCategoryAsync(file.FileID, category.CategoryID).Wait();
		}


		public async Task ClearAsync()
		{
			await server.ClearFileAttachmentCategoryAsync(file.FileID, category.CategoryID);
		}


		public IEnumerator<IAttachmentData> GetEnumerator()
		{
			foreach(var attinfo in collectionInfo.Attachments)
				yield return new RemoteAttachment(owner, attinfo);
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (var attinfo in collectionInfo.Attachments)
				yield return new RemoteAttachment(owner, attinfo);
		}
	}
}
