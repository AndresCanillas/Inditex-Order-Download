using System;
using System.IO;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal abstract class ZplUnwrapperStreamBase : StreamDecoratorBase, IDisposable
	{
		protected Stream dataUnwrapperStream;

		protected string fileNameOnPrinter;

		private long unmimedFileSize;

		private ushort crc16;

		private ushort sum;

		protected PrinterFileType unwrappedType;

		protected abstract PrinterWrappingType TypeToUnwrap
		{
			get;
		}

		public ZplUnwrapperStreamBase()
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (this.dataUnwrapperStream != null)
			{
				this.dataUnwrapperStream.Dispose();
			}
			base.Dispose(disposing);
		}

		public override PrinterFileMetadata GetPrinterFileMetadata()
		{
			string str = string.Format("{0:X4}", this.crc16);
			string str1 = string.Format("{0:X4}", (this.sum ^ 65535) + 1);
			return new PrinterFileMetadata(this.unmimedFileSize, str, str1, this.fileNameOnPrinter)
			{
				PrinterFileType = this.unwrappedType,
				PrinterWrappingType = this.TypeToUnwrap
			};
		}

		public override int ReadByte()
		{
			int num = this.dataUnwrapperStream.ReadByte();
			if (num != -1)
			{
				this.unmimedFileSize += (long)1;
				this.sum = (ushort)(this.sum + (ushort)num);
				this.crc16 = ZCRC16.AddCrc16Byte_cpcl(this.crc16, num);
			}
			return num;
		}
	}
}