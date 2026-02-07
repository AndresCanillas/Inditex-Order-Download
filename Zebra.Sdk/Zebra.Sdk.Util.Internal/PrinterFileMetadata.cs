using System;
using System.Globalization;
using System.IO;
using Zebra.Sdk.Util.FileConversion.Internal;

namespace Zebra.Sdk.Util.Internal
{
	internal class PrinterFileMetadata
	{
		private long fileSize;

		private string crc16;

		private string checkSum;

		protected string fileName;

		private Zebra.Sdk.Util.FileConversion.Internal.PrinterWrappingType printerWrappingType;

		private Zebra.Sdk.Util.FileConversion.Internal.PrinterFileType printerFileType = Zebra.Sdk.Util.FileConversion.Internal.PrinterFileType.UNSUPPORTED;

		public string CheckSum
		{
			get
			{
				return this.checkSum;
			}
		}

		public string Crc16
		{
			get
			{
				return this.crc16;
			}
		}

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

		public long FileSize
		{
			get
			{
				return this.fileSize;
			}
		}

		public Zebra.Sdk.Util.FileConversion.Internal.PrinterFileType PrinterFileType
		{
			get
			{
				return this.printerFileType;
			}
			set
			{
				this.printerFileType = value;
			}
		}

		public Zebra.Sdk.Util.FileConversion.Internal.PrinterWrappingType PrinterWrappingType
		{
			get
			{
				return this.printerWrappingType;
			}
			set
			{
				this.printerWrappingType = value;
			}
		}

		public PrinterFileMetadata(long fileSize, string crc16, string checkSum)
		{
			this.Init(fileSize, crc16, checkSum, null);
		}

		public PrinterFileMetadata(long fileSize, string crc16, string checkSum, string fileName)
		{
			this.Init(fileSize, crc16, checkSum, fileName);
		}

		public PrinterFileMetadata(Stream contentStream)
		{
			ushort num = 0;
			ushort num1 = 0;
			long num2 = (long)0;
			for (int i = contentStream.ReadByte(); i != -1; i = contentStream.ReadByte())
			{
				num1 = (ushort)(num1 + (ushort)i);
				num = ZCRC16.AddCrc16Byte_cpcl(num, i);
				num2 += (long)1;
			}
			string str = string.Format("{0:X4}", num);
			string str1 = string.Format("{0:X4}", (num1 ^ 65535) + 1);
			this.Init(num2, str, str1, null);
		}

		private void Init(long fileSize, string crc16, string checkSum, string fileName)
		{
			if (fileSize <= (long)0 || this.IsInvalid16BitHex(crc16) || this.IsInvalid16BitHex(checkSum))
			{
				throw new ArgumentException("Could not instantiate a valid file metadata");
			}
			this.fileSize = fileSize;
			this.crc16 = crc16;
			this.checkSum = checkSum;
			this.fileName = fileName;
		}

		private bool IsInvalid16BitHex(string str)
		{
			try
			{
				int.Parse(str, NumberStyles.HexNumber);
				if (str.Length == 4)
				{
					return false;
				}
			}
			catch (Exception)
			{
			}
			return true;
		}
	}
}