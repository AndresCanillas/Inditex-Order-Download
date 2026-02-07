using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace WebLink.Contracts
{
	public class ImageProcessing
	{
		public static byte[] CreateThumb(byte[] content, int width = 128, int height = 128)
		{
			using (var thumb = new Bitmap(width, height))
			{
				using (MemoryStream ms = new MemoryStream(content))
				{
					using (var image = new Bitmap(ms))
					{
						ImageProcessing.GetAdjustedThumbSize(new Size(image.Width, image.Height), new Size(width, height), out var thumbSize, out var offset);
						using (Graphics g = Graphics.FromImage(thumb))
						{
							g.SmoothingMode = SmoothingMode.AntiAlias;
							g.InterpolationMode = InterpolationMode.HighQualityBicubic;
							g.Clear(Color.White);
							g.DrawImage(image, new Rectangle(offset.X, offset.Y, thumbSize.Width, thumbSize.Height));
						}
						using (MemoryStream dst = new MemoryStream())
						{
							thumb.Save(dst, ImageFormat.Png);
							return dst.ToArray();
						}
					}
				}
			}
		}


		public static byte[] CreateThumb(Stream content, int width = 128, int height = 128)
		{
			using (var thumb = new Bitmap(width, height))
			{
				using (var image = new Bitmap(content))
				{
					ImageProcessing.GetAdjustedThumbSize(new Size(image.Width, image.Height), new Size(width, height), out var thumbSize, out var offset);
					using (Graphics g = Graphics.FromImage(thumb))
					{
						g.SmoothingMode = SmoothingMode.AntiAlias;
						g.InterpolationMode = InterpolationMode.HighQualityBicubic;
						g.Clear(Color.White);
						g.DrawImage(image, new Rectangle(offset.X, offset.Y, thumbSize.Width, thumbSize.Height));
					}
					using (MemoryStream dst = new MemoryStream())
					{
						thumb.Save(dst, ImageFormat.Png);
						return dst.ToArray();
					}
				}
			}
		}


		public static void GetAdjustedThumbSize(Size imageSize, Size thumbSize, out Size adjustedSize, out Point offset)
		{
			float w;
			float h;
			offset = new Point(0, 0);
			float f1 = (float)thumbSize.Width / imageSize.Width;
			float f2 = (float)thumbSize.Height / imageSize.Height;
			if (f1 <= f2)
			{
				w = imageSize.Width * f1;
				h = imageSize.Height * f1;
				offset = new Point(0, (int)(thumbSize.Height / 2 - (h / 2)));
			}
			else
			{
				w = imageSize.Width * f2;
				h = imageSize.Height * f2;
				offset = new Point((int)(thumbSize.Width / 2 - (w / 2)), 0);
			}
			adjustedSize = new Size((int)w, (int)h);
		}
	}
}
