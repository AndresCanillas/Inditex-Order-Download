using System;
using System.IO;

namespace Zebra.Sdk.Graphics.Internal
{
	internal class NaughtyBitOutputStream : MemoryStream
	{
		private Stream os;

		public NaughtyBitOutputStream(Stream os)
		{
			this.os = os;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				this.WriteByte(buffer[offset + i]);
			}
		}

		public override void WriteByte(byte value)
		{
			this.os.WriteByte((byte)(~value));
		}
	}
}