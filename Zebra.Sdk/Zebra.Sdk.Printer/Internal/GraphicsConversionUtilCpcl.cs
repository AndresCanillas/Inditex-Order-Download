using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Graphics;
using Zebra.Sdk.Graphics.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class GraphicsConversionUtilCpcl : GraphicsConversionUtil
	{
		public GraphicsConversionUtilCpcl()
		{
		}

		public byte[] CreatePcxHeader(int width, int height)
		{
			byte[] array;
			byte[] numArray = new byte[] { 10, 5, 1, 1 };
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				binaryWriter.Write(numArray);
				binaryWriter.Write(new byte[4]);
				binaryWriter.Write(this.IntegerToLittleEndianByteArray(width - 1));
				binaryWriter.Write(this.IntegerToLittleEndianByteArray(height - 1));
				binaryWriter.Write(new byte[] { 200, 0, 200, 0 });
				for (int i = 0; i < 48; i++)
				{
					binaryWriter.Write((byte)0);
				}
				binaryWriter.Write((byte)0);
				binaryWriter.Write((byte)1);
				int widthOfImage = GraphicsConversionUtilCpcl.GetWidthOfImage(width);
				ushort num = (ushort)(widthOfImage + widthOfImage % 2);
				binaryWriter.Write(this.IntegerToLittleEndianByteArray((int)num));
				binaryWriter.Write(new byte[2]);
				for (int j = 0; j < 58; j++)
				{
					binaryWriter.Write((byte)0);
				}
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		public byte[] CreatePcxImage(int width, int height, byte[] imageBytes)
		{
			byte[] array;
			int widthOfImage = GraphicsConversionUtilCpcl.GetWidthOfImage(width);
			byte[] numArray = (new RleEncodedImage()).RleEncoding(imageBytes, widthOfImage);
			byte[] numArray1 = this.CreatePcxHeader(width, height);
			using (BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream()))
			{
				binaryWriter.Write(numArray1);
				binaryWriter.Write(numArray);
				array = ((MemoryStream)binaryWriter.BaseStream).ToArray();
			}
			return array;
		}

		public byte[] CreatePcxImage(int width, int height, ZebraImageInternal imageData)
		{
			byte[] numArray;
			if (width <= 0)
			{
				width = imageData.Width;
			}
			if (height <= 0)
			{
				height = imageData.Height;
			}
			using (MemoryStream memoryStream = new MemoryStream())
			{
				DitheredImageProvider.GetDitheredImage(imageData, memoryStream);
				numArray = this.CreatePcxImage(width, height, memoryStream.ToArray());
			}
			return numArray;
		}

		private static string GetCorrectedFileName(string deviceDriveAndFileName)
		{
			return string.Concat(FileUtilities.ParseDriveAndExtension(deviceDriveAndFileName).FileName, ".PCX");
		}

		private static int GetWidthOfImage(int width)
		{
			return (width + 7) / 8;
		}

		private byte[] IntegerToLittleEndianByteArray(int data)
		{
			return new byte[] { (byte)data, (byte)(data >> 8 & 255) };
		}

		public override void SendImageToStream(string deviceDriveAndFileName, ZebraImageInternal image, int width, int height, Stream graphicOutputStream)
		{
			image.ScaleImage(width, height);
			byte[] numArray = this.CreatePcxImage(width, height, image);
			string str = "! CISDFCRC16";
			string upper = CpclCrcHeader.GetCRC16ForCertificateFilesOnly(numArray).ToUpper();
			string str1 = StringUtilities.ConvertTo8dot3(GraphicsConversionUtilCpcl.GetCorrectedFileName(deviceDriveAndFileName));
			int length = (int)numArray.Length;
			string places = StringUtilities.StringPadToPlaces(8, "0", length.ToString("X4"));
			string upper1 = CpclCrcHeader.GetWChecksum(numArray).ToUpper();
			byte[] bytes = Encoding.UTF8.GetBytes("\r\n");
			using (MemoryStream memoryStream = new MemoryStream())
			{
				memoryStream.Write(Encoding.UTF8.GetBytes(str), 0, str.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				memoryStream.Write(Encoding.UTF8.GetBytes(upper), 0, upper.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				memoryStream.Write(Encoding.UTF8.GetBytes(str1), 0, str1.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				memoryStream.Write(Encoding.UTF8.GetBytes(places), 0, places.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				memoryStream.Write(Encoding.UTF8.GetBytes(upper1), 0, upper1.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				memoryStream.Write(numArray, 0, (int)numArray.Length);
				memoryStream.Write(bytes, 0, (int)bytes.Length);
				byte[] array = memoryStream.ToArray();
				graphicOutputStream.Write(array, 0, (int)array.Length);
			}
		}
	}
}