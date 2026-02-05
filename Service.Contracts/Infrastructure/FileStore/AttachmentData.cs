using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Service.Contracts
{
	class AttachmentData : IAttachmentData
	{
		private AttachmentCollection owner;
		private string path;
		private string filePath;
		private string fileName;
		private string metaFilePath;
		private long filesize;
		private DateTime createdDate;
		private DateTime updatedDate;

		public AttachmentData(AttachmentCollection owner, string filePath)
		{
			this.owner = owner;
			this.filePath = filePath;
			path = Path.GetDirectoryName(filePath);
			fileName = Path.GetFileName(filePath);
			if (!File.Exists(filePath))
				File.WriteAllBytes(filePath, new byte[] { });
			var fi = new FileInfo(filePath);
			filesize = fi.Length;
			createdDate = fi.CreationTime;
			updatedDate = fi.LastWriteTime;
			metaFilePath = filePath + "_meta.dat";
		}

		public Guid FileGUID { get => Guid.Empty; } // LocalFileStore does not support this feature

		public string FileName
		{
			get
			{
				return fileName;
			}
			set
			{
				if (String.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException("FileName");
				if (value.Contains("\\"))
					value = Path.GetFileName(value);
				var newFilePath = Path.Combine(path, value);
				if (File.Exists(newFilePath))
					throw new InvalidOperationException("There is already an attachment with the specified file name.");

				var originalFilePath = filePath;
				File.Move(filePath, newFilePath);
				if (File.Exists(metaFilePath))
					File.Move(metaFilePath, newFilePath + "_meta.dat");

				fileName = value;
				filePath = newFilePath;
				metaFilePath = newFilePath + "_meta.dat";
				updatedDate = DateTime.Now;
				owner.UpdateAttachmentName(originalFilePath, newFilePath);
			}
		}


		public long FileSize { get => filesize; }


		public bool HasPhysicalCopy { get => true; }


		public string PhysicalPath { get => filePath; }


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
			if (File.Exists(metaFilePath))
			{
				var json = File.ReadAllText(metaFilePath, Encoding.UTF8).Trim();
				if (json != null && json.Length > 0)
				{
					if (json[0] == 65279)
						json = json.Substring(1);
					var userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
					var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
					if (typeMeta != null)
						return JsonConvert.DeserializeObject<T>(typeMeta.Content);
				}
			}
			return new T();
		}


		public void SetMetadata<T>(T meta) where T : new()
		{
			List<UserMeta> userMeta = new List<UserMeta>();
			if (File.Exists(metaFilePath))
			{
				var json = File.ReadAllText(metaFilePath, Encoding.UTF8).Trim();
				if (json != null && json.Length > 0)
				{
					if (json[0] == 65279)
						json = json.Substring(1);
					userMeta = JsonConvert.DeserializeObject<List<UserMeta>>(json);
				}
			}

			var typeMeta = userMeta.FirstOrDefault(p => p.MetaType == typeof(T).Name);
			if (typeMeta != null)
				typeMeta.Content = JsonConvert.SerializeObject(meta);
			else
				userMeta.Add(new UserMeta() { MetaType = typeof(T).Name, Content = JsonConvert.SerializeObject(meta) });
			File.WriteAllText(metaFilePath, JsonConvert.SerializeObject(userMeta), Encoding.UTF8);
		}


		public byte[] GetMetadataBytes()
		{
			if (File.Exists(metaFilePath))
				return File.ReadAllBytes(metaFilePath);
			else
				return new byte[] { };
		}


		public void SetMetadataBytes(byte[] metadata)
		{
			File.WriteAllBytes(metaFilePath, metadata);
		}


		public Stream GetContentAsStream()
		{
			return FileStoreHelper.GetFileStream(filePath);
		}


		public byte[] GetContentAsBytes()
		{
			byte[] result;
			using (Stream s = FileStoreHelper.GetFileStream(filePath))
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
						using (var dst = File.OpenWrite(filePath))
						{
							dst.SetLength(0L);
							src.CopyTo(dst, 4096);
						}
					}
					updatedDate = DateTime.Now;
					filesize = new FileInfo(filePath).Length;
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
			var tmpFile = filePath + "_upload";
			using (var tmpStream = FileStoreHelper.WaitToWrite(tmpFile))
			{
				tmpStream.SetLength(0L);
				stream.CopyTo(tmpStream, 4096);
				tmpStream.Flush();
				tmpStream.Seek(0, SeekOrigin.Begin);
				using (var dstStream = FileStoreHelper.WaitToWrite(filePath))
				{
					dstStream.SetLength(0L);
					tmpStream.CopyTo(dstStream, 4096);
				}
			}
			FileStoreHelper.TryDeleteFile(tmpFile);
			updatedDate = DateTime.Now;
			filesize = new FileInfo(filePath).Length;
		}


		public void SetContent(byte[] fileContent)
		{
			using (var dst = FileStoreHelper.WaitToWrite(filePath))
			{
				dst.SetLength(0L);
				dst.Write(fileContent, 0, fileContent.Length);
			};
			updatedDate = DateTime.Now;
			filesize = new FileInfo(filePath).Length;
		}


		public void Copy(IFSFile source)
		{
			using (var stream = source.GetContentAsStream())
			{
				this.SetContent(stream);
			}
		}


		public void Delete()
		{
			if (File.Exists(filePath))
				File.Delete(filePath);
			if (File.Exists(filePath + "_meta.dat"))
				File.Delete(filePath + "_meta.dat");
			owner.removeAttachment(filePath);
		}
	}
}
