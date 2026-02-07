using System;
using System.Globalization;
using System.IO;
using System.Text;
using Zebra.Sdk.Device;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class CisdfUnwrapperStream : StreamDecoratorBase
	{
		private PrinterFileMetadata printerFileMetadata;

		private long bytesLeftToRead;

		private Stream sourceStream;

		public CisdfUnwrapperStream(Stream sourceStream)
		{
			try
			{
				Stream stream = sourceStream;
				if (stream == null)
				{
					throw new IOException("Missing CISDF Header : input stream is null");
				}
				this.sourceStream = stream;
				this.SkipToHeaderInfo();
				string str = this.GrabNextCisdfLine();
				string str1 = this.GrabNextCisdfLine();
				int num = int.Parse(this.GrabNextCisdfLine(), NumberStyles.HexNumber);
				string str2 = this.GrabNextCisdfLine();
				this.printerFileMetadata = new PrinterFileMetadata((long)num, str, str2, str1)
				{
					PrinterFileType = this.GetFileType(str1),
					PrinterWrappingType = PrinterWrappingType.CISDF
				};
				this.bytesLeftToRead = (long)num;
			}
			catch (FormatException formatException1)
			{
				FormatException formatException = formatException1;
				throw new ArgumentException(formatException.Message, formatException);
			}
			catch (OverflowException overflowException1)
			{
				OverflowException overflowException = overflowException1;
				throw new IOException(overflowException.Message, overflowException);
			}
		}

		private PrinterFileType GetFileType(string fileName)
		{
			PrinterFileType uNSUPPORTED = PrinterFileType.UNSUPPORTED;
			try
			{
				uNSUPPORTED = PrinterFileType.GetUnwrappedType(FileUtilities.ParseDriveAndExtension(fileName).Extension);
			}
			catch (ZebraIllegalArgumentException)
			{
				uNSUPPORTED = PrinterFileType.UNSUPPORTED;
			}
			return uNSUPPORTED;
		}

		public override PrinterFileMetadata GetPrinterFileMetadata()
		{
			return this.printerFileMetadata;
		}

		private string GrabNextCisdfLine()
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = this.sourceStream.ReadByte();
			while (num != -1)
			{
				if (char.IsWhiteSpace((char)num))
				{
					num = this.sourceStream.ReadByte();
				}
				else
				{
					break;
				}
			}
			while (num != -1 && !this.IsEndOfLineCharacter(num))
			{
				stringBuilder.Append((char)num);
				num = this.sourceStream.ReadByte();
			}
			num = this.sourceStream.ReadByte();
			if (!this.IsEndOfLineCharacter(num) && this.sourceStream.Position > (long)0)
			{
				this.sourceStream.Seek((long)-1, SeekOrigin.Current);
			}
			if (stringBuilder.Length == 0)
			{
				throw new IOException("Invalid CISDF Header");
			}
			return stringBuilder.ToString().Trim();
		}

		private bool IsEndOfLineCharacter(int b)
		{
			if (b == 10)
			{
				return true;
			}
			return b == 13;
		}

		public override int ReadByte()
		{
			if (this.bytesLeftToRead <= (long)0)
			{
				return -1;
			}
			int num = -1;
			try
			{
				num = this.sourceStream.ReadByte();
			}
			catch (EndOfStreamException)
			{
			}
			if (num < 0)
			{
				throw new IOException("Expected more data");
			}
			this.bytesLeftToRead -= (long)1;
			return num;
		}

		private void SkipToHeaderInfo()
		{
			string str = this.GrabNextCisdfLine();
			while (string.IsNullOrEmpty(str))
			{
				str = this.GrabNextCisdfLine();
			}
			if (!str.StartsWith("! CISDFCRC16") && !str.StartsWith("! CISDFRCRC16"))
			{
				throw new IOException(string.Concat("Invalid CISDF Header : \"", str, "\" does not start with \"! CISDFCRC16\" or \"! CISDFRCRC16\"."));
			}
		}
	}
}