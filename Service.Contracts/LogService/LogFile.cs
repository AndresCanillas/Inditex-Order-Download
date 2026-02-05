using Service.Contracts;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Services.Core
{
	class LogFile : ILogFile
	{
		public const int MIN_FILE_SIZE = 10485760;  // 10 MB
		public const int MAX_FILE_SIZE = 209715200; // 20 MB 

		private readonly object syncObj = new object();
		private readonly string filePath;
		private readonly FileStream fs;
		private readonly StreamWriter writer;

		private int maxSize = MAX_FILE_SIZE;
		private volatile bool initialized;

		public LogFile(string filePath, int maxSize = MIN_FILE_SIZE)
		{
			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			// Pevent maxSize go over the maximum size
			if(maxSize > MAX_FILE_SIZE)
				maxSize = MAX_FILE_SIZE;

			// Pevent maxSize to be below the minimum size
			if(maxSize < MIN_FILE_SIZE)
				maxSize = MIN_FILE_SIZE;

			this.maxSize = maxSize;

			var directory = Path.GetDirectoryName(filePath)
				?? throw new InvalidOperationException($"Invalid filePath: {filePath}");

			Directory.CreateDirectory(directory);
			CleanTmpFiles(directory);

			var retryCount = 0;
			var success = false;
            do
            {
                try
                {
                    retryCount++;
                    fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 4096);
                    fs.Seek(0, SeekOrigin.End);
                    writer = new StreamWriter(fs);

                    var security = new FileSecurity();

                    security.AddAccessRule(new FileSystemAccessRule(
                        new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                        FileSystemRights.FullControl,
                        AccessControlType.Allow
                    ));

                    var fInfo = new FileInfo(filePath);
                    fInfo.SetAccessControl(security);

                    success = true;
                }
                catch(IOException)
                {
                    if(retryCount > 3)
                        throw;
                    try
                    {
                        File.Move(filePath, TempFileService.GetRandomTempFileName(filePath, true));
                    }
                    catch
                    {
                        filePath = TempFileService.GetRandomTempFileName(filePath, true);
                    }
                }
            } while(!success);

            this.filePath = filePath;
            initialized = true;
		}

		public int MaxFileSize => maxSize;

		public string FilePath => filePath;

		public void LogEntry(ILogEntry e)
		{
			lock(syncObj)
			{
				if(!initialized)
					return;

				writer.Write(e.ToString());
				writer.Flush();
				fs.Flush();
				if(fs.Length > maxSize)
					Shrink(writer, fs, maxSize);
			}
		}

		public void Dispose()
		{
			lock(syncObj)
			{
				if(!initialized)
					return;

				writer.Flush();
				fs.Flush();
				writer.Dispose();
				fs.Dispose();
				initialized = false;
			}
		}


		private readonly static Random rnd = new Random();

		// Shrinks the file to 75% of its current size (by deleting old entries).
		private static void Shrink(StreamWriter writer, FileStream fs, int maxSize)
		{
			long shrinkStartPosition = maxSize / 4;
			fs.Seek(shrinkStartPosition, SeekOrigin.Begin);
			while(fs.ReadByte() != 0x0A) { } // Read ahead until the next line end ('\n')
			shrinkStartPosition = fs.Position;

			byte[] buffer = new byte[4096];
			int bytesRead = 0;
			long position = 0;

			// Move bottom portion of the file to the top
			while((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
			{
				fs.Seek(position, SeekOrigin.Begin);
				fs.Write(buffer, 0, bytesRead);
				position += bytesRead;
				fs.Seek(shrinkStartPosition + position, SeekOrigin.Begin);
			}

			// flush and set file length
			fs.Flush();
			fs.SetLength(position);
		}

		private static void CleanTmpFiles(string directory)
		{
			if(string.IsNullOrWhiteSpace(directory))
				return;

			if(!Directory.Exists(directory))
				return;

			foreach(string targetFile in Directory.GetFiles(directory, "*.tmp"))
			{
				try
				{
					var fi = new FileInfo(targetFile);
					
					if(fi.LastWriteTimeUtc.AddDays(10) < DateTime.UtcNow)
						File.Delete(targetFile);
				}
				catch
				{
					// Empty catch is intended, just keep going
				}
			}
		}

		private static string CreateTmpFileName(string file = "file")
		{
			string result;

			string path = Path.GetDirectoryName(file)
				?? throw new InvalidOperationException($"Could not determine a valid file path for {file}");

			string fileName = Path.GetFileNameWithoutExtension(file);
			do
			{
				result = Path.Combine(path, $"_tmp_{fileName}_{rnd.Next(1, 1000000000):D9}.tmp");
			} while(File.Exists(result));
			return result;
		}
	}
}
