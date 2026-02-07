using System;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class GraphicsUtilA : GraphicsUtil
	{
		public GraphicsUtilA()
		{
		}

		public abstract void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat);

		public void PrintImage(string imageFilePath, int x, int y)
		{
			this.PrintImage(imageFilePath, x, y, -1, -1, false);
		}

		public void PrintImage(string imageFilePath, int x, int y, int width, int height, bool insideFormat)
		{
			using (ZebraImageI zebraImageI = ReflectionUtil.InvokeZebraImageFactory_GetImage(imageFilePath))
			{
				this.PrintImage(zebraImageI, x, y, width, height, insideFormat);
			}
		}

		protected ZebraImageInternal ScaleImage(int width, int height, ZebraImageInternal encodedImage)
		{
			encodedImage.ScaleImage(width, height);
			return encodedImage;
		}

		public abstract void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height);

		public void StoreImage(string deviceDriveAndFileName, string imageFullPath, int width, int height)
		{
			using (ZebraImageI zebraImageI = ReflectionUtil.InvokeZebraImageFactory_GetImage(imageFullPath))
			{
				this.StoreImage(deviceDriveAndFileName, zebraImageI, width, height);
			}
		}
	}
}