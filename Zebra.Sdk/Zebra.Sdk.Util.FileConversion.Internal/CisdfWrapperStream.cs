using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class CisdfWrapperStream : MemoryStream, IDisposable
	{
		private Stream sourceStream;

		private MemoryStream cisdfHeader;

		public CisdfWrapperStream(Stream sourceStream, PrinterFileMetadata metaData)
		{
			this.sourceStream = sourceStream;
			this.cisdfHeader = new MemoryStream(Encoding.UTF8.GetBytes(string.Format("! CISDFCRC16\r\n{0}\r\n{1}\r\n{2:X8}\r\n{3}\r\n", new object[] { metaData.Crc16, metaData.FileName, metaData.FileSize, metaData.CheckSum })));
		}

		protected override void Dispose(bool disposing)
		{
			if (this.cisdfHeader != null)
			{
				this.cisdfHeader.Dispose();
			}
			base.Dispose(disposing);
		}

		public override int Read(byte[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0 || count < 0 || count > (int)buffer.Length - index)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count == 0)
			{
				return 0;
			}
			int num = this.ReadByte();
			if (num == -1)
			{
				return -1;
			}
			buffer[index] = (byte)num;
			int num1 = 1;
			try
			{
				while (num1 < count)
				{
					num = this.ReadByte();
					if (num == -1)
					{
						break;
					}
					buffer[index + num1] = (byte)num;
					num1++;
				}
			}
			catch (Exception)
			{
			}
			return num1;
		}

		public override int ReadByte()
		{
			int num = -1;
			num = (this.cisdfHeader.Position >= this.cisdfHeader.Length ? this.sourceStream.ReadByte() : this.cisdfHeader.ReadByte());
			return num;
		}
	}
}