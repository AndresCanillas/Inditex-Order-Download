using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Contracts
{
	class FileData : IFileData
	{
		private int id;
		private string path;
		private string fileName;
		private string fullPath;
		private string[] categories;
		private long fileSize;
		private DateTime createdDate;
		private DateTime updatedDate;
		private string metaFile;
		private Dictionary<string, IAttachmentCollection> attachments = new Dictionary<string, IAttachmentCollection>();


		public FileData(int id, string path, string fileName, string[] categories)
		{
			if (id < 0)
				throw new InvalidOperationException("argument id cannot be negative");

			if (String.IsNullOrWhiteSpace(path))
				throw new InvalidOperationException("argument path cannot be null or empty");

			if (String.IsNullOrWhiteSpace(fileName))
				throw new InvalidOperationException("Argument filename cannot be null or empty");

			if (categories == null)
				categories = new string[0];

			foreach (var cat in categories)
			{
				if (String.IsNullOrWhiteSpace(cat))
					throw new InvalidOperationException("Argument categories cannot contain null or empty values");
			}

			this.id = id;
			this.path = path;
			this.fileName = fileName;
			this.fullPath = Path.Combine(path, fileName);
			this.categories = categories;

			metaFile = Path.Combine(path, "_meta.dat");

			var info = new FileInfo(fullPath);
			fileSize = info.Length;
			createdDate = info.CreationTime;
			updatedDate = info.LastWriteTime;
		}


		internal static IFileData ValidateFile(int id, string path, string[] categories)
		{
			var metaFile = Path.Combine(path, "_meta.dat");
			try
			{
				var filemeta = GetMetadata<FileMeta>(metaFile);
				if (filemeta == null || String.IsNullOrWhiteSpace(filemeta.FileName))
					return null;

				var fullPath = Path.Combine(path, filemeta.FileName);
				if (!File.Exists(fullPath))
				{
					using (var sx = FileStoreHelper.WaitToWrite(fullPath))
					{
						// File does not exist, create an empty file
					}
				}

				return new FileData(id, path, filemeta.FileName, categories);
			}
			catch
			{
				return null;
			}
		}


		internal static IFileData CreateFile(int id, string path, string fileName, string[] categories)
		{
			var metaFile = Path.Combine(path, "_meta.dat");
			using (var s = FileStoreHelper.WaitToWrite(metaFile))
			{
				using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8))
				{
					var fileMeta = new FileMeta() { FileName = fileName, Version = 1 };
					var userMeta = new List<UserMeta>() {
						new UserMeta() {
							MetaType = typeof(FileMeta).Name,
							Content = JsonConvert.SerializeObject(fileMeta)
						}
					};
					sw.Write(JsonConvert.SerializeObject(userMeta));

					var fullPath = Path.Combine(path, fileName);
					if (!File.Exists(fullPath))
						using (var sx = FileStoreHelper.WaitToWrite(fullPath)) { }  // File does not exist, create an empty file
				}
			};

			return new FileData(id, path, fileName, categories);
		}


		public Guid FileGUID { get => Guid.Empty; }  // LocalFileStore does not support this feature


		public int FileID
		{
			get { return id; }
		}


		public string FileName
		{
			get { return fileName; }
			set
			{
				if (value.Contains("\\"))
					value = Path.GetFileName(value);
				var newFilePath = Path.Combine(path, value);
				File.Move(fullPath, newFilePath);
				fileName = value;
				fullPath = newFilePath;
			}
		}


		public long FileSize
		{
			get { return fileSize; }
		}


		public bool HasPhysicalCopy { get => true; }


		public string PhysicalPath { get => fullPath; }


		public DateTime CreatedDate
		{
			get { return createdDate; }
		}


		public DateTime UpdatedDate
		{
			get { return updatedDate; }
		}


		public T GetMetadata<T>() where T : new()
		{
			return GetMetadata<T>(metaFile);
		}


		private static T GetMetadata<T>(string metaFilePath) where T : new()
		{
			if (File.Exists(metaFilePath))
			{
				string json = null;

				using (var src = FileStoreHelper.WaitToRead(metaFilePath))
				{
					using (StreamReader sr = new StreamReader(src, Encoding.UTF8))
					{
						json = sr.ReadToEnd();
					}
				};

				if (json != null && json.Length > 0)
				{
					json = json.Trim();
					if (json[0] == 65279)
						json = json.Substring(1);
					if (json.StartsWith("["))
					{
						var userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
						var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
						if (typeMeta != null)
							return JsonConvert.DeserializeObject<T>(typeMeta.Content);
					}
				}
			}
			return new T();
		}


		public byte[] GetMetadataBytes()
		{
			if (File.Exists(metaFile))
				return File.ReadAllBytes(metaFile);
			else
				return null;
		}


		public void SetMetadataBytes(byte[] metadata)
		{
			File.WriteAllBytes(metaFile, metadata);
		}


		private FileMeta CreateFileMeta(string path)
		{
			var meta = new FileMeta();
			metaFile = Path.Combine(path, "_meta.dat");
			foreach (var file in System.IO.Directory.GetFiles(path))
			{
				var filename = Path.GetFileName(file);
				if (filename == "_content.dat") continue;
				if (filename == "_meta.dat") continue;
				if (filename.StartsWith("_v") && filename.EndsWith(".dat")) continue;
				meta.FileName = fileName;
				meta.Version = 1;
				SetMetadata(meta);
				return meta;
			}
			meta.FileName = "container";
			meta.Version = 1;
			SetMetadata(meta);
			return meta;
		}


		public void SetMetadata<T>(T meta) where T : new()
		{
			List<UserMeta> userMeta = new List<UserMeta>();
			if (File.Exists(metaFile))
			{
				try
				{
					var json = File.ReadAllText(metaFile, Encoding.UTF8).Trim();
					if (json != null && json.Length > 0)
					{
						if (json[0] == 65279)
							json = json.Substring(1);
						userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
					}
				}
				catch { }
			}

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				typeMeta.Content = JsonConvert.SerializeObject(meta);
			else
				userMeta.Add(new UserMeta() { MetaType = typeof(T).Name, Content = JsonConvert.SerializeObject(meta) });

			File.WriteAllText(metaFile, JsonConvert.SerializeObject(userMeta), Encoding.UTF8);
		}


		public Stream GetContentAsStream()
		{
			return FileStoreHelper.GetFileStream(fullPath);
		}


		public byte[] GetContentAsBytes()
		{
			byte[] result;
			using (Stream s = FileStoreHelper.GetFileStream(fullPath))
			{
				using (MemoryStream ms = new MemoryStream((int)s.Length))
				{
					s.CopyTo(ms, 4096);
					result = ms.ToArray();
				}
			}
			return result;
		}


		public void SetContent(string sourceFile)
		{
			Exception lastEx = null;
			int retryCount = 0;
			bool success = false;
			do
			{
				try
				{
					using (var src = File.OpenRead(sourceFile))
					{
						using (var dst = File.OpenWrite(fullPath))
						{
							dst.SetLength(0L);
							src.CopyTo(dst, 4096);
						}
					}
					updatedDate = DateTime.Now;
					fileSize = new FileInfo(fullPath).Length;
					success = true;
				}
				catch (IOException ex)
				{
					lastEx = ex;
					Thread.Sleep(500);
				}
			} while (!success && retryCount++ < 3);
			if (!success && lastEx != null)
				throw lastEx;
		}


		public void SetContent(Stream stream)
		{
			// Note: In this case we first copy the stream to a temporary file as it is likely the stream is a
			// remote file transfer that might get interrupted mid way...  IF we dont first copy to a temporal file
			// we can easily end up corrupting the target file with an incomplete upload.
			var tmpFile = fullPath + "_upload";
			using (var tmpStream = FileStoreHelper.WaitToWrite(tmpFile))
			{
				tmpStream.SetLength(0L);
				stream.CopyTo(tmpStream, 4096);
				tmpStream.Flush();
				tmpStream.Seek(0, SeekOrigin.Begin);
				using(var dstStream = FileStoreHelper.WaitToWrite(fullPath))
				{
					dstStream.SetLength(0L);
					tmpStream.CopyTo(dstStream, 4096);
				}
			}
			FileStoreHelper.TryDeleteFile(tmpFile);
			updatedDate = DateTime.Now;
			var fi = new FileInfo(fullPath);
			fileSize = fi.Length;
		}


		public void SetContent(byte[] fileContent)
		{
			using (var dst = FileStoreHelper.WaitToWrite(fullPath))
			{
				dst.SetLength(0L);
				dst.Write(fileContent, 0, fileContent.Length);
			};
			updatedDate = DateTime.Now;
			fileSize = new FileInfo(fullPath).Length;
		}


		public void Copy(IFSFile source)
		{
			using(var stream = source.GetContentAsStream())
			{
				this.SetContent(stream);
			}
		}


		public IEnumerable<string> AttachmentCategories
		{
			get
			{
				return categories;
			}
		}


		public IAttachmentCollection GetAttachmentCategory(string category)
		{
			IAttachmentCollection result;
			var cat = categories.FirstOrDefault(p => p == category);
			if (cat != null)
			{
				if (!attachments.TryGetValue(category, out result))
				{
					result = new AttachmentCollection(category, Path.Combine(path, category));
					attachments.Add(category, result);
				}
				return result;
			}
			else throw new InvalidOperationException($"Category {category} is not configured for this file store.");
		}


		public IDisposable AcquireExclusiveLock()
		{
			return SharedLock.Acquire(id.ToString());
		}


		public void Delete()
		{
			if (Directory.Exists(path))
				FileStoreHelper.DeleteDirectory(path, true);
		}
	}


	class FileMeta
	{
		public string FileName { get; set; }
		public int Version { get; set; }
	}

	class UserMeta
	{
		public string MetaType { get; set; }
		public string Content { get; set; }
	}
}
