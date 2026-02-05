using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Service.Contracts
{
	class AttachmentCollection: IAttachmentCollection
	{
		private string category;
		private string path;
		private List<string> files;

		public AttachmentCollection(string category, string path)
		{
			this.category = category;
			this.path = path;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			files = new List<string>();
			foreach(var filename in Directory.GetFiles(path))
			{
				if (filename.EndsWith("_meta.dat") || filename.EndsWith(".ver"))
					continue;
				if (filename.StartsWith("Thumbs.db"))
					continue;
				files.Add(filename);
			}
		}


		public string CategoryName { get => category; }


		public bool HasPhysicalCopy { get => true; }


		public string PhysicalPath { get => path; }


		public int Count
		{
			get { return files.Count; }
		}


		public IAttachmentData this[int index]
		{
			get
			{
				var file = files[index];
				return new AttachmentData(this, file);
			}
		}


		public bool TryGetAttachment(string fileName, out IAttachmentData attachment)
		{
			attachment = null;
			if (fileName.Contains("\\"))
				fileName = Path.GetFileName(fileName);
			string filePath = Path.Combine(path, fileName);
			var existingFile = files.SingleOrDefault(p => p == filePath);
			if (existingFile != null)
			{
				attachment = new AttachmentData(this, filePath);
				return true;
			}
			else
			{
				return false;
			}
		}


		public IAttachmentData GetOrCreateAttachment(string fileName)
		{
			if (fileName.Contains("\\"))
				fileName = Path.GetFileName(fileName);
			string filePath = Path.Combine(path, fileName);
			var existingFile = files.SingleOrDefault(p => p == filePath);
			if (existingFile == null)
			{
				using (var fs = File.Create(filePath)) { }
			}

             return new AttachmentData(this, filePath);
		}


		public IAttachmentData CreateAttachment(string fileName)
		{
			if (fileName.Contains("\\"))
				fileName = Path.GetFileName(fileName);
			string filePath = Path.Combine(path, fileName);
			files.Add(filePath);
			return new AttachmentData(this, filePath);
		}


		public void DeleteAttachment(IAttachmentData attachment)
		{
			string filePath = Path.Combine(path, attachment.FileName);
			var index = files.FindIndex(p => p == filePath);
			if (index >= 0)
				attachment.Delete();
			else
				throw new InvalidOperationException("Could not find the specified attachment.");
		}


		internal void removeAttachment(string filePath)
		{
			var index = files.FindIndex(p => p == filePath);
			if (index >= 0)
				files.RemoveAt(index);
		}


		public void Clear()
		{
			foreach(string file in Directory.GetFiles(path))
				File.Delete(file);
			files.Clear();
		}


		public IEnumerator<IAttachmentData> GetEnumerator()
		{
			foreach(string file in files)
			{
				yield return new AttachmentData(this, file);
			}
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		internal void UpdateAttachmentName(string originalFilePath, string newFilePath)
		{
			int index = files.FindIndex(p => p == originalFilePath);
			if(index >= 0)
				files[index] = newFilePath;
		}
	}
}
