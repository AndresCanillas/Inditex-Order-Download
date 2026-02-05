using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public static class ByteArrayExtensions
	{
		public static int FindByteIndex(this byte[] buffer, byte b, int startIndex = 0)
		{
			for (int i = startIndex; i < buffer.Length; i++)
			{
				if (buffer[i] == b)
					return i;
			}
			return -1;
		}

		public static int SearchIndex(this byte[] buffer, byte[] pattern, int startIndex, int availableBytes)
		{
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			if (pattern.Length == 0)
				return -1;
			for (int i = startIndex; i < buffer.Length && i < availableBytes; i++)
			{
				if (buffer[i] == pattern[0] && isMatch(i))
					return i;
			}
			return -1;

			bool isMatch(int idx)
			{
				for (int i = 1, j = idx + 1; i < pattern.Length && j < buffer.Length; j++, i++)
				{
					if (buffer[j] != pattern[i])
						return false;
				}
				return true;
			}
		}


		public static byte[] Insert(this byte[] target, int startIndex, byte[] content)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if(startIndex > target.Length || startIndex < 0)
				throw new IndexOutOfRangeException($"{nameof(startIndex)} {startIndex} is outside the bounds of the target byte array, Length: {target.Length}.");
			if (content == null || content.Length == 0)
				return target;

			int i = 0;
			byte[] result = new byte[target.Length + content.Length];

			for (; i < startIndex; i++)
				result[i] = target[i];

			int j = 0;
			for (; j < content.Length; j++)
				result[i+j] = content[j];

			for (; i < target.Length; i++)
				result[i + j] = target[i];

			return result;
		}


		public static List<byte[]> Split(this byte[] buffer, byte[] pattern)
		{
			int idx;
			int pos = 0;
			var result = new List<byte[]>();
			do
			{
				idx = buffer.SearchIndex(pattern, pos, buffer.Length);
				if(idx >= 0)
				{
					idx += pattern.Length;
					int len = idx - pos;
					var cpy = new byte[len];
					if (len > 0)
						Array.Copy(buffer, pos, cpy, 0, cpy.Length);
					result.Add(cpy);
					pos = idx;
				}
			} while (idx > 0);
			if(pos < buffer.Length)
			{
				var cpy = new byte[buffer.Length - pos];
				Array.Copy(buffer, pos, cpy, 0, cpy.Length);
				result.Add(cpy);
			}
			return result;
		}

		public static byte[] Join(this IEnumerable<byte[]> collection)
		{
			int pos = 0;
			int len = 0;
			foreach(var buffer in collection)
			{
				if (buffer != null)
					len += buffer.Length;
			}

			var result = new byte[len];
			foreach (var buffer in collection)
			{
				if (buffer != null)
				{
					Array.Copy(buffer, 0, result, pos, buffer.Length);
					pos += buffer.Length;
				}
			}
			return result;
		}


		public static bool IsEqual(this byte[] buffer, byte[] other)
		{
			if (buffer == null || other == null)
				throw new ArgumentNullException();

			if (buffer.Length != other.Length)
				return false;

			for (int i = 0; i < buffer.Length; i++)
			{
				if (buffer[i] != other[i])
					return false;
			}
			return true;
		}


		public static string ToHexString(this byte[] bytes)
		{
			StringBuilder sb = new StringBuilder(bytes.Length * 2);
			var upperBound = bytes.Length;
			for (int i = 0; i < upperBound; i++)
				sb.AppendFormat("{0:x2}", bytes[i]);
			return sb.ToString();
		}


		public static string FromHexString(this byte[] bytes)
		{
			StringBuilder sb = new StringBuilder(bytes.Length * 2);
			var upperBound = bytes.Length;
			for (int i = 0; i < upperBound; i++)
				sb.Append((bytes[i]-48).ToString());
			return sb.ToString();
		}


		// From CodeProject  :)
		public static string HexDump(this byte[] bytes, int index = 0, int length = -1, int bytesPerLine = 16)
		{
			if (bytes == null) return "<null>";
			if (index < 0) index = 0;
			if (length < 0) length = bytes.Length;

			char[] HexChars = "0123456789ABCDEF".ToCharArray();

			int firstHexColumn =
				  8                   // 8 characters for the address
				+ 3;                  // 3 spaces

			int firstCharColumn = firstHexColumn
				+ bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
				+ (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
				+ 2;                  // 2 spaces 

			int lineLength = firstCharColumn
				+ bytesPerLine           // - characters to show the ascii value
				+ Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

			char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
			int expectedLines = (length + bytesPerLine - 1) / bytesPerLine;
			StringBuilder result = new StringBuilder(expectedLines * lineLength);

			for (int i = 0; i < length; i += bytesPerLine)
			{
				line[0] = HexChars[(i >> 28) & 0xF];
				line[1] = HexChars[(i >> 24) & 0xF];
				line[2] = HexChars[(i >> 20) & 0xF];
				line[3] = HexChars[(i >> 16) & 0xF];
				line[4] = HexChars[(i >> 12) & 0xF];
				line[5] = HexChars[(i >> 8) & 0xF];
				line[6] = HexChars[(i >> 4) & 0xF];
				line[7] = HexChars[(i >> 0) & 0xF];

				int hexColumn = firstHexColumn;
				int charColumn = firstCharColumn;

				for (int j = 0; j < bytesPerLine; j++)
				{
					if (j > 0 && (j & 7) == 0) hexColumn++;
					if (i + j >= length)
					{
						line[hexColumn] = ' ';
						line[hexColumn + 1] = ' ';
						line[charColumn] = ' ';
					}
					else
					{
						byte b = bytes[i + index + j];
						line[hexColumn] = HexChars[(b >> 4) & 0xF];
						line[hexColumn + 1] = HexChars[b & 0xF];
						line[charColumn] = (b < 32 ? '·' : (char)b);
					}
					hexColumn += 3;
					charColumn++;
				}
				result.Append(line);
			}
			return result.ToString();
		}
	}
}
