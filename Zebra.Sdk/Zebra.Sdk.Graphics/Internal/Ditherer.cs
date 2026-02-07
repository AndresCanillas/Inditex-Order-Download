using System;
using System.IO;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Graphics.Internal
{
	internal class Ditherer
	{
		public Ditherer()
		{
		}

		public static int GetPixelWidthFromWidth(int width)
		{
			return (width + 7) / 8 * 8;
		}

		public static int[] GetZebraSpecificPngHeader(int width, int height)
		{
			int pixelWidthFromWidth = Ditherer.GetPixelWidthFromWidth(width);
			return new int[] { pixelWidthFromWidth >> 8 & 255, pixelWidthFromWidth & 255, height >> 8 & 255, height & 255 };
		}

		public static void WriteDitheredContents(Stream inputStream, Stream outputStream)
		{
			using (ZebraImageInternal zebraImageInternal = (ZebraImageInternal)ReflectionUtil.InvokeZebraImageFactory_GetImage(inputStream))
			{
				int[] zebraSpecificPngHeader = Ditherer.GetZebraSpecificPngHeader(zebraImageInternal.Width, zebraImageInternal.Height);
				for (int i = 0; i < (int)zebraSpecificPngHeader.Length; i++)
				{
					outputStream.WriteByte((byte)zebraSpecificPngHeader[i]);
				}
				DitheredImageProvider.GetDitheredImage(zebraImageInternal, new NaughtyBitOutputStream(outputStream));
			}
		}
	}
}