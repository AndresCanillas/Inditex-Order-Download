using System;
using Zebra.Sdk.Graphics.Internal;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class DYHelper
	{
		public DYHelper()
		{
		}

		public static int[] CalculateZebraHeader(int[] pngDataHeader, int bytesPerRow, int totalBytesInData)
		{
			int[] numArray = DYHelper.CalculateZebraHeader(pngDataHeader) ?? DYHelper.CalculateZebraHeader(bytesPerRow, totalBytesInData);
			return numArray;
		}

		public static int[] CalculateZebraHeader(int[] pngDataHeader)
		{
			int[] zebraSpecificPngHeader = null;
			if (DYHelper.IsPcPng(pngDataHeader))
			{
				zebraSpecificPngHeader = new int[4];
				int intFromBytes = DYHelper.GetIntFromBytes(pngDataHeader[16], pngDataHeader[17], pngDataHeader[18], pngDataHeader[19]);
				int num = DYHelper.GetIntFromBytes(pngDataHeader[20], pngDataHeader[21], pngDataHeader[22], pngDataHeader[23]);
				zebraSpecificPngHeader = Ditherer.GetZebraSpecificPngHeader(intFromBytes, num);
			}
			return zebraSpecificPngHeader;
		}

		public static int[] CalculateZebraHeader(int bytesPerRow, int totalBytesInData)
		{
			return Ditherer.GetZebraSpecificPngHeader(bytesPerRow * 8, totalBytesInData / bytesPerRow);
		}

		private static int GetIntFromBytes(int b1, int b2, int b3, int b4)
		{
			return (255 & b1) << 24 | (255 & b2) << 16 | (255 & b3) << 8 | 255 & b4;
		}

		private static bool IsPcPng(int[] pngDataHeader)
		{
			if ((int)pngDataHeader.Length < 24 || pngDataHeader[0] != 137 || pngDataHeader[1] != 80 || pngDataHeader[2] != 78)
			{
				return false;
			}
			return pngDataHeader[3] == 71;
		}
	}
}