using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Printer.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class MetadataExtractorPassthruStream : StreamDecoratorBase, IDisposable
	{
		private Stream sourceStream;

		private ushort crc16;

		private ushort sum;

		private long numberOfBytes;

		private MemoryStream headerBuffer = new MemoryStream();

		public MetadataExtractorPassthruStream(Stream sourceStream)
		{
			Stream stream = sourceStream;
			if (stream == null)
			{
				throw new IOException("Input stream is null");
			}
			this.sourceStream = stream;
			this.crc16 = 0;
			this.sum = 0;
		}

		private void CheckForHeader(int byteRead)
		{
			if (this.headerBuffer.Length < (long)200 && (!char.IsWhiteSpace((char)byteRead) || this.headerBuffer.Length != 0))
			{
				this.headerBuffer.WriteByte((byte)byteRead);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (this.headerBuffer != null)
			{
				this.headerBuffer.Dispose();
			}
			base.Dispose(disposing);
		}

		private string ExtractFWVersionFromDCHeader(string header)
		{
			string str;
			using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(header)))
			{
				str = FirmwareUtil.ExtractFirmwareVersion(memoryStream);
			}
			return str;
		}

		public override PrinterFileMetadata GetPrinterFileMetadata()
		{
			string str = string.Format("{0:X4}", (int)this.crc16);
			string str1 = string.Format("{0:X4}", (this.sum ^ 65535) + 1);
			PrinterFileMetadata printerFileMetadatum = new PrinterFileMetadata(this.numberOfBytes, str, str1);
			string str2 = Encoding.UTF8.GetString(this.headerBuffer.ToArray());
			printerFileMetadatum.PrinterWrappingType = this.GetTypeToUnwrap(str2);
			if (printerFileMetadatum.PrinterWrappingType == PrinterWrappingType.DC)
			{
				printerFileMetadatum.FileName = this.ExtractFWVersionFromDCHeader(str2);
				printerFileMetadatum.PrinterFileType = PrinterFileType.FIRMWARE;
			}
			return printerFileMetadatum;
		}

		private PrinterWrappingType GetTypeToUnwrap(string potentialHeader)
		{
			PrinterWrappingType printerWrappingType = PrinterWrappingType.UNSUPPORTED;
			string str = potentialHeader.ToUpper().Replace(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX, "~").Trim();
			if (str.StartsWith("~DG"))
			{
				printerWrappingType = PrinterWrappingType.DG;
			}
			else if (str.StartsWith("~DY"))
			{
				printerWrappingType = PrinterWrappingType.DY;
			}
			else if (str.StartsWith("~DZ"))
			{
				printerWrappingType = PrinterWrappingType.DZ;
			}
			else if (str.StartsWith("! CISDFCRC16"))
			{
				printerWrappingType = PrinterWrappingType.CISDF;
			}
			else if (str.StartsWith("! CISDFRCRC16"))
			{
				printerWrappingType = PrinterWrappingType.CISDF;
			}
			else if (str.Contains("~DC"))
			{
				printerWrappingType = PrinterWrappingType.DC;
			}
			else if (str.Contains("<ZEBRA-OBJECT>"))
			{
				printerWrappingType = PrinterWrappingType.HZO;
			}
			else if (str.StartsWith("--") && str.Contains("CONTENT-DISPOSITION:"))
			{
				printerWrappingType = PrinterWrappingType.MPF;
			}
			return printerWrappingType;
		}

		public override int ReadByte()
		{
			int num = this.sourceStream.ReadByte();
			if (num > -1)
			{
				this.CheckForHeader(num);
				this.numberOfBytes += (long)1;
				this.sum = (ushort)(this.sum + (ushort)num);
				this.crc16 = ZCRC16.AddCrc16Byte_cpcl(this.crc16, num);
			}
			return num;
		}
	}
}