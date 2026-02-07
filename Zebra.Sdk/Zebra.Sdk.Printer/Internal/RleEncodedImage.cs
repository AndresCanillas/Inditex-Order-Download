using System;
using System.IO;

namespace Zebra.Sdk.Printer.Internal
{
	internal class RleEncodedImage : IDisposable
	{
		private MemoryStream outputImageByteStream = new MemoryStream();

		private bool disposedValue;

		public RleEncodedImage()
		{
		}

		private bool BothUpperBitsAreSet(byte data)
		{
			return (data & 192) == 192;
		}

		private byte[] CreateRun(int runLength, byte byteValue)
		{
			byte num = (byte)((byte)runLength | 192);
			return new byte[] { num, byteValue };
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing && this.outputImageByteStream != null)
				{
					this.outputImageByteStream.Dispose();
					this.outputImageByteStream = null;
				}
				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		private byte[] EncodeRun(byte currentByte, RleEncodedImage.DataBuffer data)
		{
			int num = 1;
			int num1 = 63;
			while (data.BytesLeft > 0 && currentByte == data.Peek && num < num1)
			{
				byte getByte = data.GetByte;
				num++;
			}
			return this.CreateRun(num, currentByte);
		}

		private byte[] GetNextElement(RleEncodedImage.DataBuffer data)
		{
			byte[] numArray;
			byte getByte = data.GetByte;
			if (data.BytesLeft <= 0 || getByte != data.Peek)
			{
				numArray = (!this.BothUpperBitsAreSet(getByte) ? new byte[] { getByte } : this.CreateRun(1, getByte));
			}
			else
			{
				numArray = this.EncodeRun(getByte, data);
			}
			return numArray;
		}

		private void OutputElement(byte[] nextOutputElement)
		{
			this.outputImageByteStream.Write(nextOutputElement, 0, (int)nextOutputElement.Length);
		}

		public byte[] RleEncoding(byte[] imageData, int widthOfImageInBytes)
		{
			byte[] numArray;
			byte[] array;
			try
			{
				bool flag = widthOfImageInBytes % 2 != 0;
				byte num = 0;
				for (int i = 0; i < (int)imageData.Length; i += widthOfImageInBytes)
				{
					int length = (int)imageData.Length - i;
					int num1 = (length < widthOfImageInBytes ? length : widthOfImageInBytes);
					if (!flag)
					{
						numArray = new byte[num1];
						Array.Copy(imageData, i, numArray, 0, (int)numArray.Length);
					}
					else
					{
						numArray = new byte[num1 + 1];
						for (int j = 0; j < num1; j++)
						{
							numArray[j] = imageData[i + j];
						}
						numArray[num1] = num;
					}
					this.RleEncoding(new RleEncodedImage.DataBuffer(numArray));
				}
				array = this.outputImageByteStream.ToArray();
			}
			finally
			{
				if (this.outputImageByteStream != null)
				{
					this.outputImageByteStream.Dispose();
					this.outputImageByteStream = null;
				}
			}
			return array;
		}

		private void RleEncoding(RleEncodedImage.DataBuffer data)
		{
			this.OutputElement(this.GetNextElement(data));
			if (data.BytesLeft > 0)
			{
				this.RleEncoding(data);
			}
		}

		public class DataBuffer
		{
			private byte[] imageData;

			private int currentIndex;

			internal int BytesLeft
			{
				get
				{
					return (int)this.imageData.Length - this.currentIndex;
				}
			}

			public byte GetByte
			{
				get
				{
					byte[] numArray = this.imageData;
					int num = this.currentIndex;
					this.currentIndex = num + 1;
					return numArray[num];
				}
			}

			internal byte Peek
			{
				get
				{
					return this.imageData[this.currentIndex];
				}
			}

			public DataBuffer(byte[] imageData)
			{
				this.imageData = imageData;
			}
		}
	}
}