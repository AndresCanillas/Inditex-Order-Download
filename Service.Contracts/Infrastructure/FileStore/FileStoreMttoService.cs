using Services.Core;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Service.Contracts
{
	interface IFileStoreMttoService
	{
		void Register(string rootPath);
		void Unregister(string rootPath);
	}


	class FileStoreMttoService : IFileStoreMttoService
	{
		private ILogService log;
		private Timer mttoTimer;
		private ConcurrentDictionary<string, string> stores;

		public FileStoreMttoService(ILogService log)
		{
			this.log = log;
			mttoTimer = new Timer(checkAbandonedUploads, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
			stores = new ConcurrentDictionary<string, string>();
		}


		public void Register(string rootPath)
		{
			stores.TryAdd(rootPath, rootPath);
		}

		public void Unregister(string rootPath)
		{
			stores.TryRemove(rootPath, out _);
		}

		private void checkAbandonedUploads(object state)
		{
			mttoTimer.Change(Timeout.Infinite, Timeout.Infinite);
			try
			{
				foreach(string dir in stores.Keys)
					DeleteAbortedUploads(dir);
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				mttoTimer.Change(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
			}
		}

		private void DeleteAbortedUploads(string directory)
		{
			if (!Directory.Exists(directory))
				return;
			foreach (var file in Directory.EnumerateFiles(directory))
			{
				var fInfo = new FileInfo(file);
				if (file.EndsWith("_upload") && fInfo.LastAccessTime.AddDays(1) < DateTime.Now)
				{
					try { fInfo.Delete(); }
					catch { }
				}
			}
			foreach (var subdir in Directory.EnumerateDirectories(directory))
				DeleteAbortedUploads(subdir);
		}
	}
}
