using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public class FileStoreHelper
	{
		public static Stream GetFileStream(string path)
		{
			int retryCount = 0;
			int sleepTime = 100;
			do
			{
				retryCount++;
				try
				{
					FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
					var result = new StreamEx(fs);
					return result;
				}
				catch (FileNotFoundException fnf)
				{
					throw new FileNotFoundException($"Could not locate the specified file: {path}", fnf);
				}
				catch (IOException)
				{
					if (retryCount > 20)
						throw;
					Thread.Sleep(sleepTime);
					sleepTime += 100;
				}
			} while (true);
		}


		public static Stream WaitToRead(string path)
		{
			int retryCount = 0;
			int sleepTime = 0;
			do
			{
				if (sleepTime > 0)
					Thread.Sleep(sleepTime);
				sleepTime += 100;
				retryCount++;

				try
				{
					return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
				}
				catch (FileNotFoundException fnf)
				{
					throw new FileNotFoundException($"Could not locate the specified file: {path}", fnf);
				}
				catch (DirectoryNotFoundException fnf)
				{
					throw new FileNotFoundException($"Could not locate the specified file: {path}", fnf);
				}
				catch (IOException)
				{
					if (retryCount >= 20)
						throw;
				}
			} while (true);
		}


		public static Stream WaitToWrite(string path)
		{
			int retryCount = 0;
			int sleepTime = 0;
			do
			{
				if (sleepTime > 0)
					Thread.Sleep(sleepTime);
				sleepTime += 37;
				retryCount++;
				try
				{
					return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				}
				catch (FileNotFoundException fnf)
				{
					throw new FileNotFoundException($"Could not locate the specified file: {path}", fnf);
				}
				catch (DirectoryNotFoundException fnf)
				{
					throw new FileNotFoundException($"Could not locate the specified file: {path}", fnf);
				}
				catch (IOException)
				{
					if (retryCount > 10)
						throw;
				}
			} while (true);
		}


		public static async Task<byte[]> ReadFileContentAsync(string file)
		{
			int retryCount = 0;
			do
			{
				try
				{
					return File.ReadAllBytes(file);
				}
				catch (IOException)
				{
					retryCount++;
					if (retryCount >= 10)
						throw;
				}
				await Task.Delay(250);
			} while (true);
		}


		public static async Task EnsureCanReadAsync(string fileName, bool checkZeroLength = false)
		{
            await EnsureCanReadAsync(fileName, checkZeroLength, 100 );// wait default time
		}

        public static async Task EnsureCanReadAsync(string fileName, bool checkZeroLength = false, int waitTimeMs = 100)
        {
            var retryCount = 0;
            var maxTrys = 32;
            var decay = 16;
            while (true)
            {
                try
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (checkZeroLength && fs.Length == 0)
                            throw new Exception($"File {fileName} is empty");
                    }
                    return;
                }
                catch (Exception)
                {
                    retryCount++;
                    await Task.Delay(waitTimeMs);
                    waitTimeMs += decay;
                    if (retryCount >= maxTrys)
                        throw;
                }
            }
        }


        public static void DeleteDirectory(string directory, bool deleteTopDirectory)
		{
			if (Directory.Exists(directory))
			{
				DeleteFiles(directory);
				string[] subDirectories = Directory.GetDirectories(directory);
				foreach (string subdir in subDirectories)
				{
					DeleteDirectory(subdir, true);
				}
				try
				{
					if (deleteTopDirectory)
					{
						Directory.Delete(directory);
					}
				}
				catch (IOException)
				{
				}
			}
		}


		// Attempts to remove all files from the specified directory. Errors are ignored.
		private static void DeleteFiles(string directory)
		{
			if (Directory.Exists(directory))
			{
				string[] files = Directory.GetFiles(directory);
				DeleteFiles(files);
			}
		}


		private static void DeleteFiles(string[] fileNames)
		{
			if (fileNames != null)
			{
				foreach (string file in fileNames)
				{
					if (File.Exists(file))
					{
						try
						{
							FileInfo fInfo = new FileInfo(file);
							fInfo.IsReadOnly = false;
							File.Delete(file);
						}
						catch { }
					}
				}
			}
		}

		internal static string SanitizeFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
		}


		internal static void ValidateFileName(string value)
		{
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
			{
				if (value.IndexOf(c) >= 0)
					throw new Exception($"Invalid file name, character {c} is not valid.");
			}
		}


		/* There can be up to 3 container levels, by default 3 levels are used, altough it is recomended to 
		 * reduce to 2 or even 1, as having 3 levels can create far too many subfolders (one million).
		 * 
		 * This is how subfolders would be determined when using 3 levels:
		 * 
		 *		int container1 = (fileid / 10000) % 100;		//RC
		 *		int container2 = (fileid / 100) % 100;			//SC
		 *		int container3 = (fileid / 1) % 100;			//TC
		 *		
		 *	This distributes files across different subfolder levels, each level having 100 folders.
		 *	While the number of levels is configurable, once the system is deployed the number of
		 *	levels should not be changed, as that would require the files to be rearranged.
		 */

		private static string[] containers = new string[] {
			"RC", "SC", "TC"
		};

		public static string GetFilePhysicalPath(string basedir, int containerLevels, int fileid)
		{
			int containerid;
			StringBuilder sb = new StringBuilder(100);
			int denominator = Convert.ToInt32(Math.Pow(100, containerLevels - 1));
			for (int i = 0; i < containerLevels; i++)
			{
				containerid = (fileid / denominator) % 100;
				sb.AppendFormat("{0}{1}\\", containers[i], containerid.ToString("D2"));
				denominator /= 100;
			}
			string path = Path.Combine(basedir, sb.ToString(), fileid.ToString("D6"));
			return path;
		}


		public static string GetAttachmentPhysicalPath(string basedir, int containerLevels, int fileid, string categoryName, string fileName)
		{
			string filepath = GetFilePhysicalPath(basedir, containerLevels, fileid);
			string attachmentpath = Path.Combine(filepath, categoryName, fileName);
			return attachmentpath;
		}

		internal static void ReplaceFile(string source, string target)
		{
			using(var srcStream = File.OpenRead(source))
			{
				using(var dstStream = File.OpenWrite(target))
				{
					dstStream.SetLength(0L);
					srcStream.CopyTo(dstStream, 4096);
				}
			}
		}


		internal static void TryDeleteFile(string filename)
		{
			bool success = false;
			int retryCount = 0;
			do
			{
				try
				{
					File.Delete(filename);
					success = true;
				}
				catch {
					retryCount++;
				}
			} while (!success && retryCount < 5);
		}


		private static string GetRandomName(string filePath)
		{
			var rnd = new Random();
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
					result = Path.Combine(pathName, fileName + "_" + rnd.Next(0, 1000000000).ToString("D9") + extension);
				}

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
					if (createRetry >= 5)
						throw;
				}
			} while (!success);
			return result;
		}
	}
}
