using System;
using System.IO;

namespace Zebra.Sdk.Graphics.Internal
{
	internal class CompressedBitmapOutputStreamCpcl : CompressedBitmapOutputStreamA
	{
		public CompressedBitmapOutputStreamCpcl(Stream outputStream)
		{
			this.outputStream = outputStream;
			this.internalEncodedBuffer = new BinaryWriter(new MemoryStream());
		}

		public override void Write(byte[] dataToWrite, int offset, int count)
		{
			for (int i = offset; i < count; i++)
			{
				base.BufferAndWrite((char)(~dataToWrite[i]));
			}
		}
	}
}