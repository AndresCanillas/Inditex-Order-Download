using System;
using System.IO;
using System.Reflection;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class PrinterPngToGrfConverterStream : MemoryStream, IDisposable
	{
		private Stream pngStream;

		private Stream grfStream;

		private int headerCount;

		private int[] zebraHeader = new int[4];

		public PrinterPngToGrfConverterStream(Stream pngStream)
		{
			this.pngStream = pngStream;
		}

		protected override void Dispose(bool disposing)
		{
			if (this.grfStream != null)
			{
				this.grfStream.Dispose();
			}
			base.Dispose(disposing);
		}

		private byte[] PngToGrf(Stream pngStream2)
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
						numArray = (byte[])Assembly.Load(new AssemblyName(name)).GetType("Zebra.Sdk.Graphics.Internal.PngToGrfConverter", true, true).GetTypeInfo().GetMethod("PngToGrf", new Type[] { typeof(Stream) }).Invoke(null, new object[] { pngStream2 });
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
				int num = this.pngStream.ReadByte();
				int[] numArray = this.zebraHeader;
				int num1 = this.headerCount;
				this.headerCount = num1 + 1;
				numArray[num1] = num;
				return num;
			}
			if (this.headerCount == 4)
			{
				this.headerCount++;
				this.grfStream = new MemoryStream(this.PngToGrf(this.pngStream));
			}
			return this.grfStream.ReadByte();
		}
	}
}