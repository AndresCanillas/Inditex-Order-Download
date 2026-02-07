using System;
using System.IO;
using System.Reflection;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class GrfToPrinterPngConverterStream : MemoryStream, IDisposable
	{
		private Stream grfStream;

		private Stream pngStream;

		private int headerCount;

		private int[] zebraHeader = new int[4];

		public GrfToPrinterPngConverterStream(Stream grfStream)
		{
			this.grfStream = grfStream;
		}

		protected override void Dispose(bool disposing)
		{
			if (this.pngStream != null)
			{
				this.pngStream.Dispose();
			}
			base.Dispose(disposing);
		}

		private byte[] GrfToPng(Stream grfStream2, int width, int height)
		{
			byte[] numArray;
			AssemblyName[] referencedAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
			for (int i = 0; i < (int)referencedAssemblies.Length; i++)
			{
				string name = referencedAssemblies[i].Name;
				if (name.Contains("SdkApi"))
				{
					try
					{
						numArray = (byte[])Assembly.Load(new AssemblyName(name)).GetType("Zebra.Sdk.Graphics.Internal.GrfToPngConverter", true, true).GetTypeInfo().GetMethod("GrfToPng", new Type[] { typeof(Stream), typeof(int), typeof(int) }).Invoke(null, new object[] { grfStream2, width, height });
						return numArray;
					}
					catch
					{
					}
				}
			}
			return null;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int num = this.ReadByte();
			if (num == -1)
			{
				return -1;
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
			if (this.headerCount < 4)
			{
				int num = this.grfStream.ReadByte();
				int[] numArray = this.zebraHeader;
				int num1 = this.headerCount;
				this.headerCount = num1 + 1;
				numArray[num1] = num;
				return num;
			}
			if (this.headerCount == 4)
			{
				int num2 = (this.zebraHeader[0] << 8) + this.zebraHeader[1];
				int num3 = (this.zebraHeader[2] << 8) + this.zebraHeader[3];
				this.headerCount++;
				byte[] png = this.GrfToPng(this.grfStream, num2, num3);
				this.pngStream = new MemoryStream(png);
			}
			return this.pngStream.ReadByte();
		}
	}
}