using System;
using System.IO;
using System.Text;

namespace Services.Core
{
	delegate void StreamWriterRedirectorEvent(string text);

	class StreamWriterRedirector : Stream
	{
		private string data;
		private StreamWriterRedirectorEvent onlinewritten;

		public StreamWriterRedirector(StreamWriterRedirectorEvent onlinewritten)
		{
			this.onlinewritten = onlinewritten;
			data = "";
		}

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override void Flush()
		{
			if(data.Length != 0)
			{
				onlinewritten(data);
				data = "";
			}
		}

		public override long Length
		{
			get { return 0; }
		}

		public override long Position
		{
			get { return 0; }
			set { }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException();
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}


		public override void Write(byte[] buffer, int offset, int count)
		{
			int idx;
			data += Encoding.UTF8.GetString(buffer, offset, count);
			do
			{
				if(data.Length == 0) return;
				idx = data.IndexOfAny(new char[] { '\r', '\n' });
				if(idx >= 0)
				{
					string line = data.Substring(0, idx);
					onlinewritten(line.Replace("{", "{{").Replace("}", "}}"));
					if(data.Length > idx + 1 && data[idx + 1] == '\n')
						idx++;
					data = data.Substring(idx + 1);
				}
			} while(idx >= 0 && data.Length > 0);
		}
	}
}
