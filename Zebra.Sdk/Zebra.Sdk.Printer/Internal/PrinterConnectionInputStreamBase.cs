using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal abstract class PrinterConnectionInputStreamBase : MemoryStream
	{
		private Connection printerConnection;

		private DateTime lastDataReceivedTime;

		private StringBuilder terminationQueue;

		private bool endStreamOnNextRead;

		private long maxTimeToWaitForMoreData;

		protected string terminator;

		public PrinterConnectionInputStreamBase(Connection printerConnection, long maxTimeToWaitForMoreData)
		{
			this.printerConnection = printerConnection;
			this.maxTimeToWaitForMoreData = maxTimeToWaitForMoreData;
			this.terminator = null;
			this.terminationQueue = new StringBuilder();
			this.lastDataReceivedTime = DateTime.Now;
		}

		private void HandleTerminationOfStream(char retVal)
		{
			if (this.terminator != null)
			{
				if (this.terminationQueue.Length >= this.terminator.Length)
				{
					this.terminationQueue.Remove(0, 1);
				}
				this.terminationQueue.Append(retVal);
				if (this.terminator.Equals(this.terminationQueue.ToString()))
				{
					this.endStreamOnNextRead = true;
				}
			}
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
			TimeSpan now;
			int num = -1;
			if (this.endStreamOnNextRead)
			{
				return -1;
			}
			try
			{
				do
				{
					num = this.printerConnection.ReadChar();
					now = DateTime.Now - this.lastDataReceivedTime;
					if (num == -1)
					{
						Sleeper.Sleep((long)50);
					}
					else
					{
						this.lastDataReceivedTime = DateTime.Now;
						this.SetTerminatorBasedOnData(num);
						this.HandleTerminationOfStream((char)num);
					}
				}
				while (num == -1 && now.TotalMilliseconds < (double)this.maxTimeToWaitForMoreData);
			}
			catch (ConnectionException connectionException)
			{
				throw new Exception(connectionException.GetBaseException().ToString());
			}
			return num;
		}

		protected abstract void SetTerminatorBasedOnData(int b);
	}
}