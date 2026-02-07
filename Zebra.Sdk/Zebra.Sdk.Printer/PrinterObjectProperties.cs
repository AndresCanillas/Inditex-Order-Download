using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Container for properties of a printer object.
	///       </summary>
	public abstract class PrinterObjectProperties
	{
		protected string drivePrefix;

		protected string fileName;

		protected string extension;

		protected long crc32;

		protected long fileSize;

		/// <summary>
		///       Gets/sets the 32-bit CRC value of the file.
		///       </summary>
		public long CRC32
		{
			get
			{
				return this.crc32;
			}
			set
			{
				this.crc32 = value;
			}
		}

		/// <summary>
		///       Gets/sets the drive prefix with the trailing colon.
		///       </summary>
		public string DrivePrefix
		{
			get
			{
				return this.drivePrefix;
			}
			set
			{
				this.drivePrefix = value;
			}
		}

		/// <summary>
		///       Gets/sets the file extension.
		///       </summary>
		public string Extension
		{
			get
			{
				return this.extension;
			}
			set
			{
				this.extension = value;
			}
		}

		/// <summary>
		///       Gets/sets the file name.
		///       </summary>
		public string FileName
		{
			get
			{
				return this.fileName;
			}
			set
			{
				this.fileName = value;
			}
		}

		/// <summary>
		///       Gets/sets the size of the file in bytes
		///       </summary>
		public long FileSize
		{
			get
			{
				return this.fileSize;
			}
			set
			{
				this.fileSize = value;
			}
		}

		/// <summary>
		///       Gets the full name of the file on the printer.
		///       </summary>
		public string FullName
		{
			get
			{
				return string.Concat(this.drivePrefix, this.fileName, ".", this.extension);
			}
		}

		protected PrinterObjectProperties()
		{
		}
	}
}