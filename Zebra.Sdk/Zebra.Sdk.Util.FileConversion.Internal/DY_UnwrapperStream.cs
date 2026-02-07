using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class DY_UnwrapperStream : ZplUnwrapperStreamBase
	{
		private DY_UnwrapperStream.DY_SourceDataType dy_SourceDataType;

		protected override PrinterWrappingType TypeToUnwrap
		{
			get
			{
				return PrinterWrappingType.DY;
			}
		}

		public DY_UnwrapperStream(Stream sourceStream)
		{
			if (sourceStream == null)
			{
				throw new IOException("input stream is null");
			}
			using (DYDataProviderStream dYDataProviderStream = new DYDataProviderStream(sourceStream))
			{
				this.unwrappedType = PrinterFileType.GetUnwrappedType(string.Concat("~DY_", dYDataProviderStream.FileExtensionCode));
				this.dy_SourceDataType = DY_UnwrapperStream.DY_SourceDataType.GetSourceType(dYDataProviderStream.FormatDownloadedInDataField);
				Stream dataDecodingStream = this.GetDataDecodingStream(dYDataProviderStream);
				this.dataUnwrapperStream = dataDecodingStream;
				if (this.ShouldPrependZebraImageHeader())
				{
					this.dataUnwrapperStream = this.GetImageStream(dataDecodingStream, dYDataProviderStream.GetBytesPerRow(), dYDataProviderStream.GetTotalBytesInData());
				}
				this.fileNameOnPrinter = dYDataProviderStream.FilenameOnPrinter;
			}
		}

		private Stream GetDataDecodingStream(DYDataProviderStream dyDataProviderStream)
		{
			Stream deflateStream = dyDataProviderStream;
			if (dyDataProviderStream.DataFormatSpecifier == DataFormatSpecifier.MIME_UNCOMPRESSED)
			{
				using (Stream colonSignifiesEndStream = new ColonSignifiesEndStream(dyDataProviderStream))
				{
					deflateStream = Base64.ConvertFromBase64(colonSignifiesEndStream);
				}
			}
			else if (dyDataProviderStream.DataFormatSpecifier == DataFormatSpecifier.MIME_COMPRESSED)
			{
				using (Stream stream = new ColonSignifiesEndStream(dyDataProviderStream))
				{
					deflateStream = new DeflateStream(Base64.ConvertFromBase64(stream, true), CompressionMode.Decompress);
				}
			}
			else if (dyDataProviderStream.DataFormatSpecifier == DataFormatSpecifier.ASCII_HEX)
			{
				deflateStream = new AsciiDecoderStream(dyDataProviderStream, dyDataProviderStream.GetBytesPerRow());
			}
			return deflateStream;
		}

		private Stream GetImageStream(Stream dataDecodingStream, int bytesPerRow, int totalBytesInData)
		{
			Stream zebraImageHeaderPrependerStream = new ZebraImageHeaderPrependerStream(dataDecodingStream, bytesPerRow, totalBytesInData);
			Stream grfToPrinterPngConverterStream = zebraImageHeaderPrependerStream;
			if (this.ShouldConvertFromGrfToPng())
			{
				grfToPrinterPngConverterStream = new GrfToPrinterPngConverterStream(zebraImageHeaderPrependerStream);
			}
			else if (this.ShouldConvertFromPngToGrf())
			{
				grfToPrinterPngConverterStream = new PrinterPngToGrfConverterStream(zebraImageHeaderPrependerStream);
			}
			return grfToPrinterPngConverterStream;
		}

		private bool ShouldConvertFromGrfToPng()
		{
			if (this.dy_SourceDataType != DY_UnwrapperStream.DY_SourceDataType.UNCOMPRESSED)
			{
				return false;
			}
			return this.unwrappedType == PrinterFileType.PRINTER_PNG;
		}

		private bool ShouldConvertFromPngToGrf()
		{
			if (this.dy_SourceDataType != DY_UnwrapperStream.DY_SourceDataType.PNG)
			{
				return false;
			}
			return this.unwrappedType == PrinterFileType.PRINTER_GRF;
		}

		private bool ShouldPrependZebraImageHeader()
		{
			if (this.unwrappedType == PrinterFileType.PRINTER_GRF)
			{
				return true;
			}
			return this.unwrappedType == PrinterFileType.PRINTER_PNG;
		}

		internal class DY_SourceDataType
		{
			internal static DY_UnwrapperStream.DY_SourceDataType UNCOMPRESSED;

			internal static DY_UnwrapperStream.DY_SourceDataType BINARY;

			internal static DY_UnwrapperStream.DY_SourceDataType PNG;

			private string formatDownloadedInDataField;

			private static List<DY_UnwrapperStream.DY_SourceDataType> sourceDataTypes;

			static DY_SourceDataType()
			{
				DY_UnwrapperStream.DY_SourceDataType.UNCOMPRESSED = new DY_UnwrapperStream.DY_SourceDataType("A");
				DY_UnwrapperStream.DY_SourceDataType.BINARY = new DY_UnwrapperStream.DY_SourceDataType("B");
				DY_UnwrapperStream.DY_SourceDataType.PNG = new DY_UnwrapperStream.DY_SourceDataType("P");
				DY_UnwrapperStream.DY_SourceDataType.sourceDataTypes = new List<DY_UnwrapperStream.DY_SourceDataType>()
				{
					DY_UnwrapperStream.DY_SourceDataType.UNCOMPRESSED,
					DY_UnwrapperStream.DY_SourceDataType.BINARY,
					DY_UnwrapperStream.DY_SourceDataType.PNG
				};
			}

			private DY_SourceDataType(string formatDownloadedInDataField)
			{
				this.formatDownloadedInDataField = formatDownloadedInDataField;
			}

			internal static DY_UnwrapperStream.DY_SourceDataType GetSourceType(string letterCode)
			{
				DY_UnwrapperStream.DY_SourceDataType dYSourceDataType;
				List<DY_UnwrapperStream.DY_SourceDataType>.Enumerator enumerator = DY_UnwrapperStream.DY_SourceDataType.sourceDataTypes.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						DY_UnwrapperStream.DY_SourceDataType current = enumerator.Current;
						if (!Regex.IsMatch(letterCode, current.formatDownloadedInDataField, RegexOptions.IgnoreCase))
						{
							continue;
						}
						dYSourceDataType = current;
						return dYSourceDataType;
					}
					throw new IOException("Invalid ~DY Header  --  Missing Format Downloaded In Data Field parameter");
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
			}
		}
	}
}