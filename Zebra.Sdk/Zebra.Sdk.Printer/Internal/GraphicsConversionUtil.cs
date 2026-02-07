using System;
using System.IO;
using Zebra.Sdk.Graphics.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class GraphicsConversionUtil
	{
		protected GraphicsConversionUtil()
		{
		}

		public abstract void SendImageToStream(string deviceDriveAndFileName, ZebraImageInternal image, int width, int height, Stream graphicOutputStream);
	}
}