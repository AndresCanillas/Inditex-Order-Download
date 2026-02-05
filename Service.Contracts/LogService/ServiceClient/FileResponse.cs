using System;
using System.IO;

namespace Services.Core
{
	public sealed class FileResponse : IDisposable
	{
		private readonly string backingFile;

		public FileResponse() { }

		public FileResponse(string backingFile, string fileName, Stream fileContent)
		{
			this.backingFile = backingFile;
			FileName = Path.GetFileName(fileName);
			FileContent = fileContent;
		}

		public void Dispose()
		{
			FileContent?.Dispose();
			try
			{
				if(backingFile == null)
					return;
				if(File.Exists(backingFile))
					File.Delete(backingFile);
			}
			catch { } // empty catch intended, we are just cleaning up, ignore errors
		}

		public string FileName { get; set; }

		public Stream FileContent { get; set; }

		public void Validate()
		{
			if(FileName == null)
				throw new InvalidOperationException("File upload request is invalid: Missing FileName");
			if(FileContent == null)
				throw new InvalidOperationException("File upload request is invalid: Missing FileContent");
		}

		public bool IsValid() =>
			FileName != null && FileContent != null;
	}
}
