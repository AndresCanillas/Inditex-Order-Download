using System;
using System.IO;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Util.Internal
{
	internal class HasWrite
	{
		private object localWriter;

		public HasWrite(object targetStream)
		{
			this.localWriter = targetStream;
		}

		public void Write(byte[] data, int offset, int length)
		{
			if (this.localWriter is Connection)
			{
				try
				{
					((Connection)this.localWriter).Write(data, offset, length);
				}
				catch (ConnectionException connectionException1)
				{
					ConnectionException connectionException = connectionException1;
					throw new IOException(connectionException.Message, connectionException);
				}
				return;
			}
			if (!(this.localWriter is Stream))
			{
				throw new Exception("OutputStream is null");
			}
			((Stream)this.localWriter).Write(data, offset, length);
		}
	}
}