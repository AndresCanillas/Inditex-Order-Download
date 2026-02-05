using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class FileStoreManager : IFileStoreManager
	{
		private object syncObj = new object();
		private IFactory factory;
		private string server;
		private int port;
		private volatile List<FSStoreInfo> configuredStores;
		private ConcurrentDictionary<string, IRemoteFileStore> openStores;

		public FileStoreManager(IFactory factory, IAppConfig config)
		{
			this.factory = factory;
			server = config.GetValue<string>("FileStoreManager.Server");
			port = config.GetValue<int>("FileStoreManager.Port");
			configuredStores = null;
			openStores = new ConcurrentDictionary<string, IRemoteFileStore>();
		}

		public async Task<List<FSStoreInfo>> GetAllStoresAsync()
		{
			if (configuredStores == null)
				await LoadStoresAsync();

			var json = JsonConvert.SerializeObject(configuredStores);
			return JsonConvert.DeserializeObject<List<FSStoreInfo>>(json); // creates a deep clone of the stores
		}


		public async Task<FSStoreInfo> GetStoreConfigAsync(string storename)
		{
			if (configuredStores == null)
				await LoadStoresAsync();

			var storeConfig = configuredStores.FirstOrDefault(s => String.Compare(s.StoreName, storename, true) == 0);
			if (storeConfig == null)
				throw new Exception($"Specified file store name [{storename}] is not present in the system configuration.");

			return storeConfig;
		}


		public async Task<FSStoreInfo> GetStoreConfigAsync(int storeid)
		{
			if (configuredStores == null)
				await LoadStoresAsync();

			var storeConfig = configuredStores.FirstOrDefault(s => s.StoreID == storeid);
			if (storeConfig == null)
				throw new Exception($"Specified file store id [{storeid}] is not present in the system configuration.");

			return storeConfig;
		}


		public IRemoteFileStore OpenStore(string storename)
		{
			IRemoteFileStore store;
			do
			{
				if (!openStores.TryGetValue(storename, out store))
				{
					store = factory.GetInstance<IRemoteFileStore>();
					store.Configure(storename);
					if (!openStores.TryAdd(storename, store))
					{
						store.Dispose();
						store = openStores[storename];
					}
				}
				else
				{
					if (store.Disposed)
					{
						openStores.TryRemove(storename, out _);
						store = null;
					}
				}
			} while (store == null);
			return store;
		}


		public IRemoteFileStore OpenStore(int storeid)
		{
			var store = openStores.Values.FirstOrDefault(s => s.StoreID == storeid);
			if (store == null)
			{
				store = factory.GetInstance<IRemoteFileStore>();
				store.Configure(storeid);
				if (!openStores.TryAdd(storeid.ToString(), store))
				{
					store.Dispose();
					store = openStores[storeid.ToString()];
				}
			}
			return store;
		}


		public IFSFile GetFile(Guid fileguid)
		{
			if (fileguid == Guid.Empty)
				throw new InvalidOperationException("The given FileGUID is invalid, it cannot be empty.");

			return GetFileAsync(fileguid).Result;
		}


		public async Task<IFSFile> GetFileAsync(Guid fileguid)
		{
			if (fileguid == Guid.Empty)
				throw new InvalidOperationException("The given FileGUID is invalid, it cannot be empty.");

			var fr = FSFileReference.FromGuid(fileguid);
			var store = OpenStore(fr.StoreID);
			var file = await store.TryGetFileAsync(fr.FileID);

			if (fr.Type == FSFileReferenceType.File)
			{
				return file;
			}
			else if (fr.Type == FSFileReferenceType.Attachment)
			{
				if (file == null)
					throw new FileNotFoundException($"File StoreID[{fr.StoreID}].FileID[{fr.FileID}] could not be found");

				var category = store.GetCategoryByID(fr.CategoryID);
				var attachmentCollection = file.GetAttachmentCategory(category.CategoryName) as RemoteAttachmentCollection;
				attachmentCollection.TryGetAttachmentByID(fr.AttachmentID, out var attachment);
				return attachment;
			}
			else throw new NotImplementedException($"Unsupported FileReferenceType {fr.Type}");
		}


		public void DeleteFile(Guid fileguid)
		{
			if (fileguid == Guid.Empty)
				throw new InvalidOperationException("The given FileGUID is invalid, it cannot be empty.");

			DeleteFileAsync(fileguid).Wait();
		}


		public async Task DeleteFileAsync(Guid fileguid)
		{
			if (fileguid == Guid.Empty)
				throw new InvalidOperationException("The given FileGUID is invalid, it cannot be empty.");

			var fr = FSFileReference.FromGuid(fileguid);
			var store = OpenStore(fr.StoreID);
			await store.DeleteFileAsync(fr.FileID);
		}


		public void CopyFile(Guid sourceFile, Guid targetFile)
		{
			CopyFileAsync(sourceFile, targetFile).Wait();
		}


		public async Task CopyFileAsync(Guid sourceFile, Guid targetFile)
		{
			if (sourceFile == Guid.Empty)
				throw new InvalidOperationException("The sourceFile FileGUID is invalid, it cannot be empty.");

			if (targetFile == Guid.Empty)
				throw new InvalidOperationException("The targetFile FileGUID is invalid, it cannot be empty.");

			var fr = FSFileReference.FromGuid(sourceFile);
			var sourceStore = OpenStore(fr.StoreID);
			var source = await sourceStore.TryGetFileAsync(fr.FileID);
			if (source == null)
				throw new InvalidOperationException("The sourceFile FileGUID is invalid, the file could not be found in the file store.");

			fr = FSFileReference.FromGuid(targetFile);
			var targetStore = OpenStore(fr.StoreID);
			var target = await targetStore.TryGetFileAsync(fr.FileID);
			if (target == null)
				throw new InvalidOperationException("The targetFile FileGUID is invalid, the file could not be found in the file store.");

			await target.SetContentAsync(await source.GetContentAsBytesAsync());
		}


		private async Task LoadStoresAsync()
		{
			using (var peer = factory.GetInstance<IMsgPeer>())
			{
				peer.Connect(server, port);
				var cfgSrv = peer.GetServiceProxy<IFileStoreConfigService>();
				var stores = await cfgSrv.GetAllAsync();
				lock (syncObj)
				{
					if (configuredStores == null)
						configuredStores = stores;
				}
			}
		}
	}
}
