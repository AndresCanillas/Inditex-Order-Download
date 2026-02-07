using System;
using System.IO;
using System.Text;

namespace Zebra.Sdk.Util.FileConversion.Internal
{
	internal class HzoToDzConverterStream : MemoryStream, IDisposable
	{
		private Stream sourceStream;

		private bool preambleSkipped;

		public HzoToDzConverterStream(Stream sourceStream)
		{
			this.sourceStream = sourceStream;
			if (sourceStream == null)
			{
				throw new IOException("input stream is null");
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (this.sourceStream != null)
			{
				this.sourceStream.Dispose();
			}
			base.Dispose(disposing);
		}

		public override int ReadByte()
		{
			if (!this.preambleSkipped)
			{
				this.SkipPreamble();
			}
			int num = this.sourceStream.ReadByte();
			if (num == 93)
			{
				while (-1 != num)
				{
					num = this.sourceStream.ReadByte();
				}
			}
			return num;
		}

		private void SkipPreamble()
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = this.sourceStream.ReadByte();
			if (-1 == num)
			{
				throw new IOException("File not found");
			}
			while (num != -1)
			{
				stringBuilder.AppendFormat("{0}", Convert.ToChar(num));
				if (stringBuilder.ToString().EndsWith("<![CDATA["))
				{
					break;
				}
				num = this.sourceStream.ReadByte();
			}
			this.preambleSkipped = true;
		}
	}
}