using System;
using System.IO;
using System.Text;

namespace Zebra.Sdk.Util.Internal
{
	internal class FontConverterHelper
	{
		public FontConverterHelper()
		{
		}

		public static long CalculateStreamSize(Stream sourceFileStream)
		{
			return sourceFileStream.Length;
		}

		public static Stream GetFontHeader(Stream sourceFileStream, string pathOnPrinter, char linkedFontFlagForHeader)
		{
			Stream fontHeader;
			try
			{
				fontHeader = FontConverterHelper.GetFontHeader((long)((int)FontConverterHelper.CalculateStreamSize(sourceFileStream)), pathOnPrinter, linkedFontFlagForHeader);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new Exception(oException.Message, oException);
			}
			return fontHeader;
		}

		public static Stream GetFontHeader(long size, string pathOnPrinter, char linkedFontFlagForHeader)
		{
			BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());
			try
			{
				string str = string.Concat(size);
				binaryWriter.Write(ZPLUtilities.ZPL_INTERNAL_COMMAND_PREFIX_CHAR);
				binaryWriter.Write(Encoding.UTF8.GetBytes("DY"));
				binaryWriter.Write(Encoding.UTF8.GetBytes(pathOnPrinter));
				binaryWriter.Write(ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR);
				binaryWriter.Write(Convert.ToByte('b'));
				binaryWriter.Write(ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR);
				binaryWriter.Write(linkedFontFlagForHeader);
				binaryWriter.Write(ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR);
				binaryWriter.Write(Encoding.UTF8.GetBytes(str));
				binaryWriter.Write(ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR);
				binaryWriter.Write(ZPLUtilities.ZPL_INTERNAL_DELIMITER_CHAR);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new Exception(oException.Message, oException);
			}
			return binaryWriter.BaseStream;
		}

		public static void SaveFontAsPrinterFont(Stream fontInputStream, Stream destinationStream, string pathOnPrinter, string extension)
		{
			try
			{
				pathOnPrinter = FileUtilities.ChangeExtension(pathOnPrinter, extension);
				if (fontInputStream is FileStream)
				{
					PrinterFileMetadata printerFileMetadatum = new PrinterFileMetadata(fontInputStream.Length, "0000", "0000");
					using (PrinterFileDescriptor printerFileDescriptor = new PrinterFileDescriptor(fontInputStream, pathOnPrinter, printerFileMetadatum))
					{
						byte[] bytes = Encoding.UTF8.GetBytes(FileWrapper.CreateCisdfHeader(printerFileDescriptor));
						destinationStream.Write(bytes, 0, (int)bytes.Length);
						StreamHelper.CopyAndCloseSourceStream(destinationStream, fontInputStream);
						byte[] cisdfTrailer = FileWrapper.CisdfTrailer;
						destinationStream.Write(cisdfTrailer, 0, (int)cisdfTrailer.Length);
					}
				}
				else if (!fontInputStream.CanSeek)
				{
					using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
					{
						for (int i = 0; (long)i < fontInputStream.Length; i++)
						{
							binaryWriter.Write((byte)fontInputStream.ReadByte());
						}
						byte[] array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
						PrinterFileMetadata printerFileMetadatum1 = new PrinterFileMetadata((new BinaryReader(new MemoryStream(array))).BaseStream);
						using (PrinterFileDescriptor printerFileDescriptor1 = new PrinterFileDescriptor((new BinaryReader(new MemoryStream(Encoding.UTF8.GetBytes("")))).BaseStream, pathOnPrinter, printerFileMetadatum1))
						{
							array = Encoding.UTF8.GetBytes(FileWrapper.CreateCisdfHeader(printerFileDescriptor1));
							destinationStream.Write(array, 0, (int)array.Length);
							binaryWriter.BaseStream.Position = (long)0;
							StreamHelper.CopyAndCloseSourceStream(destinationStream, binaryWriter.BaseStream);
							byte[] numArray = FileWrapper.CisdfTrailer;
							destinationStream.Write(numArray, 0, (int)numArray.Length);
						}
					}
				}
				else
				{
					PrinterFileMetadata printerFileMetadatum2 = new PrinterFileMetadata(fontInputStream);
					fontInputStream.Position = (long)0;
					using (PrinterFileDescriptor printerFileDescriptor2 = new PrinterFileDescriptor(fontInputStream, pathOnPrinter, printerFileMetadatum2))
					{
						byte[] bytes1 = Encoding.UTF8.GetBytes(FileWrapper.CreateCisdfHeader(printerFileDescriptor2));
						destinationStream.Write(bytes1, 0, (int)bytes1.Length);
						StreamHelper.CopyAndCloseSourceStream(destinationStream, fontInputStream);
						byte[] cisdfTrailer1 = FileWrapper.CisdfTrailer;
						destinationStream.Write(cisdfTrailer1, 0, (int)cisdfTrailer1.Length);
					}
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new ArgumentException(exception.Message, exception);
			}
		}
	}
}