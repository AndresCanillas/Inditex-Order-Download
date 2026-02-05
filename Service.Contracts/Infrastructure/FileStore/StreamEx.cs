using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Service.Contracts
{
	public class StreamEx : Stream
	{
		private Stream s;

		public event EventHandler<int> OnWrite;
		public event EventHandler<int> OnRead;
		public event EventHandler OnClosed;

		public StreamEx(Stream s)
		{
			this.s = s;
		}

		public string Name
		{
			get
			{
				if (s is FileStream)
					return (s as FileStream).Name;
				else return null;
			}
		}

		public override bool CanRead
		{
			get { return s.CanRead; }
		}

		public override bool CanSeek
		{
			get { return s.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return s.CanWrite; }
		}

		public override long Length
		{
			get { return s.Length; }
		}

		public override long Position
		{
			get { return s.Position; }
			set { s.Position = value; }
		}

		public override void Flush()
		{
			s.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int rb = s.Read(buffer, offset, count);
			try { OnRead?.Invoke(this, rb); }
			catch { }
			return rb;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return s.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			s.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			s.Write(buffer, offset, count);
			try { OnWrite?.Invoke(this, count); }
			catch { }
		}

		public override void Close()
		{
			base.Close();
			s.Close();
			try { OnClosed?.Invoke(this, EventArgs.Empty); }
			catch { }
		}
	}
}
