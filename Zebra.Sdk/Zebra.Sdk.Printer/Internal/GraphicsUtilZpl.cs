using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class GraphicsUtilZpl : GraphicsUtilA
	{
		protected Connection printerConnection;

		public GraphicsUtilZpl(Connection printerConnection)
		{
			this.printerConnection = printerConnection;
		}

		private string GetBodyHeader(int x, int y, bool insideFormat, int widthOfImageInBytes, int sizeOfImageInBytes)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("^FO");
			stringBuilder.Append(x);
			stringBuilder.Append(",");
			stringBuilder.Append(y);
			stringBuilder.Append("^GFA");
			stringBuilder.Append(",");
			stringBuilder.Append(sizeOfImageInBytes);
			stringBuilder.Append(",");
			stringBuilder.Append(sizeOfImageInBytes);
			stringBuilder.Append(",");
			stringBuilder.Append(widthOfImageInBytes);
			stringBuilder.Append(",");
			string str = stringBuilder.ToString();
			if (!insideFormat)
			{
				str = string.Concat("^XA", str);
			}
			return str;
		}

		public override void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat)
		{
			using (ZebraImageInternal zebraImageInternal = base.ScaleImage(width, height, (ZebraImageInternal)image))
			{
				int num = (zebraImageInternal.Width + 7) / 8;
				int num1 = num * zebraImageInternal.Height;
				string str = ZPLUtilities.ReplaceAllWithInternalCharacters(this.GetBodyHeader(x, y, insideFormat, num, num1));
				this.printerConnection.Write(Encoding.UTF8.GetBytes(str));
				using (PrinterConnectionOutputStream printerConnectionOutputStream = new PrinterConnectionOutputStream(this.printerConnection))
				{
					using (Stream compressedBitmapOutputStreamZpl = new CompressedBitmapOutputStreamZpl(printerConnectionOutputStream))
					{
						try
						{
							DitheredImageProvider.GetDitheredImage(zebraImageInternal, compressedBitmapOutputStreamZpl);
						}
						catch (IOException oException)
						{
							throw new ConnectionException(oException.Message);
						}
					}
				}
				if (!insideFormat)
				{
					string str1 = ZPLUtilities.DecorateWithFormatPrefix("^XZ");
					this.printerConnection.Write(Encoding.UTF8.GetBytes(str1));
				}
			}
		}

		public override void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height)
		{
			try
			{
				GraphicsConversionUtilZpl graphicsConversionUtilZpl = new GraphicsConversionUtilZpl();
				using (PrinterConnectionOutputStream printerConnectionOutputStream = new PrinterConnectionOutputStream(this.printerConnection))
				{
					graphicsConversionUtilZpl.SendImageToStream(deviceDriveAndFileName, (ZebraImageInternal)image, width, height, printerConnectionOutputStream);
				}
			}
			catch (IOException oException)
			{
				throw new ConnectionException(oException.Message);
			}
		}
	}
}