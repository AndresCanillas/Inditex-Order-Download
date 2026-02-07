using System;
using System.IO;

namespace Zebra.Sdk.Graphics.Internal
{
	internal abstract class CompressedBitmapOutputStreamA : MemoryStream
	{
		private static int INTERNAL_ENCODED_BUFFER_SIZE;

		protected Stream outputStream;

		protected BinaryWriter internalEncodedBuffer;

		static CompressedBitmapOutputStreamA()
		{
			CompressedBitmapOutputStreamA.INTERNAL_ENCODED_BUFFER_SIZE = 1024;
		}

		public CompressedBitmapOutputStreamA()
		{
		}

		protected void BufferAndWrite(char encodedChar)
		{
			if (this.internalEncodedBuffer.BaseStream.Length < (long)CompressedBitmapOutputStreamA.INTERNAL_ENCODED_BUFFER_SIZE)
			{
				this.internalEncodedBuffer.Write((byte)encodedChar);
			}
			if (this.internalEncodedBuffer.BaseStream.Length == (long)CompressedBitmapOutputStreamA.INTERNAL_ENCODED_BUFFER_SIZE)
			{
				this.outputStream.Write(((MemoryStream)this.internalEncodedBuffer.BaseStream).ToArray(), 0, (int)this.internalEncodedBuffer.BaseStream.Length);
				this.internalEncodedBuffer.BaseStream.SetLength((long)0);
			}
		}

		protected override void Dispose(bool disposing)
		{
			this.Flush();
		}

		public override void Flush()
		{
			if (this.internalEncodedBuffer.BaseStream.Length != 0)
			{
				this.outputStream.Write(((MemoryStream)this.internalEncodedBuffer.BaseStream).ToArray(), 0, (int)this.internalEncodedBuffer.BaseStream.Length);
				this.internalEncodedBuffer.BaseStream.SetLength((long)0);
			}
		}

		public override void WriteByte(byte value)
		{
			throw new IOException("This method is not implemented.");
		}
	}
}