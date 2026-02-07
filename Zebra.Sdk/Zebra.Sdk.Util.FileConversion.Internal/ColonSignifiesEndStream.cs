using System;
using System.IO;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class ColonSignifiesEndStream : MemoryStream
	{
		private Stream sourceStream;

		public ColonSignifiesEndStream(Stream sourceStream)
		{
			this.sourceStream = sourceStream;
		}

		public override int ReadByte()
		{
			int num = this.sourceStream.ReadByte();
			if (num == 58)
			{
				while (num != -1)
				{
					num = this.sourceStream.ReadByte();
				}
			}
			return num;
		}
	}
}