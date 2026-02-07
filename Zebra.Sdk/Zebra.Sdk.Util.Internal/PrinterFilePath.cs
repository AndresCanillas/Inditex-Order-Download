using System;

namespace Zebra.Sdk.Util.Internal
{
	internal class PrinterFilePath
	{
		private string drive;

		private string fileName;

		private string extension;

		public string Drive
		{
			get
			{
				return this.drive;
			}
		}

		public string Extension
		{
			get
			{
				return this.extension;
			}
		}

		public string FileName
		{
			get
			{
				return this.fileName;
			}
		}

		public PrinterFilePath(string drive, string fileName, string extension)
		{
			this.drive = drive;
			this.fileName = fileName;
			this.extension = extension;
		}

		public override string ToString()
		{
			string str;
			string str1;
			str1 = (this.Drive == null || this.Drive.Equals("") ? "" : string.Concat(this.Drive, ":"));
			str = (this.Extension == null || this.Extension.Equals("") ? "" : this.Extension);
			return string.Concat(str1, this.FileName, str);
		}
	}
}