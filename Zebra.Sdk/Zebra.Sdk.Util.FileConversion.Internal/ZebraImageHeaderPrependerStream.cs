using System;
using System.IO;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class ZebraImageHeaderPrependerStream : MemoryStream
	{
		private Stream dataUnwrapperStream;

		private static int preReadLimit;

		private int preReadCounter;

		private int preReadIndex;

		private int[] preReadDataBuffer = new int[ZebraImageHeaderPrependerStream.preReadLimit];

		private int[] headerData;

		private int headerCounter;

		static ZebraImageHeaderPrependerStream()
		{
			ZebraImageHeaderPrependerStream.preReadLimit = 50;
		}

		public ZebraImageHeaderPrependerStream(Stream dyDataProviderStream, int bytesPerRow, int totalBytesInData)
		{
			Stream stream = dyDataProviderStream;
			if (stream == null)
			{
				throw new IOException("input stream is null");
			}
			this.dataUnwrapperStream = stream;
			while (this.preReadIndex < ZebraImageHeaderPrependerStream.preReadLimit)
			{
				int[] numArray = this.preReadDataBuffer;
				int num = this.preReadIndex;
				this.preReadIndex = num + 1;
				numArray[num] = this.dataUnwrapperStream.ReadByte();
			}
			if (this.headerData == null)
			{
				this.headerData = DYHelper.CalculateZebraHeader(this.preReadDataBuffer, bytesPerRow, totalBytesInData);
			}
		}

		public override int ReadByte()
		{
			int num;
			int num1 = -1;
			if (this.headerData != null && this.headerCounter < 4)
			{
				int[] numArray = this.headerData;
				num = this.headerCounter;
				this.headerCounter = num + 1;
				num1 = numArray[num];
			}
			else if (this.preReadCounter >= this.preReadIndex)
			{
				num1 = this.dataUnwrapperStream.ReadByte();
			}
			else
			{
				int[] numArray1 = this.preReadDataBuffer;
				num = this.preReadCounter;
				this.preReadCounter = num + 1;
				num1 = numArray1[num];
			}
			return num1;
		}
	}
}