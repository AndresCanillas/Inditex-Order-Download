using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Operations.Internal;
using Zebra.Sdk.Util.FileConversion.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class FileUtilLinkOsImpl : FileUtilLinkOs
	{
		private ZebraPrinterLinkOs zebraPrinterLinkOs;

		private static HashSet<string> validExtensionsToGetObjectFromPrinter;

		static FileUtilLinkOsImpl()
		{
			FileUtilLinkOsImpl.validExtensionsToGetObjectFromPrinter = new HashSet<string>(new string[] { "FNT", "ZPL", "GRF", "DAT", "BAS", "STO", "PNG", "LBL", "TTF", "PCX", "BMP", "IMG", "TTE", "WML", "CSV", "HTM", "BAE", "TXT" });
		}

		public FileUtilLinkOsImpl(ZebraPrinterLinkOs zebraPrinterLinkOs)
		{
			this.zebraPrinterLinkOs = zebraPrinterLinkOs;
		}

		private void CopyInputStreamToOutputStream(Stream inputStream, Stream outputStream)
		{
			int num = inputStream.ReadByte();
			if (num == -1)
			{
				throw new ZebraIllegalArgumentException("Invalid extension or file not found");
			}
			while (num != -1)
			{
				outputStream.WriteByte(Convert.ToByte(num));
				num = inputStream.ReadByte();
			}
		}

		public void DeleteFile(string filePath)
		{
			SGD.SET("file.delete", filePath, this.zebraPrinterLinkOs.Connection);
		}

		private void GetFileOverFtp(Stream destination, string filePath, string ftpPassword)
		{
			Connection connection = this.zebraPrinterLinkOs.Connection;
			if (!(connection is TcpConnection))
			{
				if (!(connection is MultichannelConnection) || !(((MultichannelConnection)connection).PrintingChannel is TcpConnection))
				{
					throw new ConnectionException("Must be a TCP connected printer to tranfer files");
				}
				using (FTP fTP = new FTP(((TcpConnection)((MultichannelConnection)connection).PrintingChannel).Address, null, ftpPassword))
				{
					fTP.GetFile(destination, filePath);
				}
			}
			else
			{
				using (FTP fTP1 = new FTP(((TcpConnection)connection).Address, null, ftpPassword))
				{
					fTP1.GetFile(destination, filePath);
				}
			}
		}

		private XmlNode GetObjectData(string filePath, Stream resultStream)
		{
			return XmlUtil.GetDataAtNamedNode(resultStream, "OBJECT-DATA", filePath);
		}

		public byte[] GetObjectFromPrinter(string filePath)
		{
			byte[] array;
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				this.GetObjectFromPrinter(binaryWriter.BaseStream, filePath);
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		public void GetObjectFromPrinter(Stream destinationStream, string filePath)
		{
			using (Stream stream = (new ObjectGrabberOperation(filePath, this.zebraPrinterLinkOs.Connection, this.zebraPrinterLinkOs.PrinterControlLanguage, this.zebraPrinterLinkOs.LinkOsInformation)).Execute())
			{
				HzoToDzConverterStream hzoToDzConverterStream = null;
				Stream mPFUnwrapperStream = null;
				try
				{
					try
					{
						if (stream is MultipartFormReceiverStream)
						{
							mPFUnwrapperStream = new MPF_UnwrapperStream(stream);
						}
						else if (!FileWrapper.IsHzoExtension(filePath.Substring(filePath.LastIndexOf('.') + 1)))
						{
							if (!this.ValidExtension(filePath))
							{
								throw new ZebraIllegalArgumentException("Invalid extension, cannot retrieve file type");
							}
							this.RetrieveFileViaFileTypeSgdCommand(destinationStream, filePath);
							return;
						}
						else
						{
							hzoToDzConverterStream = new HzoToDzConverterStream(stream);
							mPFUnwrapperStream = new DZ_UnwrapperStream(hzoToDzConverterStream);
						}
						this.CopyInputStreamToOutputStream(mPFUnwrapperStream, destinationStream);
					}
					catch (IOException oException1)
					{
						IOException oException = oException1;
						if (!oException.Message.Equals("File not found"))
						{
							throw new ConnectionException(oException.Message);
						}
						throw new ZebraIllegalArgumentException(oException.Message);
					}
				}
				finally
				{
					if (hzoToDzConverterStream != null)
					{
						hzoToDzConverterStream.Dispose();
					}
					if (mPFUnwrapperStream != null)
					{
						mPFUnwrapperStream.Dispose();
					}
				}
			}
		}

		public byte[] GetObjectFromPrinterViaFtp(string filePath, string ftpPassword)
		{
			byte[] array;
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				this.GetObjectFromPrinterViaFtp(binaryWriter.BaseStream, filePath, ftpPassword);
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		public void GetObjectFromPrinterViaFtp(Stream destinationStream, string filePath, string ftpPassword)
		{
			if (!this.ValidExtension(filePath))
			{
				throw new ZebraIllegalArgumentException("Invalid extension, cannot retrieve file type");
			}
			this.GetFileOverFtp(destinationStream, filePath, ftpPassword);
		}

		public byte[] GetPrinterDownloadableObjectFromPrinter(string filePath)
		{
			byte[] bytes;
			if (!this.ValidExtension(filePath))
			{
				throw new ZebraIllegalArgumentException("Invalid extension, cannot retrieve file type");
			}
			using (Stream stream = (new ObjectGrabberOperation(filePath, this.zebraPrinterLinkOs.Connection, this.zebraPrinterLinkOs.PrinterControlLanguage, this.zebraPrinterLinkOs.LinkOsInformation)).Execute())
			{
				Stream mPFUnwrapperStream = null;
				using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
				{
					try
					{
						try
						{
							if (stream is MultipartFormReceiverStream)
							{
								mPFUnwrapperStream = new MPF_UnwrapperStream(stream);
								this.CopyInputStreamToOutputStream(mPFUnwrapperStream, binaryWriter.BaseStream);
								bytes = Encoding.UTF8.GetBytes(FileWrapper.WrapFile(((MemoryStream)binaryWriter.BaseStream).ToArray(), filePath, false, false));
							}
							else if (!FileWrapper.IsHzoExtension(filePath.Substring(filePath.LastIndexOf('.') + 1)))
							{
								bytes = FileWrapper.WrapFileWithCisdfHeader(this.RetrieveFileViaFileTypeSgdCommand(filePath), filePath);
							}
							else
							{
								this.CopyInputStreamToOutputStream(stream, binaryWriter.BaseStream);
								string str = this.ParseHzo(filePath, (new BinaryReader(binaryWriter.BaseStream)).BaseStream);
								bytes = Encoding.UTF8.GetBytes(str);
							}
						}
						catch (IOException oException1)
						{
							IOException oException = oException1;
							throw new ConnectionException(oException.Message, oException);
						}
					}
					finally
					{
						if (mPFUnwrapperStream != null)
						{
							mPFUnwrapperStream.Dispose();
						}
					}
				}
			}
			return bytes;
		}

		public List<StorageInfo> GetStorageInfo()
		{
			return (new StorageInfoGrabber(this.zebraPrinterLinkOs.Connection, this.zebraPrinterLinkOs.PrinterControlLanguage, this.zebraPrinterLinkOs.LinkOsInformation)).Execute();
		}

		private string ParseHzo(string filePath, Stream resultStream)
		{
			string textContent;
			try
			{
				textContent = XmlUtil.GetTextContent(this.GetObjectData(filePath, resultStream), "");
			}
			catch (Exception exception)
			{
				throw new ZebraIllegalArgumentException(exception.Message);
			}
			return textContent;
		}

		private byte[] RetrieveFileViaFileTypeSgdCommand(string value)
		{
			byte[] array;
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				this.RetrieveFileViaFileTypeSgdCommand(binaryWriter.BaseStream, value);
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		private void RetrieveFileViaFileTypeSgdCommand(Stream destinationStream, string value)
		{
			PrinterCommandImpl printerCommandImpl = new PrinterCommandImpl(string.Concat("! U1 do \"file.type\" \"", value, "\"", StringUtilities.CRLF));
			Connection connection = this.zebraPrinterLinkOs.Connection;
			((PrinterCommand)printerCommandImpl).SendAndWaitForResponse(new BinaryWriter(destinationStream), connection, connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, null);
		}

		public void StoreFileOnPrinter(string filePath)
		{
			this.StoreFileOnPrinter(filePath, FileUtilities.GetFileNameOnPrinter(filePath));
		}

		public void StoreFileOnPrinter(string filePath, string fileNameOnPrinter)
		{
			this.StoreFileOnPrinter(FileReader.ToByteArray(filePath), fileNameOnPrinter);
		}

		public void StoreFileOnPrinter(byte[] fileContents, string fileNameOnPrinter)
		{
			if (fileContents == null)
			{
				throw new ZebraIllegalArgumentException("File not found");
			}
			using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(fileContents)))
			{
				PrinterFileMetadata printerFileMetadatum = null;
				try
				{
					printerFileMetadatum = new PrinterFileMetadata(binaryReader.BaseStream);
					binaryReader.BaseStream.Position = (long)0;
				}
				catch (IOException oException)
				{
					throw new ZebraIllegalArgumentException(oException.Message);
				}
				(new FileStorer(new List<PrinterFileDescriptor>()
				{
					new PrinterFileDescriptor(binaryReader.BaseStream, fileNameOnPrinter, printerFileMetadatum)
				}, this.zebraPrinterLinkOs.Connection, this.zebraPrinterLinkOs.PrinterControlLanguage, this.zebraPrinterLinkOs.LinkOsInformation)).Execute();
			}
		}

		private bool ValidExtension(string filePath)
		{
			return FileUtilLinkOsImpl.validExtensionsToGetObjectFromPrinter.Contains(filePath.Substring(filePath.LastIndexOf('.') + 1));
		}
	}
}