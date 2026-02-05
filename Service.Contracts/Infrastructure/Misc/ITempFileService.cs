using Services.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Service.Contracts
{
	public interface ITempFileService
	{
		string CreateTmpFileName(string file);
		string GetTempFileName(bool createFile = false, string extension = ".tmp");
		string GetTempFileName(string baseFileName, bool createFile = false);
		TempFileInfo GetTempFile(string extension = ".tmp");
		string GetTempDirectory();
		byte[] ReadTempFile(string tempFile);
		void DeleteTempFile(string tempFile, int retries = 5);
		void DeleteTempDirectory(string basePath, int retries = 5);
		string SanitizeFileName(string v);
		void RegisterForDelete(string tempFile);
		void RegisterForDelete(string tempFile, DateTime expirationDate);
	}

	public sealed class TempFileInfo : IDisposable
	{
		private ITempFileService srv;
		// al final recordar cambiar a internal
		public TempFileInfo(ITempFileService srv, string path)
		{
			this.srv = srv;
			FilePath = path;
		} 

		public void Dispose()
		{
			srv.DeleteTempFile(FilePath);
		}

		public string FilePath { get; }

		public override string ToString()
		{
			return FilePath;
		}

		public string ReadFileContent(Encoding encoding)
		{
			int retryCount = 0;
			int sleepTime = 50;
			do
			{
				try
				{
					return File.ReadAllText(FilePath, encoding);
				}
				catch (IOException)
				{
					retryCount++;
					if (retryCount >= 5) 
						throw;
					Thread.Sleep(sleepTime);
					sleepTime += 50;
				}
			} while (true);
		}
	}

	public class TempFileService: ITempFileService
	{
        private static readonly object syncObj = new object();
		private static readonly Random rnd = new Random();
		private Timer timer;

		private IAppInfo appInfo;
		private ILogService log;
		private ConcurrentDictionary<string, DateTime> pendingDeletion = new ConcurrentDictionary<string, DateTime>();


        static TempFileService()
        {
            try
            {
                using(RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    byte[] arr = new byte[4];
                    rng.GetBytes(arr);
                    int seed = BitConverter.ToInt32(arr, 0);
                    rnd = new Random(seed);
                }
            }
            catch
            {
                rnd = new Random(Process.GetCurrentProcess().Id);
            }
        }

        public TempFileService(IAppInfo appInfo, ILogService log)
		{
			this.appInfo = appInfo;
			this.log = log;
			timer = new Timer(ClearTempFiles, null, 120000, Timeout.Infinite);
		}


		public void RegisterForDelete(string filepath)
		{
			pendingDeletion.TryAdd(filepath, DateTime.Now);
		}

		public void RegisterForDelete(string filepath, DateTime expirationDate)
		{
			if (expirationDate < DateTime.Now)
				expirationDate = DateTime.Now;

			pendingDeletion.TryAdd(filepath, expirationDate);
		}


		private void ClearTempFiles(object state)
		{
			try
			{
				DeleteRegisteredForDeletion();
				DeleteOldTempFiles(appInfo.UserTempDir, TimeSpan.FromHours(2));
				DeleteOldTempFiles(appInfo.SystemTempDir, TimeSpan.FromHours(2));
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
			finally
			{
				timer.Change((int)TimeSpan.FromHours(2).TotalMilliseconds, Timeout.Infinite);
			}
		}


		private void DeleteRegisteredForDeletion()
		{
			var keys = pendingDeletion.Keys.ToList();
			foreach (string key in keys)
			{
				try
				{
					if(pendingDeletion.TryGetValue(key, out var date))
					{
						if(date < DateTime.Now)
						{
							if (File.Exists(key))
								File.Delete(key);
							pendingDeletion.TryRemove(key, out _);
						}
					}
				}
				catch (Exception) { }
			}
		}


		private void DeleteOldTempFiles(string basePath, TimeSpan timeThreshold, bool deleteDirectory = false)
		{
			if (!Directory.Exists(basePath))
				return;
			var dir = new DirectoryInfo(basePath);
			foreach (var file in dir.EnumerateFiles())
			{
				try
				{
					if (file.LastWriteTime.Add(timeThreshold) < DateTime.Now)
						file.Delete();
				}
				catch { }
			}
			foreach (var subdir in dir.EnumerateDirectories())
				DeleteOldTempFiles(subdir.FullName, timeThreshold, true);

			if (deleteDirectory)
			{
				try
				{
					if(!dir.EnumerateFiles().Any())
						dir.Delete(true);
				}
				catch { }
			}
		}


		public string CreateTmpFileName(string file)
		{
			string result;
			string path = Path.GetDirectoryName(file);
			string fileName = Path.GetFileNameWithoutExtension(file);
			do
			{
                int randomNum;
                lock(syncObj)
                    randomNum = rnd.Next(0, 1000000000);

                result = Path.Combine(path, fileName + "_" + randomNum.ToString("D9") + ".tmp");
			} while (File.Exists(result));
			return result;
		}


		public string GetTempFileName(bool createFile = false, string extension = ".tmp")
		{
			string baseFileName;
			baseFileName = Path.Combine(appInfo.SystemTempDir, $"TempFile{extension}");
			return GetRandomName(baseFileName, createFile);
		}

		
		public string GetTempFileName(string baseFileName, bool createFile = false)
		{
			var dir = Path.GetDirectoryName(baseFileName);
			if (String.IsNullOrWhiteSpace(dir))
			{
				baseFileName = Path.Combine(appInfo.SystemTempDir, baseFileName);
			}
			return GetRandomName(baseFileName, createFile);
		}


		public TempFileInfo GetTempFile(string extension = ".tmp")
		{
			return new TempFileInfo(this, GetTempFileName(true, extension));
		}


		public string GetTempDirectory()
		{
			string result;
			string basePath;
			basePath = appInfo.SystemTempDir;
			lock(syncObj)
			{
				do
				{
                    int randomNum;
                    lock(syncObj)
                        randomNum = rnd.Next(0, 1000000000);

                    result = Path.Combine(basePath, "Tmp_" + randomNum.ToString("D9"));
				} while (Directory.Exists(result));
				Directory.CreateDirectory(result);
			}
			return result;
		}


        public string GetRandomName(string filePath, bool createFile = false)
        {
            return GetRandomTempFileName(filePath, createFile);
        }

        public static string GetRandomTempFileName(string filePath, bool createFile = false)
		{
			int createRetry = 0;
			bool success = false;
			string pathName = Path.GetDirectoryName(filePath);
			string fileName = Path.GetFileNameWithoutExtension(filePath);
			string extension = Path.GetExtension(filePath);
			string result = Path.Combine(pathName, fileName + extension);
			do
			{
				while (File.Exists(result))
				{
                    int randomNum;
                    lock(syncObj)
                        randomNum = rnd.Next(0, 1000000000);
                    result = Path.Combine(pathName, fileName + "_" + randomNum.ToString("D9") + extension);
				}
				if (createFile)
				{
					try
					{
						using (FileStream fs = File.Open(result, FileMode.CreateNew))
						{
							success = true;
						}
					}
					catch
					{
						createRetry++;
						if (createRetry >= 30)
							throw;
					}
				}
				else success = true;
			} while (!success);
			return result;
		}


		public byte[] ReadTempFile(string tempFile)
		{
			int retryCount = 0;
			do
			{
				try
				{
					return File.ReadAllBytes(tempFile);
				}
				catch
				{
					Thread.Sleep(100);
					retryCount++;
				}
			} while (retryCount < 5);
			throw new Exception("Could not read from file " + tempFile);
		}


		private bool RetryTask(Action action, int retries, bool ignoreExceptions = true)
		{
			Exception lastExt = null;
			var waitTime = 200;
			bool success = false;
			int retryCount = 0;
			do
			{
				try
				{
					action();
					success = true;
				}
				catch(Exception ex)
				{
					lastExt = ex;
					Thread.Sleep(waitTime);
					retryCount++;
					waitTime += 100;
				}
			} while (!success && retryCount < retries);
			if (!success && !ignoreExceptions)
				throw lastExt;
			else
				return success;
		}



		public void DeleteTempFile(string tempFile, int retries = 5)
		{
			RetryTask(() =>
			{
				if (File.Exists(tempFile))
					File.Delete(tempFile);
			}, retries, true);
		}



		public void DeleteTempDirectory(string basePath, int retries = 5)
		{
			if (!Directory.Exists(basePath))
				return;
			var dir = new DirectoryInfo(basePath);
			foreach (var file in dir.EnumerateFiles())
				DeleteTempFile(file.FullName, retries);
			foreach (var subdir in dir.EnumerateDirectories())
				DeleteTempDirectory(subdir.FullName, retries);
			RetryTask(() =>	Directory.Delete(basePath, true), retries, true);
		}


		public string SanitizeFileName(string filename)
		{
			var invalidChars = new List<char>(Path.GetInvalidFileNameChars());
			invalidChars.AddRange(Path.GetInvalidPathChars());
			StringBuilder sb = new StringBuilder(filename.Length);
			foreach(var c in filename)
			{
				if (invalidChars.IndexOf(c) < 0)
					sb.Append(c);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}
	}
}
