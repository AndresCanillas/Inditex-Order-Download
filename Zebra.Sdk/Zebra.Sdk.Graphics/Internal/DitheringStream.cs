using System;
using System.IO;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Graphics.Internal
{
	internal class DitheringStream : MemoryStream
	{
		private ZebraImageInternal image;

		private int[] headerInfo;

		private int headerIndex;

		private MemoryStream tempDitheredImageBuffer;

		public DitheringStream(Stream sourceStream) : this((ZebraImageInternal)ReflectionUtil.InvokeZebraImageFactory_GetImage(sourceStream))
		{
		}

		public DitheringStream(ZebraImageInternal imageArg)
		{
			this.image = imageArg;
			this.ProcessImage();
		}

		protected override void Dispose(bool disposing)
		{
			if (this.tempDitheredImageBuffer != null)
			{
				this.tempDitheredImageBuffer.Dispose();
			}
			if (this.image != null)
			{
				this.image.Dispose();
			}
			base.Dispose(disposing);
		}

		private void ProcessImage()
		{
			int width = this.image.Width;
			int height = this.image.Height;
			this.headerInfo = Ditherer.GetZebraSpecificPngHeader(width, height);
			this.tempDitheredImageBuffer = new MemoryStream();
			DitheredImageProvider.GetDitheredImage(this.image, new NaughtyBitOutputStream(this.tempDitheredImageBuffer));
			this.tempDitheredImageBuffer.Position = (long)0;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int num = this.ReadByte();
			if (num == -1)
			{
				return 0;
			}
			buffer[offset] = (byte)num;
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
					buffer[offset + num1] = (byte)num;
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
			int num = -1;
			if (this.headerIndex >= (int)this.headerInfo.Length)
			{
				num = this.tempDitheredImageBuffer.ReadByte();
			}
			else
			{
				int[] numArray = this.headerInfo;
				int num1 = this.headerIndex;
				this.headerIndex = num1 + 1;
				num = numArray[num1];
			}
			return num;
		}
	}
}