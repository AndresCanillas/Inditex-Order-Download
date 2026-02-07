using System;
using System.IO;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal abstract class StreamDecoratorBase : MemoryStream, MetadataProvider
	{
		public StreamDecoratorBase()
		{
		}

		public abstract PrinterFileMetadata GetPrinterFileMetadata();

		public override int Read(byte[] buffer, int index, int count)
		{
			int i;
			if (buffer == null)
			{
				throw new ArgumentNullException();
			}
			if (index < 0 || count < 0 || count > (int)buffer.Length - index)
			{
				throw new IndexOutOfRangeException();
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
			for (i = 1; i < count; i++)
			{
				num = this.ReadByte();
				if (num == -1)
				{
					break;
				}
				buffer[index + i] = (byte)num;
			}
			return i;
		}
	}
}