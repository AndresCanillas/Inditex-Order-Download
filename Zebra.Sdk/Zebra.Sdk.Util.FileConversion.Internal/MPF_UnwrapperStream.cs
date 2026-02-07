using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Device;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class MPF_UnwrapperStream : StreamDecoratorBase
	{
		private Stream sourceStream;

		private StringBuilder readAheadBuffer = new StringBuilder();

		private string boundary;

		private bool readingDataBody;

		private string fileName = "UNKNOWN.GRF";

		private long unwrappedFileSize;

		private ushort crc16;

		private ushort sum;

		public MPF_UnwrapperStream(Stream sourceStream)
		{
			Stream stream = sourceStream;
			if (stream == null)
			{
				throw new NullReferenceException("input stream is null");
			}
			this.sourceStream = stream;
		}

		private void ExtractBoundary()
		{
			for (int i = this.sourceStream.ReadByte(); i != -1; i = this.sourceStream.ReadByte())
			{
				this.readAheadBuffer.Append((char)i);
				MatchCollection matchCollections = (new Regex("^[\\s]*--([^\\s|^-]+)\\r\\n")).Matches(this.readAheadBuffer.ToString());
				if (matchCollections.Count > 0)
				{
					GroupCollection groups = matchCollections[0].Groups;
					this.boundary = string.Format("\r\n--{0}--\r\n", groups[1].ToString());
					this.readAheadBuffer.Remove(0, this.readAheadBuffer.Length);
					return;
				}
			}
		}

		private void ExtractFileNameFromHeader()
		{
			int num = this.sourceStream.ReadByte();
			while (num != -1)
			{
				this.readAheadBuffer.Append((char)num);
				if (!this.readAheadBuffer.ToString().EndsWith("\r\n\r\n"))
				{
					num = this.sourceStream.ReadByte();
				}
				else
				{
					MatchCollection matchCollections = (new Regex("\\s+filename\\s*=\\s*\"([^\"]+)")).Matches(this.readAheadBuffer.ToString());
					if (matchCollections.Count <= 0)
					{
						break;
					}
					GroupCollection groups = matchCollections[0].Groups;
					this.fileName = groups[1].ToString();
					return;
				}
			}
		}

		private PrinterFileType GetFileType()
		{
			PrinterFileType uNSUPPORTED = PrinterFileType.UNSUPPORTED;
			try
			{
				uNSUPPORTED = PrinterFileType.GetUnwrappedType(FileUtilities.ParseDriveAndExtension(this.fileName).Extension);
			}
			catch (ZebraIllegalArgumentException)
			{
				uNSUPPORTED = PrinterFileType.UNSUPPORTED;
			}
			return uNSUPPORTED;
		}

		private int GetNextCharacter()
		{
			int chars;
			if (this.readAheadBuffer.Length == 0 || this.readAheadBuffer.ToString().Equals(this.boundary))
			{
				chars = -1;
			}
			else
			{
				chars = this.readAheadBuffer[0];
				this.readAheadBuffer.Remove(0, 1);
				int num = this.sourceStream.ReadByte();
				if (num == -1)
				{
					throw new IOException("Malformed Multipart Form Data");
				}
				this.readAheadBuffer.Append((char)num);
				if (this.readAheadBuffer.ToString().Equals(this.boundary))
				{
					this.readAheadBuffer.Remove(0, this.readAheadBuffer.Length);
				}
			}
			return chars;
		}

		public override PrinterFileMetadata GetPrinterFileMetadata()
		{
			string str = string.Format("{0:X4}", this.crc16);
			string str1 = string.Format("{0:X4}", (this.sum ^ 65535) + 1);
			return new PrinterFileMetadata(this.unwrappedFileSize, str, str1, this.fileName)
			{
				PrinterFileType = this.GetFileType(),
				PrinterWrappingType = PrinterWrappingType.MPF
			};
		}

		private void PreloadReadAheadBuffer()
		{
			if (this.boundary != null)
			{
				this.readAheadBuffer.Remove(0, this.readAheadBuffer.Length);
				while (this.readAheadBuffer.Length != this.boundary.Length)
				{
					this.readAheadBuffer.Append((char)this.sourceStream.ReadByte());
				}
			}
		}

		public override int ReadByte()
		{
			if (!this.readingDataBody)
			{
				this.ExtractBoundary();
				this.ExtractFileNameFromHeader();
				this.PreloadReadAheadBuffer();
				this.readingDataBody = true;
			}
			int nextCharacter = this.GetNextCharacter();
			if (nextCharacter != -1)
			{
				this.unwrappedFileSize += (long)1;
				this.sum = (ushort)(this.sum + (ushort)nextCharacter);
				this.crc16 = ZCRC16.AddCrc16Byte_cpcl(this.crc16, nextCharacter);
			}
			return nextCharacter;
		}
	}
}