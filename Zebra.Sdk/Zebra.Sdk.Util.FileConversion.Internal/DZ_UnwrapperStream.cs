using System;
using System.IO;
using System.IO.Compression;
using Zebra.Sdk.Device;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class DZ_UnwrapperStream : ZplUnwrapperStreamBase
	{
		private Stream sourceStream;

		protected override PrinterWrappingType TypeToUnwrap
		{
			get
			{
				return PrinterWrappingType.DZ;
			}
		}

		public DZ_UnwrapperStream(Stream sourceStreamArg)
		{
			this.sourceStream = sourceStreamArg;
			if (this.sourceStream == null)
			{
				throw new IOException("input stream is null");
			}
			using (DZ_DataProviderStream dZDataProviderStream = new DZ_DataProviderStream(this.sourceStream))
			{
				if (dZDataProviderStream.DataFormatSpecifier == DataFormatSpecifier.MIME_UNCOMPRESSED)
				{
					using (Stream colonSignifiesEndStream = new ColonSignifiesEndStream(dZDataProviderStream))
					{
						this.dataUnwrapperStream = Base64.ConvertFromBase64(colonSignifiesEndStream);
					}
				}
				else if (dZDataProviderStream.DataFormatSpecifier != DataFormatSpecifier.MIME_COMPRESSED)
				{
					this.dataUnwrapperStream = dZDataProviderStream;
				}
				else
				{
					using (Stream stream = new ColonSignifiesEndStream(dZDataProviderStream))
					{
						this.dataUnwrapperStream = new DeflateStream(Base64.ConvertFromBase64(stream, true), CompressionMode.Decompress);
					}
				}
				try
				{
					PrinterFilePath printerFilePath = FileUtilities.ParseDriveAndExtension(dZDataProviderStream.FilenameOnPrinter);
					this.unwrappedType = PrinterFileType.GetUnwrappedType(printerFilePath.Extension);
					if (printerFilePath.FileName.Length <= 8)
					{
						this.ConsumeShortDzHeader();
					}
					else
					{
						this.ConsumeLargeDzHeader();
					}
				}
				catch (ZebraIllegalArgumentException zebraIllegalArgumentException1)
				{
					ZebraIllegalArgumentException zebraIllegalArgumentException = zebraIllegalArgumentException1;
					throw new IOException(zebraIllegalArgumentException.Message, zebraIllegalArgumentException);
				}
				this.fileNameOnPrinter = dZDataProviderStream.FilenameOnPrinter;
			}
		}

		private void ConsumeLargeDzHeader()
		{
			for (int i = 0; i < 24; i++)
			{
				this.dataUnwrapperStream.ReadByte();
			}
		}

		private void ConsumeShortDzHeader()
		{
			for (int i = 0; i < 16; i++)
			{
				this.dataUnwrapperStream.ReadByte();
			}
		}

		public override int ReadByte()
		{
			int num = base.ReadByte();
			if (num == -1)
			{
				do
				{
					num = this.sourceStream.ReadByte();
				}
				while (num != -1);
			}
			return num;
		}
	}
}