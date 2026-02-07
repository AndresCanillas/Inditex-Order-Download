using System;
using System.IO;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class AsciiDecoderStream : MemoryStream
	{
		private Stream sourceStream;

		private int rleRepeatCount;

		private int rleRepeatChar = -1;

		private int nibbleCounter;

		private int[] previousRow;

		private int previousRowIndex = -1;

		public AsciiDecoderStream(Stream sourceStream, int bytesPerRow)
		{
			this.sourceStream = sourceStream;
			this.previousRow = (bytesPerRow < 0 ? new int[2] : new int[bytesPerRow * 2]);
			this.previousRowIndex = (int)this.previousRow.Length;
		}

		private void FillRemainderOfRow(int startIndex, char charToFill)
		{
			for (int i = startIndex; i < (int)this.previousRow.Length; i++)
			{
				this.previousRow[i] = charToFill;
			}
		}

		private int GetNextChar()
		{
			int previousRowNibbleAt = -1;
			if (this.previousRowIndex < (int)this.previousRow.Length)
			{
				previousRowNibbleAt = this.GetPreviousRowNibbleAt(this.previousRowIndex);
			}
			else if (this.rleRepeatCount <= 1)
			{
				this.rleRepeatCount = 0;
				int length = this.nibbleCounter % (int)this.previousRow.Length;
				do
				{
					previousRowNibbleAt = this.sourceStream.ReadByte();
					if (previousRowNibbleAt == 44)
					{
						this.FillRemainderOfRow(length, '0');
						previousRowNibbleAt = this.GetPreviousRowNibbleAt(length);
					}
					else if (previousRowNibbleAt == 33)
					{
						this.FillRemainderOfRow(length, 'F');
						previousRowNibbleAt = this.GetPreviousRowNibbleAt(length);
					}
					else if (previousRowNibbleAt == 58)
					{
						previousRowNibbleAt = this.GetPreviousRowNibbleAt(0);
					}
					else if (previousRowNibbleAt > 70 && previousRowNibbleAt < 90)
					{
						this.rleRepeatCount = this.rleRepeatCount + (previousRowNibbleAt - 70);
					}
					else if (previousRowNibbleAt <= 102 || previousRowNibbleAt >= 123)
					{
						if (this.rleRepeatCount <= 0)
						{
							continue;
						}
						this.rleRepeatChar = previousRowNibbleAt;
					}
					else
					{
						this.rleRepeatCount = this.rleRepeatCount + (previousRowNibbleAt - 102) * 20;
					}
				}
				while (previousRowNibbleAt != -1 && !this.IsAsciiHex(previousRowNibbleAt));
			}
			else
			{
				this.rleRepeatCount--;
				previousRowNibbleAt = this.rleRepeatChar;
			}
			return previousRowNibbleAt;
		}

		private int GetPreviousRowNibbleAt(int index)
		{
			this.previousRowIndex = index;
			int[] numArray = this.previousRow;
			int num = this.previousRowIndex;
			this.previousRowIndex = num + 1;
			return numArray[num];
		}

		private static int HexToInt(int ch)
		{
			if (97 <= ch && ch <= 102)
			{
				return ch - 97 + 10;
			}
			if (65 <= ch && ch <= 70)
			{
				return ch - 65 + 10;
			}
			if (48 > ch || ch > 57)
			{
				throw new ArgumentException(Convert.ToString(ch));
			}
			return ch - 48;
		}

		private bool IsAsciiHex(int hiNibble)
		{
			bool flag;
			try
			{
				AsciiDecoderStream.HexToInt(hiNibble);
				return true;
			}
			catch (ArgumentException)
			{
				flag = false;
			}
			return flag;
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
			int nextChar = this.GetNextChar();
			this.SaveCurrentNibble(nextChar);
			int num = this.GetNextChar();
			this.SaveCurrentNibble(num);
			if (nextChar == -1 || num == -1)
			{
				return -1;
			}
			return (ushort)(AsciiDecoderStream.HexToInt(nextChar) << 4 | AsciiDecoderStream.HexToInt(num));
		}

		private void SaveCurrentNibble(int hiNibble)
		{
			int[] numArray = this.previousRow;
			int num = this.nibbleCounter;
			this.nibbleCounter = num + 1;
			numArray[num % (int)this.previousRow.Length] = hiNibble;
		}
	}
}