using System;
using System.IO;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class PrinterFileUnwrapperStream : StreamDecoratorBase, IDisposable
	{
		private StreamDecoratorBase baseStream;

		private PrinterWrappingType fileType;

		public PrinterFileUnwrapperStream(Stream inputStream, PrinterWrappingType fileType)
		{
			if (inputStream == null)
			{
				throw new IOException("Input stream is null");
			}
			this.fileType = fileType;
			switch (fileType)
			{
				case PrinterWrappingType.CISDF:
				{
					this.baseStream = new CisdfUnwrapperStream(inputStream);
					return;
				}
				case PrinterWrappingType.DY:
				{
					this.baseStream = new DY_UnwrapperStream(inputStream);
					return;
				}
				case PrinterWrappingType.DG:
				{
					this.baseStream = new DY_UnwrapperStream(new DG_ToDyConverterStream(inputStream));
					return;
				}
				case PrinterWrappingType.DZ:
				{
					this.baseStream = new DZ_UnwrapperStream(inputStream);
					return;
				}
				case PrinterWrappingType.DC:
				{
					this.baseStream = new MetadataExtractorPassthruStream(inputStream);
					return;
				}
				case PrinterWrappingType.HZO:
				{
					this.baseStream = new DZ_UnwrapperStream(new HzoToDzConverterStream(inputStream));
					return;
				}
				case PrinterWrappingType.MPF:
				{
					this.baseStream = new MPF_UnwrapperStream(inputStream);
					return;
				}
				default:
				{
					this.baseStream = new MetadataExtractorPassthruStream(inputStream);
					return;
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (this.baseStream != null)
			{
				this.baseStream.Dispose();
			}
			base.Dispose(disposing);
		}

		public override PrinterFileMetadata GetPrinterFileMetadata()
		{
			PrinterFileMetadata printerFileMetadata = this.baseStream.GetPrinterFileMetadata();
			printerFileMetadata.PrinterWrappingType = this.fileType;
			return printerFileMetadata;
		}

		public override int ReadByte()
		{
			return this.baseStream.ReadByte();
		}
	}
}