using System;
using System.Text;

namespace Zebra.Sdk.Util.Internal
{
	internal class PacketParsingUtil
	{
		public PacketParsingUtil()
		{
		}

		public static string ParseAddress(byte[] data, int offset, int length)
		{
			StringBuilder stringBuilder = new StringBuilder("");
			for (int i = offset; i < offset + length; i++)
			{
				stringBuilder.Append(Convert.ToString(Convert.ToInt32(data[i])));
				if (i + 1 != offset + length)
				{
					stringBuilder.Append(".");
				}
			}
			return stringBuilder.ToString();
		}

		public static bool ParseBoolean(byte[] data, int offset, int length)
		{
			for (int i = offset; i < offset + length; i++)
			{
				if (data[offset] == 1)
				{
					return true;
				}
			}
			return false;
		}

		public static string ParseGeneralByte(byte[] data, int offset, int length)
		{
			StringBuilder stringBuilder = new StringBuilder("");
			string str = "";
			for (int i = offset; i < offset + length; i++)
			{
				int num = Convert.ToInt32(data[i]);
				str = num.ToString("X2");
				if (str.Length == 1)
				{
					stringBuilder.Append("0");
				}
				stringBuilder.Append(str);
			}
			return stringBuilder.ToString();
		}

		public static string ParseGeneralString(byte[] data, int offset, int length)
		{
			int num = 0;
			for (int i = offset; i < offset + length && data[i] != 0; i++)
			{
				num++;
			}
			return Encoding.UTF8.GetString(data, offset, num);
		}

		public static int ParseInteger(byte[] data, int offset, int length)
		{
			int num = 0;
			int num1 = 8 * (length - 1);
			for (int i = offset; i < offset + length; i++)
			{
				num = num | data[i] << (num1 & 31) & 255 << (num1 & 31);
				num1 -= 8;
			}
			return num;
		}
	}
}