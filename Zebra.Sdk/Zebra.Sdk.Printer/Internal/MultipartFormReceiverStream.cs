using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Printer.Internal
{
	internal class MultipartFormReceiverStream : PrinterConnectionInputStreamBase, IDisposable
	{
		private long numBytesRead;

		private MemoryStream boundaryBuffer = new MemoryStream();

		public MultipartFormReceiverStream(Connection printerConnection, long maxTimeToWaitForMoreData) : base(printerConnection, maxTimeToWaitForMoreData)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (this.boundaryBuffer != null)
			{
				this.boundaryBuffer.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void SetTerminatorBasedOnData(int b)
		{
			this.numBytesRead += (long)1;
			if (this.numBytesRead < (long)100)
			{
				this.boundaryBuffer.WriteByte(byte.Parse(b.ToString()));
				return;
			}
			if (this.terminator == null)
			{
				Match match = (new Regex("^[\\s]*--([^\\s|^-]+)\\r\\n")).Match(Encoding.UTF8.GetString(this.boundaryBuffer.ToArray()));
				if (match.Length > 0)
				{
					this.terminator = string.Format("--{0}--\r\n", match.Groups[1].ToString());
				}
			}
		}
	}
}