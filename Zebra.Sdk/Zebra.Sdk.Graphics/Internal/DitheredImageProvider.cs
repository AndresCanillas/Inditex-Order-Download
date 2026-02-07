using System;
using System.IO;
using Zebra.Sdk.Graphics;

namespace Zebra.Sdk.Graphics.Internal
{
	internal class DitheredImageProvider
	{
		public DitheredImageProvider()
		{
		}

		private static int ConvertByteToGrayscale(int pixelToConvert)
		{
			int num = (pixelToConvert & 65280) >> 8;
			int num1 = pixelToConvert & 255;
			int num2 = (((pixelToConvert & 16711680) >> 16) * 30 + num * 59 + num1 * 11) / 100;
			if (num2 > 255)
			{
				num2 = 255;
			}
			else if (num2 < 0)
			{
				num2 = 0;
			}
			return num2;
		}

		public static void GetDitheredImage(ZebraImageInternal image, Stream outputStream)
		{
			DitheredImageProvider.GetDitheredImage(image.Width, image.Height, image, outputStream);
		}

		protected static void GetDitheredImage(int width, int height, ZebraImageInternal image, Stream outputStream)
		{
			object obj;
			int[] row = image.GetRow(0);
			int[] grayscale = image.GetRow(1);
			int num = width / 8 + (width % 8 == 0 ? 0 : 1);
			int num1 = 8 - width % 8;
			if (num1 == 8)
			{
				num1 = 0;
			}
			byte[] numArray = new byte[num];
			byte num2 = 0;
			for (int i = 0; i < width; i++)
			{
				row[i] = DitheredImageProvider.ConvertByteToGrayscale(row[i]);
			}
			for (int j = 0; j < height; j++)
			{
				for (int k = 0; k < (int)numArray.Length; k++)
				{
					numArray[k] = 0;
				}
				int num3 = 0;
				for (int l = 0; l < width; l++)
				{
					if (l % 8 == 0)
					{
						num2 = 128;
					}
					int num4 = row[l];
					num3 = l / 8;
					if (num4 >= 128)
					{
						obj = 255;
					}
					else
					{
						obj = null;
					}
					byte num5 = (byte)obj;
					numArray[num3] = (byte)(numArray[num3] | num2 & num5);
					int num6 = num4 - (num5 & 255);
					if (l < width - 1)
					{
						row[l + 1] = row[l + 1] + 7 * num6 / 16;
					}
					if (l > 0 && j < height - 1)
					{
						grayscale[l - 1] = grayscale[l - 1] + 3 * num6 / 16;
					}
					if (j < height - 1)
					{
						if (l == 0)
						{
							grayscale[l] = DitheredImageProvider.ConvertByteToGrayscale(grayscale[l]);
						}
						grayscale[l] = grayscale[l] + 5 * num6 / 16;
					}
					if (j < height - 1 && l < width - 1)
					{
						grayscale[l + 1] = DitheredImageProvider.ConvertByteToGrayscale(grayscale[l + 1]);
						grayscale[l + 1] = grayscale[l + 1] + num6 / 16;
					}
					num2 = (byte)((num2 & 255) >> 1);
				}
				numArray[num3] = (byte)(numArray[num3] | 255 >> (8 - num1 & 31));
				outputStream.Write(numArray, 0, (int)numArray.Length);
				row = grayscale;
				grayscale = image.GetRow(j + 2);
			}
		}
	}
}