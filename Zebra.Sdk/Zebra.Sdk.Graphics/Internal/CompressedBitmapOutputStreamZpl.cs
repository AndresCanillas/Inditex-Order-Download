using System;
using System.IO;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Graphics.Internal
{
	internal class CompressedBitmapOutputStreamZpl : CompressedBitmapOutputStreamA
	{
		private byte previousByteWritten;

		private int previousByteWrittenRepeatCount;

		private static int[] charMap;

		private static char[] charVal;

		static CompressedBitmapOutputStreamZpl()
		{
			CompressedBitmapOutputStreamZpl.charMap = new int[] { 380, 360, 340, 320, 300, 280, 260, 240, 220, 200, 180, 160, 140, 120, 100, 80, 60, 40, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
			CompressedBitmapOutputStreamZpl.charVal = new char[] { 'y', 'x', 'w', 'v', 'u', 't', 's', 'r', 'q', 'p', 'o', 'n', 'm', 'l', 'k', 'j', 'i', 'h', 'g', 'Y', 'X', 'W', 'V', 'U', 'T', 'S', 'R', 'Q', 'P', 'O', 'N', 'M', 'L', 'K', 'J', 'I', 'H', 'G' };
		}

		public CompressedBitmapOutputStreamZpl(Stream outputStream)
		{
			this.outputStream = outputStream;
			this.internalEncodedBuffer = new BinaryWriter(new MemoryStream());
		}

		private void ComputeAndOutput()
		{
			if (this.previousByteWrittenRepeatCount > 1)
			{
				int num = this.previousByteWrittenRepeatCount / 400;
				int num1 = this.previousByteWrittenRepeatCount % 400;
				for (int i = 0; i < num; i++)
				{
					base.BufferAndWrite('z');
				}
				for (int j = 0; j < (int)CompressedBitmapOutputStreamZpl.charMap.Length; j++)
				{
					if (num1 >= CompressedBitmapOutputStreamZpl.charMap[j])
					{
						base.BufferAndWrite(CompressedBitmapOutputStreamZpl.charVal[j]);
						num1 -= CompressedBitmapOutputStreamZpl.charMap[j];
					}
				}
			}
			int num2 = this.previousByteWritten & 15;
			base.BufferAndWrite(num2.ToString("X")[0]);
		}

		private byte[] ExtractNibblesFromByte(byte byteToBreakIntoNibbles)
		{
			return new byte[] { (byte)(~byteToBreakIntoNibbles >> 4 & 15), (byte)(~byteToBreakIntoNibbles & 15) };
		}

		public override void Flush()
		{
			if (this.previousByteWrittenRepeatCount > 0)
			{
				this.SendBufferedDataToPrinter();
				this.previousByteWrittenRepeatCount = 0;
			}
			base.Flush();
		}

		private void SendBufferedDataToPrinter()
		{
			try
			{
				this.ComputeAndOutput();
			}
			catch (ConnectionException connectionException)
			{
				throw new IOException(connectionException.Message);
			}
		}

		public override void Write(byte[] dataToWrite, int offset, int count)
		{
			for (int i = offset; i < count; i++)
			{
				this.WriteNibblesToStream(this.ExtractNibblesFromByte(dataToWrite[i]));
			}
		}

		private void WriteNibblesToStream(byte[] nibblesToWrite)
		{
			for (int i = 0; i < (int)nibblesToWrite.Length; i++)
			{
				this.WriteNibbleToStream(nibblesToWrite[i]);
			}
		}

		private void WriteNibbleToStream(byte nibbleToWrite)
		{
			if (this.previousByteWrittenRepeatCount == 0)
			{
				this.previousByteWritten = nibbleToWrite;
				this.previousByteWrittenRepeatCount++;
				return;
			}
			if (this.previousByteWritten == nibbleToWrite)
			{
				this.previousByteWrittenRepeatCount++;
				return;
			}
			this.SendBufferedDataToPrinter();
			this.previousByteWritten = nibbleToWrite;
			this.previousByteWrittenRepeatCount = 1;
		}
	}
}