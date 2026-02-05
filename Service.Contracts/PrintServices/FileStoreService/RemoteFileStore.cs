using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class RemoteFileStore : IRemoteFileStore
	{
		private object syncObj = new object();
		private IMsgPeer peer;
		private int storeid;
		private string storename;
		private Func<Task<FSStoreInfo>> getStoreConfig;
		private ILogService log;
		internal IFileStoreServer server;
		internal IFileStoreManager storeManager;
		internal FSStoreInfo storeInfo;
		internal List<FSCategoryInfo> categories;
		internal volatile bool disposed;

		public RemoteFileStore(IMsgPeer peer, IFileStoreManager storeManager, ILogService log)
		{
			this.log = log;
			this.peer = peer;
			peer.OnDisconnect += Peer_OnDisconnect;
			this.storeManager = storeManager;
		}

		private void Peer_OnDisconnect(object sender, EventArgs e)
		{
			if(storeInfo != null)
				log.LogMessage($"FileStore {storename} connection to {storeInfo.Host}:{storeInfo.Port} was closed.");
			else
				log.LogMessage($"FileStore {storename} connection to service was closed.");
		}

		public void Dispose()
		{
			disposed = true;
			log.LogMessage($"FileStore {storename} was disposed. Avoid disposing a remote file store unless the program is shutting down.");
			peer.Dispose();
			server = null;
		}


		public int StoreID { get => storeid; }


		public string StoreName { get => storename; }


		public bool Disposed { get => disposed; }


		public void Configure(int storeid)
		{
			this.storeid = storeid;
			getStoreConfig = () => storeManager.GetStoreConfigAsync(this.storeid);
		}


		public void Configure(string storename)
		{
			this.storename = storename;
			getStoreConfig = () => storeManager.GetStoreConfigAsync(this.storename);
		}


		private void CheckConnection()
		{
			lock (syncObj)
			{
				if (storeInfo == null)
				{
					storeInfo = getStoreConfig().Result;
					storename = storeInfo.StoreName;
					storeid = storeInfo.StoreID;
				}

				if (!peer.IsConnected)
				{
					peer.Connect(storeInfo.Host, storeInfo.Port);
					log.LogMessage($"FileStore {storename} connected to {storeInfo.Host}:{storeInfo.Port}");
				}

				if (server == null)
					server = peer.GetServiceProxy<IFileStoreServer>();

				if (categories == null)
					categories = server.GetCategoriesAsync().Result;
			}
		}


		public FSCategoryInfo GetCategoryByID(int categoryid)
		{
			CheckConnection();
			return categories.FirstOrDefault(c => c.CategoryID == categoryid);
		}


		public bool TryGetFile(int id, out IFileData file)
		{
			CheckConnection();
			FSFileInfo fi = server.GetFileAsync(id).Result;
			if (fi != null)
			{
				file = new RemoteFile(this, fi);
				return true;
			}
			else
			{
				file = null;
				return false;
			}
		}


		public async Task<IRemoteFile> TryGetFileAsync(int fileid)
		{
			CheckConnection();
			FSFileInfo fi = await server.GetFileAsync(fileid);
			if (fi != null)
			{
				return new RemoteFile(this, fi);
			}
			else
			{
				return null;
			}
		}


		public async Task<IRemoteFile> TryGetFileAsync(Guid fileguid)
		{
			CheckConnection();
			FSFileReference fr = FSFileReference.FromGuid(fileguid);
			if (fr.StoreID != storeid)
				throw new InvalidOperationException($"The referenced file is not from {storename} file store.");

			RemoteFile file = null;
			FSFileInfo fi = await server.GetFileAsync(fr.FileID);
			if(fi != null)
				file = new RemoteFile(this, fi);

			switch (fr.Type)
			{
				case FSFileReferenceType.File:
					return file;
				case FSFileReferenceType.Attachment:
				default:
					throw new NotImplementedException($"Unsupported FileReferenceType {fr.Type}");
			}
		}


		public IFileData GetOrCreateFile(int id, string filename)
		{
			CheckConnection();
			FSFileInfo fi = server.GetOrCreateFileAsync(id, filename).Result;
			var file = new RemoteFile(this, fi);
			return file;
		}


		public async Task<IRemoteFile> GetOrCreateFileAsync(int fileid, string filename)
		{
			CheckConnection();
			FSFileInfo fi = await server.GetOrCreateFileAsync(fileid, filename);
			var file = new RemoteFile(this, fi);
			return file;
		}


		public IFileData CreateFile(int id, string filename)
		{
			CheckConnection();
			FSFileInfo fi = server.CreateFileAsync(id, filename).Result;
			var file = new RemoteFile(this, fi);
			return file;
		}


		public async Task<IRemoteFile> CreateFileAsync(int fileid, string filename)
		{
			CheckConnection();
			FSFileInfo fi = await server.CreateFileAsync(fileid, filename);
			var file = new RemoteFile(this, fi);
			return file;
		}


		public IRemoteFile CreateFile(string filename)
		{
			return CreateFileAsync(filename).Result;
		}


		public async Task<IRemoteFile> CreateFileAsync(string filename)
		{
			CheckConnection();
			FSFileInfo fi = await server.CreateFileAsync(filename);
			var file = new RemoteFile(this, fi);
			return file;
		}


		public void DeleteFile(int id)
		{
			CheckConnection();
			server.DeleteFileAsync(id).Wait();
		}


		public async Task DeleteFileAsync(int fileid)
		{
			CheckConnection();
			await server.DeleteFileAsync(fileid);
		}


		public async Task<IRemoteAttachment> GetFileAttachmentAsync(int fileid, string category, string filename)
		{
			CheckConnection();
			var attachmentInfo = await server.GetFileAttachmentAsync(fileid, category, filename);
			if (attachmentInfo != null)
				return new RemoteAttachment(this, attachmentInfo);
			else
				throw new FileNotFoundException($"Attachment {filename} was not found in category {category} in file {fileid}");
		}


		public IEnumerable<string> Categories
		{
			get
			{
				CheckConnection();
				foreach (var cat in categories)
					yield return cat.CategoryName;
			}
		}
	}
}
