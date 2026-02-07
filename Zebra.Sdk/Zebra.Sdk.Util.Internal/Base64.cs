using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zebra.Sdk.Util.Internal
{
	internal class Base64
	{
		public Base64()
		{
		}

		internal static Stream ConvertFromBase64(Stream stream)
		{
			return Base64.ConvertFromBase64(stream, false);
		}

		internal static Stream ConvertFromBase64(Stream stream, bool skipHeaderBytes)
		{
			int num = 0;
			List<byte> nums = new List<byte>();
			while (true)
			{
				int num1 = stream.ReadByte();
				num = num1;
				if (num1 == -1)
				{
					break;
				}
				nums.Add((byte)num);
			}
			byte[] array = Convert.FromBase64String(Encoding.UTF8.GetString(nums.ToArray()));
			if (skipHeaderBytes && array[0] == 120 && array[1] == 156)
			{
				array = array.Skip<byte>(2).ToArray<byte>();
			}
			return new MemoryStream(array);
		}
	}
}