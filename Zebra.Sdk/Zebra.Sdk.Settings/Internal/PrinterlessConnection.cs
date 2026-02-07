using System;
using System.IO;
using System.Text;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Settings.Internal
{
	internal class PrinterlessConnection : ConnectionWithWriteLogging, Connection
	{
		private MemoryStream baos = new MemoryStream();

		private byte[] dataToReturnOnEveryRead;

		private BinaryWriter myWriteLogStream;

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		public int MaxTimeoutForRead
		{
			get
			{
				return 10;
			}
			set
			{
			}
		}

		public string SimpleConnectionName
		{
			get
			{
				return null;
			}
		}

		public int TimeToWaitForMoreData
		{
			get
			{
				return 10;
			}
			set
			{
			}
		}

		public PrinterlessConnection()
		{
		}

		public PrinterlessConnection(byte[] dataToReturnOnEveryRead)
		{
			this.dataToReturnOnEveryRead = dataToReturnOnEveryRead;
		}

		public void AddWriteLogStream(BinaryWriter logStream)
		{
			this.myWriteLogStream = logStream;
		}

		public int BytesAvailable()
		{
			if (this.dataToReturnOnEveryRead == null)
			{
				return 0;
			}
			return (int)this.dataToReturnOnEveryRead.Length;
		}

		public void Close()
		{
		}

		public ConnectionReestablisher GetConnectionReestablisher(long thresholdTime)
		{
			return null;
		}

		public string GetStuffWrittenOnConnection()
		{
			return Encoding.UTF8.GetString(this.baos.ToArray());
		}

		public void Open()
		{
		}

		public byte[] Read()
		{
			byte[] numArray = null;
			if (this.dataToReturnOnEveryRead != null)
			{
				numArray = this.dataToReturnOnEveryRead;
				this.dataToReturnOnEveryRead = null;
			}
			return numArray;
		}

		public void Read(BinaryWriter destinationStream)
		{
		}

		public int ReadChar()
		{
			return -1;
		}

		public byte[] SendAndWaitForResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, string terminator)
		{
			this.Write(dataToSend, 0, (int)dataToSend.Length);
			return this.Read();
		}

		public void SendAndWaitForResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, string terminator)
		{
			this.Write(sourceStream);
		}

		public byte[] SendAndWaitForValidResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator)
		{
			this.Write(dataToSend, 0, (int)dataToSend.Length);
			return this.Read();
		}

		public void SendAndWaitForValidResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator)
		{
		}

		public void WaitForData(int maxTimeout)
		{
		}

		public void Write(byte[] data)
		{
			this.Write(data, 0, (int)data.Length);
			this.WriteToLogStream(data, 0, (int)data.Length);
		}

		public void Write(byte[] data, int offset, int length)
		{
			this.baos.Write(data, offset, length);
			this.WriteToLogStream(data, offset, length);
		}

		public void Write(BinaryReader dataSource)
		{
		}

		private void WriteToLogStream(byte[] buffer, int offset, int numBytes)
		{
			if (this.myWriteLogStream != null)
			{
				try
				{
					this.myWriteLogStream.Write(buffer, offset, numBytes);
				}
				catch (IOException oException)
				{
					throw new LogStreamException(string.Concat("Error writing to log: ", oException.Message));
				}
			}
		}
	}
}