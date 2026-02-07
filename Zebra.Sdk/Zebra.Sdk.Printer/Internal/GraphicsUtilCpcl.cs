using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class GraphicsUtilCpcl : GraphicsUtilA
	{
		protected Connection printerConnection;

		public GraphicsUtilCpcl(Connection printerConnection)
		{
			this.printerConnection = printerConnection;
		}

		public override void PrintImage(ZebraImageI image, int x, int y, int width, int height, bool insideFormat)
		{
			ZebraImageInternal zebraImageInternal = base.ScaleImage(width, height, (ZebraImageInternal)image);
			int num = (image.Width + 7) / 8;
			try
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
				{
					string str = (insideFormat ? "" : string.Concat("! 0 200 200 ", zebraImageInternal.Height, " 1\r\n"));
					string str1 = (insideFormat ? "" : "FORM\r\nPRINT\r\n");
					binaryWriter.Write(Encoding.UTF8.GetBytes(str));
					binaryWriter.Write(Encoding.UTF8.GetBytes("CG "));
					binaryWriter.Write(Encoding.UTF8.GetBytes(Convert.ToString(num)));
					binaryWriter.Write(Encoding.UTF8.GetBytes(" "));
					binaryWriter.Write(Encoding.UTF8.GetBytes(Convert.ToString(zebraImageInternal.Height)));
					binaryWriter.Write(Encoding.UTF8.GetBytes(" "));
					binaryWriter.Write(Encoding.UTF8.GetBytes(Convert.ToString(x)));
					binaryWriter.Write(Encoding.UTF8.GetBytes(" "));
					binaryWriter.Write(Encoding.UTF8.GetBytes(Convert.ToString(y)));
					binaryWriter.Write(Encoding.UTF8.GetBytes(" "));
					this.printerConnection.Write(((MemoryStream)binaryWriter.BaseStream).ToArray());
					using (PrinterConnectionOutputStream printerConnectionOutputStream = new PrinterConnectionOutputStream(this.printerConnection))
					{
						using (Stream compressedBitmapOutputStreamCpcl = new CompressedBitmapOutputStreamCpcl(printerConnectionOutputStream))
						{
							DitheredImageProvider.GetDitheredImage(zebraImageInternal, compressedBitmapOutputStreamCpcl);
						}
					}
					this.printerConnection.Write(Encoding.UTF8.GetBytes(StringUtilities.CRLF));
					this.printerConnection.Write(Encoding.UTF8.GetBytes(str1));
				}
			}
			catch (Exception exception)
			{
				throw new ConnectionException(exception.Message);
			}
		}

		public override void StoreImage(string deviceDriveAndFileName, ZebraImageI image, int width, int height)
		{
			using (PrinterConnectionOutputStream printerConnectionOutputStream = new PrinterConnectionOutputStream(this.printerConnection))
			{
				try
				{
					(new GraphicsConversionUtilCpcl()).SendImageToStream(deviceDriveAndFileName, (ZebraImageInternal)image, width, height, printerConnectionOutputStream);
				}
				catch (IOException oException)
				{
					throw new ConnectionException(oException.Message);
				}
			}
		}
	}
}