using System;
using System.Collections;
using System.Text;

namespace Service.Contracts
{
	public class xtConvert
	{
		/// <summary>
		/// Converts an hexadecimal string into an array of bytes.
		/// IMPORTANT: This method interprets the provided code as if it were a decimal NUMBER, it places the LSB (right hand byte in the string) at index 0 in the array.
		/// </summary>
		public static byte[] HexNumberToByteArray(string hexCode)
		{
			int numBytes = Convert.ToInt32(Math.Ceiling(hexCode.Length * 0.5d));
			return HexNumberToByteArray(hexCode, numBytes*8);
		}


		/// <summary>
		/// Converts an hexadecimal string into an array of bytes.
		/// IMPORTANT: This method interprets the provided code as if it were a decimal NUMBER, it places the LSB (right hand byte in the string) at index 0 in the array.
		/// </summary>
		public static byte[] HexNumberToByteArray(string hexCode, int bitLen)
		{
			if (!IsHexadecimal(hexCode)) throw new ArgumentException("Invalid Hexadecimal Value");
			int len = hexCode.Length;
			if (len % 2 != 0)
			{
				hexCode = "0" + hexCode;
				len++;
				bitLen += 4;
			}
			byte[] arr = new byte[bitLen / 8];
			int j = len;
			for (int i = 0; i < arr.Length; i++)
			{
				if (j >= 2)
					arr[i] = Convert.ToByte(hexCode.Substring(j - 2, 2), 16);
				else
					arr[i] = 0;
				j -= 2;
			}
			return arr;
		}


		/// <summary>
		/// Converts the array of bytes into its hexadecimal representation.
		/// IMPORTANT: This method interprets the provided code as if it were a decimal NUMBER, it places the LSB (byte at index 0) to the right side of the string.
		/// </summary>
		public static string ByteArrayToHexNumber(byte[] code)
		{
			string hex;
			if (code.Length > 32) throw new InvalidOperationException("Code value is too large, maximum capacity is 256 bits.");
			StringBuilder sb = new StringBuilder();
			for (int i = code.Length; --i >= 0; )
			{
				hex = code[i].ToString("X2");
				if(hex.Length == 1) sb.Append('0');
				sb.Append(hex);
			}
			return sb.ToString().ToUpper();
		}


		/// <summary>
		/// Converts the array of bytes into its hexadecimal representation.
		/// IMPORTANT: This method interprets the provided code as if it were a decimal NUMBER, it places the LSB (byte at index 0) to the right side of the string.
		/// </summary>
		public static string ByteArrayToHexNumber(byte[] code, int byteCount)
		{
			string hex;
			if (code.Length > 32) throw new InvalidOperationException("Code value is too large, maximum capacity is 256 bits.");
			StringBuilder sb = new StringBuilder();
			for (int i = code.Length; --i >= 0 && byteCount > 0; byteCount--)
			{
				hex = Convert.ToString(code[i], 16);
				if(hex.Length == 1) sb.Append('0');
				sb.Append(hex);
			}
			return sb.ToString().ToUpper();
		}


		/// <summary>
		/// Converts the array of bytes into its hexadecimal representation.
		/// IMPORTANT: This method interprets the provided code as if it were a decimal NUMBER, it places the LSB (byte at index 0) to the right side of the string.
		/// </summary>
		public static string ByteArrayToHexNumberWithBitLength(byte[] code, int bitLength)
		{
			if (code == null || code.Length == 0)
				throw new InvalidOperationException("Input byte array 'code' cannot be null or empty.");

			if (code.Length > 32 || bitLength > 256)
				throw new InvalidOperationException("Input byte array 'code' is too large, maximum capacity is 256 bits.");

			if(bitLength < 1)
				throw new InvalidOperationException("Input 'bitLength' is invalid, must be between 1 and 256 bits.");

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < code.Length && bitLength > 0; i++)
			{
				var b = code[i];

				var mask = 0xFF;
				if(bitLength < 8)
					mask = mask >> (8 - bitLength % 8);

				b = (byte)(b & mask);

				if(bitLength > 4)
					sb.Insert(0, b.ToString("X2"));
				else
					sb.Insert(0, b.ToString("X"));

				bitLength -= 8;
			}
			return sb.ToString();
		}


		/// <summary>
		/// Converts the array of bytes to its hexadecimal representation. IMPORTANT: Unlike with "HexNumber" functions, HexString functions do not perform a byte reordering.
		/// </summary>
		public static string ByteArrayToHexString(byte[] code, int index, int count)
        {
            StringBuilder sb = new StringBuilder(code.Length * 2);
			var upperBound = index + count;
            for(int i = index; i < upperBound; i++)
                sb.AppendFormat("{0:x2}", code[i]);
            return sb.ToString();
        }


		/// <summary>
		/// Converts the given hexadecimal string to an array of bytes. IMPORTANT: Unlike with "HexNumber" functions, HexString functions do not perform a byte reordering.
		/// </summary>
		/// <param name="stHex"></param>
		/// <returns></returns>
		public static byte[] HexStringToByteArray(string code)
        {
			if (code.Length % 2 != 0)
				throw new Exception("Invalid input, the length of the hexadecimal string must be multiple of 2.");
			byte[] result = new byte[code.Length / 2];
			for(int i = 0; i < result.Length; i++)
			{
				var hexByte = code.Substring(i * 2, 2);
				result[i] = Convert.ToByte(hexByte, 16);
			}
			return result;
        }


		/// <summary>
		/// Converts the given Int32 into an array of bytes
		/// </summary>
		public static byte[] Int32ToByteArray(int value)
		{
			byte[] arr = new byte[4];
			int rshift = 0;
			for (int i = 0; i < 4; i++)
			{
				arr[i] = (byte)((value >> rshift) & 0xFF);
				rshift += 8;
			}
			return arr;
		}


		/// <summary>
		/// Converts the array of bytes into an Int32
		/// </summary>
		public static long ByteArrayToInt32(byte[] arr, int idx)
		{
			int v = 0;
			int lshift = 0;
			int b = arr.Length - idx;
			if (b > 4) b = 4;
			for (int i = 0; i < b; i++)
			{
				v = v + (((int)arr[i + idx]) << lshift);
				lshift += 8;
			}
			return v;
		}



		/// <summary>
		/// Converts the given Int64 into an array of bytes
		/// </summary>
		public static byte[] Int64ToByteArray(long v)
		{
			byte[] arr = new byte[8];
			int rshift = 0;
			for (int i = 0; i < 8; i++)
			{
				arr[i] = (byte)((v >> rshift) & 0xFF);
				rshift += 8;
			}
			return arr;
		}


		/// <summary>
		/// Converts the array of bytes into an Int64
		/// </summary>
		public static long ByteArrayToInt64(byte[] arr, int idx)
		{
			long v = 0;
			int lshift = 0;
			int b = arr.Length - idx;
			if (b > 8) b = 8;
			for (int i = 0; i < b; i++)
			{
				v = v + (long)(((long)arr[i + idx]) << lshift);
				lshift += 8;
			}
			return v;
		}



		/// <summary>
		/// Extracts "bitCount" bits from the given array starting at bit "startBit".
		/// Then converts the extracted bits into an Int32.
		/// </summary>
		public static int MakeInt32(byte[] arr, int startBit, int bitCount)
		{
			if (bitCount == 0) return 0;
			if (bitCount > 32) throw new ArgumentOutOfRangeException("bitCount cannot be greater than 32", "bitCount");
			int startByte = startBit / 8;
			int max = (startBit + bitCount) / 8;
			if (max == arr.Length) max--;
			if (arr.Length <= max) throw new ArgumentException("Size of arr param must be at least " + (max + 1) + " bytes to do the requested operation.");
			int v = (int)arr[startByte];
			int rshift = (startBit % 8);
			int result = v >> rshift;
			int lshift = 8 - rshift;
			for (int i = startByte + 1; i <= max; i++, lshift += 8)
			{
				v = ((int)arr[i]) << lshift;
				result = result | v;
			}
			rshift = 32 - bitCount;
			int mask = (int)(0xFFFFFFFF >> rshift);
			result = (result & mask);
			return result;
		}


		/// <summary>
		/// Extracts "bitCount" bits from the given array starting at bit "startBit".
		/// Then converts the extracted bits into an Int64.
		/// </summary>
		public static long MakeInt64(byte[] arr, int startBit, int bitCount)
		{
			if (bitCount == 0) return 0L;
			if (bitCount > 64) throw new ArgumentOutOfRangeException("bitCount cannot be greater than 64", "bitCount");
			int startByte = startBit / 8;
			int max = (startBit + bitCount) / 8;
			if (max == 8) max--;
			if (arr.Length <= max) throw new ArgumentException("Size of arr param must be at least " + max + " bytes to do the requested operation.");
			long v = (long)arr[startByte];
			int rshift = (startBit % 8);
			long result = v >> rshift;
			int lshift = 8 - rshift;
			for (int i = startByte + 1; i <= max; i++, lshift += 8)
			{
				result |= ((long)arr[i] << lshift);
			}
			rshift = 64 - bitCount;
			long mask = (long)(0xFFFFFFFFFFFFFFFFL >> rshift);
			result = (result & mask);
			return result;
		}


		/// <summary>
		/// Extracts "bitCount" bits from the given array starting at "startBit", places the extracted bits in another array and returns it.
		/// </summary>
		public static byte[] ExtractBits(byte[] code, int startBit, int bitCount)
		{
			BitArray bits = new BitArray(bitCount, false);
			int byteIndex = startBit / 8;
			int bitSelector = 0x01 << startBit % 8;
			for (int i = 0; i < bitCount; i++)
			{
				bits.Set(i, (code[byteIndex] & bitSelector) != 0);
				bitSelector <<= 1;
				if (bitSelector >= 0x100)
				{
					bitSelector = 0x01;
					byteIndex++;
				}
			}
			byte[] res = new byte[Convert.ToInt32(Math.Ceiling((double)bitCount / 8))];
			bits.CopyTo(res, 0);
			return res;
		}



		/// <summary>
		/// Copies bits from the specified value into the BitArray starting at the specified startBit, it copies up to bitLen bits.
		/// Note: Always takes the least significative bits first.
		/// </summary>
		public static void SetBits(BitArray bits, int startBit, int bitLen, long value)
		{
			if (!CheckRange(bitLen, value)) throw new ArgumentOutOfRangeException("Value", "Can't fit the given value in a " + bitLen + " bit field.");
			long bitSelector = 0x1;
			for (int i = 0; i < bitLen; i++)
			{
				bits.Set(startBit + i, ((value & bitSelector) > 0));
				bitSelector = bitSelector << 1;
			}
		}



		/// <summary>
		/// Copies bits from the specified byte array (value) into the BitArray starting at the specified startBit, it copies up to bitLen bits.
		/// Note: Always copies the least significative bits first.
		/// </summary>
		public static void SetBits(BitArray bits, int startBit, int bitLen, byte[] value)
		{
			byte bitSelector = 0x1;
			byte v;
			for (int i = 0; i < bitLen; i++)
			{
				if (i % 8 == 0) bitSelector = 0x1;
				if (i / 8 >= value.Length) break;
				v = value[i / 8];
				bits.Set(startBit + i, ((v & bitSelector) > 0));
				bitSelector = (byte)(bitSelector << 1);
			}
		}


		/// <summary>
		/// Ensures the string can be interpreted as a Decimal integer number.
		/// </summary>
		public static bool IsNumber(string str)
		{
			string ValidChars = "0123456789";
			if (str.Length > 0 && str.Length <= 18)
			{
				for (int i = 0; i < str.Length; i++)
				{
					if (ValidChars.IndexOf(str[i]) < 0) return false;
				}
				return true;
			}
			return false;
		}


		/// <summary>
		/// Ensures the string is a valid hexadecimal string
		/// </summary>
		public static bool IsHexadecimal(string str)
		{
			if (String.IsNullOrWhiteSpace(str))
				return false;
			string ValidChars = "0123456789abcdef";
			str = str.ToLower();
			for (int i = 0; i < str.Length; i++)
				if (ValidChars.IndexOf(str[i]) < 0) return false;
			return true;
		}


		/// <summary>
		/// Ensures that the value specified can be represented within the range admited by the specified bit len.
		/// </summary>
		public static bool CheckRange(int bitLen, long value)
		{
			if (bitLen <= 64)
			{
				long max = Convert.ToInt64(Math.Pow(2, bitLen)) - 1;
				return (value <= max);
			}
			return false;
		}


		/// <summary>
		/// Compares each byte from the arrays, returns:
		///		- 0 if both arrays are equal. 
		///		- positive if Arr2 is greater than Arr1
		///		- negative if Arr1 is greater than Arr2
		/// </summary>
		public static int CompareByteArrays(byte[] arr1, byte[] arr2)
		{
			if (arr1 == null || arr2 == null) throw new Exception("Arguments cannot be null.");
			int delta = arr2.Length - arr1.Length;
			if (delta != 0)
				return delta;
			int max = arr1.Length;
			for (int i = 0; i < max; i++)
			{
				delta = arr2[i] - arr1[i];
				if (delta != 0)
					return delta;
			}
			return 0;
		}
	}
}
