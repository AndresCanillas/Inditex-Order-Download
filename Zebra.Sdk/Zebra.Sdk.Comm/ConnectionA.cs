using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Abstract class which implements the default functionality of <c>Connection</c>.
	///       </summary>
	public abstract class ConnectionA : ConnectionWithWriteLogging, Connection
	{
		internal readonly static int DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA;

		internal readonly static int DEFAULT_MAX_TIMEOUT_FOR_READ;

		private int MAX_DATA_TO_WRITE_TO_STREAM_AT_ONCE = 1024;

		protected static int SIZE_OF_STREAM_BUFFERS;

		protected int maxTimeoutForRead;

		protected int timeToWaitForMoreData;

		protected ZebraSocket commLink;

		protected BinaryWriter outputStream;

		protected BinaryReader inputStream;

		protected bool isDeviceConnected;

		protected ZebraConnector zebraConnector;

		protected BinaryWriter myWriteLogStream;

		/// <summary>
		///       Returns true if the connection is open.
		///       </summary>
		public virtual bool Connected
		{
			get
			{
				return this.isDeviceConnected;
			}
		}

		/// <summary>
		///       See the classes which implement this property for the format of the printer manufacturer string.
		///       </summary>
		public virtual string Manufacturer
		{
			get
			{
				return "";
			}
		}

		/// <summary>
		///       Gets or sets the maximum number of bytes to write at one time
		///       </summary>
		public virtual int MaxDataToWrite
		{
			get
			{
				return this.MAX_DATA_TO_WRITE_TO_STREAM_AT_ONCE;
			}
			set
			{
				this.MAX_DATA_TO_WRITE_TO_STREAM_AT_ONCE = value;
			}
		}

		/// <summary>
		///       Gets or sets the maximum time, in milliseconds, to wait for any data to be received.
		///       </summary>
		public virtual int MaxTimeoutForRead
		{
			get
			{
				return this.maxTimeoutForRead;
			}
			set
			{
				this.maxTimeoutForRead = value;
			}
		}

		/// <summary>
		///       Gets a human-readable description of the connection.
		///       </summary>
		public virtual string SimpleConnectionName
		{
			get
			{
				return "";
			}
		}

		/// <summary>
		///       Gets or sets the maximum time, in milliseconds, to wait in-between reads after the initial read.
		///       </summary>
		public virtual int TimeToWaitForMoreData
		{
			get
			{
				return this.timeToWaitForMoreData;
			}
			set
			{
				this.timeToWaitForMoreData = value;
			}
		}

		static ConnectionA()
		{
			ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA = 500;
			ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ = 5000;
			ConnectionA.SIZE_OF_STREAM_BUFFERS = 16384;
		}

		protected ConnectionA()
		{
		}

		/// <summary>
		///       Sets the stream to log the write data to.
		///       </summary>
		/// <param name="logStream">The stream to log the data to.</param>
		public virtual void AddWriteLogStream(BinaryWriter logStream)
		{
			this.myWriteLogStream = logStream;
		}

		/// <summary>
		///       Returns an estimate of the number of bytes that can be read from this connection without blocking.
		///       </summary>
		/// <returns>The estimated number of bytes available.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual int BytesAvailable()
		{
			int num;
			try
			{
				if (!this.inputStream.BaseStream.CanRead)
				{
					num = 0;
				}
				else
				{
					num = (!this.inputStream.BaseStream.CanSeek ? ConnectionA.SIZE_OF_STREAM_BUFFERS : (int)(this.inputStream.BaseStream.Length - this.inputStream.BaseStream.Position));
				}
			}
			catch (Exception exception)
			{
				throw new ConnectionException(exception.Message);
			}
			return num;
		}

		/// <summary>
		///       Closes this connection and releases any system resources associated with the connection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void Close()
		{
			if (!this.isDeviceConnected)
			{
				return;
			}
			this.isDeviceConnected = false;
			try
			{
				this.outputStream.Dispose();
				this.inputStream.Dispose();
				this.commLink.Close();
			}
			catch (Exception exception)
			{
				throw new ConnectionException(string.Concat("Could not disconnect from device: ", exception.Message));
			}
		}

		/// <summary>
		///       Returns a <c>ConnectionReestablisher</c> which allows for easy recreation of a connection which may have been closed.
		///       </summary>
		/// <param name="thresholdTime">How long the Connection reestablisher will wait before attempting to reconnection to the printer.</param>
		/// <returns>Instance of <see cref="T:Zebra.Sdk.Comm.ConnectionReestablisher" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the ConnectionReestablisher could not be created.</exception>
		public virtual ConnectionReestablisher GetConnectionReestablisher(long thresholdTime)
		{
			throw new ConnectionException("Automatic reconnection is not supported for this connection type");
		}

		/// <summary>
		///       Opens the connection to a device.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection cannot be established.</exception>
		public virtual void Open()
		{
			if (this.isDeviceConnected)
			{
				return;
			}
			try
			{
				this.commLink = this.zebraConnector.Open();
				this.outputStream = this.commLink.GetOutputStream();
				this.inputStream = this.commLink.GetInputStream();
				this.isDeviceConnected = true;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				this.isDeviceConnected = false;
				throw new ConnectionException(string.Concat("Could not connect to device: ", exception.GetBaseException().Message ?? exception.Message), exception);
			}
		}

		/// <summary>
		///       Reads all the available data from the connection. This call is non-blocking.
		///       </summary>
		/// <returns>Data read from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual byte[] Read()
		{
			return this.Read(-1);
		}

		/// <summary>
		///       Reads <c>maxBytesToRead</c> of the available data from the connection.
		///       </summary>
		/// <param name="maxBytesToRead">number of bytes to read</param>
		/// <returns>the bytes read from the connection</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		public virtual byte[] Read(int maxBytesToRead)
		{
			byte[] numArray = null;
			int num = this.BytesAvailable();
			if (num > 0)
			{
				numArray = new byte[(maxBytesToRead < 0 ? num : Math.Min(maxBytesToRead, num))];
				try
				{
					int num1 = this.inputStream.Read(numArray, 0, (int)numArray.Length);
					Array.Resize<byte>(ref numArray, num1);
				}
				catch (Exception exception)
				{
					throw new ConnectionException(exception.Message);
				}
			}
			return numArray;
		}

		/// <summary>
		///       Reads all the available data from the connection.
		///       </summary>
		/// <param name="destinationStream">For read data.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void Read(BinaryWriter destinationStream)
		{
			while (this.BytesAvailable() > 0)
			{
				try
				{
					byte[] numArray = this.Read(16384);
					destinationStream.Write(numArray, 0, (int)numArray.Length);
				}
				catch (Exception exception)
				{
					throw new ConnectionException(exception.Message);
				}
			}
		}

		/// <summary>
		///       Reads <c>maxBytesToRead</c> of the available data from the connection.
		///       </summary>
		/// <param name="maxBytesToRead">number of bytes to read</param>
		/// <param name="exitOnFirstRead">true to exit on first data read</param>
		/// <returns>the bytes read from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		public virtual byte[] Read(int maxBytesToRead, bool exitOnFirstRead)
		{
			int num = 0;
			int num1 = 0;
			int num2 = maxBytesToRead;
			int num3 = 0;
			byte[] numArray = null;
			byte[] numArray1 = new byte[maxBytesToRead];
			do
			{
				try
				{
					num1 = this.inputStream.Read(numArray1, num, num2);
					if (num1 > 0)
					{
						num3 += num1;
						num += num1;
						num2 -= num1;
					}
					if (exitOnFirstRead && num3 > 0)
					{
						break;
					}
				}
				catch (Exception exception)
				{
					throw new ConnectionException(exception.Message);
				}
			}
			while (num3 < maxBytesToRead);
			if (num3 > 0)
			{
				numArray = new byte[num3];
				Array.Copy(numArray1, 0, numArray, 0, num3);
			}
			return numArray;
		}

		/// <summary>
		///       Reads the next byte of data from the connection.
		///       </summary>
		/// <returns>The next byte from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual int ReadChar()
		{
			int num;
			try
			{
				num = this.inputStream.ReadByte();
			}
			catch (Exception exception)
			{
				throw new ConnectionException(exception.Message);
			}
			return num;
		}

		/// <summary>
		///       Sends <c>dataToSend</c> and returns the response data.
		///       </summary>
		/// <param name="dataToSend">Byte array of data to send</param>
		/// <param name="initialResponseTimeout">The maximum time, in milliseconds, to wait for the initial response to be received. 
		///       If no data is received during this time, the function returns a zero length array.</param>
		/// <param name="responseCompletionTimeout">After the initial response, if no data is received for this period of time, the 
		///       input is considered complete and the method returns.</param>
		/// <param name="terminator">If the response contains this string, the input is considered complete and the method returns. 
		///       May be used to avoid waiting for more data when the response is always terminated with a known string. Use <c>null</c> 
		///       if no terminator is desired.</param>
		/// <returns>The received data.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual byte[] SendAndWaitForResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, string terminator)
		{
			byte[] array;
			if (!this.Connected)
			{
				throw new ConnectionException("No Printer Connection");
			}
			this.Write(dataToSend);
			this.WaitForData(initialResponseTimeout);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				while (this.BytesAvailable() > 0)
				{
					byte[] numArray = this.Read();
					try
					{
						memoryStream.Write(numArray, 0, (int)numArray.Length);
					}
					catch (Exception exception)
					{
						throw new ConnectionException(exception.Message);
					}
					long position = memoryStream.Position;
					memoryStream.Position = (long)0;
					if (this.ShouldWaitForData((new StreamReader(memoryStream)).ReadToEnd(), terminator))
					{
						this.WaitForData(responseCompletionTimeout);
					}
					memoryStream.Position = position;
				}
				array = memoryStream.ToArray();
			}
			return array;
		}

		/// <summary>
		///       Sends data from <c>sourceStream</c> and writes the response data to destinationStream.
		///       </summary>
		/// <param name="destinationStream">Destination for response.</param>
		/// <param name="sourceStream">Source of data to be sent.</param>
		/// <param name="initialResponseTimeout">The maximum time, in milliseconds, to wait for the initial response to be received. 
		///       If no data is received during this time, the function does not write any data to the destination stream.</param>
		/// <param name="responseCompletionTimeout">After the initial response, if no data is received for this period of time, the 
		///       input is considered complete and the method returns.</param>
		/// <param name="terminator">If the response contains this string, the input is considered complete and the method returns. 
		///       May be used to avoid waiting for more data when the response is always terminated with a known string. Use <c>null</c> 
		///       if no terminator is desired.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void SendAndWaitForResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, string terminator)
		{
			if (!this.Connected)
			{
				throw new ConnectionException("No Printer Connection");
			}
			this.Write(sourceStream);
			this.WaitForData(initialResponseTimeout);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				while (this.BytesAvailable() > 0)
				{
					byte[] numArray = this.Read(ConnectionA.SIZE_OF_STREAM_BUFFERS);
					try
					{
						if (memoryStream != null)
						{
							memoryStream.Write(numArray, 0, (int)numArray.Length);
						}
						destinationStream.Write(numArray, 0, (int)numArray.Length);
					}
					catch (Exception exception)
					{
						throw new ConnectionException(exception.Message);
					}
					long position = memoryStream.Position;
					memoryStream.Position = (long)0;
					if (terminator == null || this.ShouldWaitForData((new StreamReader(memoryStream)).ReadToEnd(), terminator))
					{
						this.WaitForData(responseCompletionTimeout);
					}
					memoryStream.Position = position;
				}
			}
		}

		/// <summary>
		///       Sends <c>dataToSend</c> and returns the response data.
		///       </summary>
		/// <param name="dataToSend">Byte array of data to send</param>
		/// <param name="initialResponseTimeout">The maximum time, in milliseconds, to wait for the initial response to be received. 
		///       If no data is received during this time, the function returns a zero length array.</param>
		/// <param name="responseCompletionTimeout">After the initial response, if no data is received for this period of time, the 
		///       input is considered complete and the method returns.</param>
		/// <param name="validator">If the response satisfies this validator, the input is considered complete and the method returns. 
		///       May be used to avoid waiting for more data when the response follows a known format.</param>
		/// <returns>received data</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual byte[] SendAndWaitForValidResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator)
		{
			byte[] array;
			if (!this.Connected)
			{
				throw new ConnectionException("No Printer Connection");
			}
			this.Write(dataToSend);
			this.WaitForData(initialResponseTimeout);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				while (this.BytesAvailable() > 0)
				{
					byte[] numArray = this.Read();
					try
					{
						memoryStream.Write(numArray, 0, (int)numArray.Length);
					}
					catch (Exception exception)
					{
						throw new ConnectionException(exception.Message);
					}
					if (validator.IsResponseComplete(memoryStream.ToArray()))
					{
						continue;
					}
					this.WaitForData(responseCompletionTimeout);
				}
				array = memoryStream.ToArray();
			}
			return array;
		}

		/// <summary>
		///       Sends data from <c>sourceStream</c> and writes the response data to destinationStream.
		///       </summary>
		/// <param name="destinationStream">Destination for response.</param>
		/// <param name="sourceStream">Source of data to be sent.</param>
		/// <param name="initialResponseTimeout">The maximum time, in milliseconds, to wait for the initial response to be received. 
		///       If no data is received during this time, the function does not write any data to the destination stream.</param>
		/// <param name="responseCompletionTimeout">After the initial response, if no data is received for this period of time, the 
		///       input is considered complete and the method returns.</param>
		/// <param name="validator">If the response satisfies this validator, the input is considered complete and the method returns. 
		///       May be used to avoid waiting for more data when the response follows a known format.If validator is null, no validation is performed. 
		///       When performing validation, this method will use enough memory to hold the entire response.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void SendAndWaitForValidResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator)
		{
			if (!this.Connected)
			{
				throw new ConnectionException("No Printer Connection");
			}
			this.Write(sourceStream);
			this.WaitForData(initialResponseTimeout);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				while (this.BytesAvailable() > 0)
				{
					byte[] numArray = this.Read(ConnectionA.SIZE_OF_STREAM_BUFFERS);
					try
					{
						if (memoryStream != null)
						{
							memoryStream.Write(numArray, 0, (int)numArray.Length);
						}
						destinationStream.Write(numArray);
					}
					catch (Exception exception)
					{
						throw new ConnectionException(exception.Message);
					}
					if (validator != null && validator.IsResponseComplete(memoryStream.ToArray()))
					{
						continue;
					}
					this.WaitForData(responseCompletionTimeout);
				}
			}
		}

		/// <summary>
		///       Sets the underlying read timeout value.
		///       </summary>
		/// <param name="timeout">The read timeout in milliseconds.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an error occurs while attempting to set the read timeout.</exception>
		public virtual void SetReadTimeout(int timeout)
		{
			if (this.commLink != null)
			{
				try
				{
					((ZebraNetworkSocket)this.commLink).SetReadTimeout(timeout);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					throw new ConnectionException(exception.Message, exception);
				}
			}
		}

		internal virtual bool ShouldWaitForData(string response, string terminator)
		{
			if (terminator == null)
			{
				return true;
			}
			return !response.Contains(terminator);
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "";
		}

		/// <summary>
		///       Causes the currently executing thread to sleep until <see cref="M:Zebra.Sdk.Comm.Connection.BytesAvailable" /> &gt; 0, or for a maximum of 
		///       <c>maxTimeout</c> milliseconds.
		///       </summary>
		/// <param name="maxTimeout">The maximum time in milliseconds to wait for an initial response from the printer.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void WaitForData(int maxTimeout)
		{
			DateTime dateTime = DateTime.Now.AddMilliseconds((double)maxTimeout);
			while (this.BytesAvailable() == 0 && DateTime.Now.CompareTo(dateTime) <= 0)
			{
				Sleeper.Sleep((long)50);
			}
		}

		/// <summary>
		///       Writes <c>data.Length</c> bytes from the specified byte array to this output stream.
		///       </summary>
		/// <param name="data">Data to write</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void Write(byte[] data)
		{
			this.Write(data, 0, (int)data.Length);
		}

		/// <summary>
		///        Writes all available bytes from the data source to this output stream.
		///       </summary>
		/// <param name="dataSource">The data.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public virtual void Write(BinaryReader dataSource)
		{
			int length = 16384;
			if (dataSource.BaseStream.CanSeek)
			{
				length = (int)dataSource.BaseStream.Length;
			}
			byte[] numArray = new byte[length];
			try
			{
				for (int i = dataSource.Read(numArray, 0, (int)numArray.Length); i > 0; i = dataSource.Read(numArray, 0, (int)numArray.Length))
				{
					this.Write(numArray, 0, i);
				}
			}
			catch (Exception exception)
			{
				throw new ConnectionException(string.Concat("Error writing to connection: ", exception.Message));
			}
		}

		/// <summary>
		///       Writes <c>length</c> bytes from <c>data</c> starting at <c>offset</c>.
		///       </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The start offset in the <c>data</c>.</param>
		/// <param name="length">The number of bytes to write.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		public virtual void Write(byte[] data, int offset, int length)
		{
			if (this.outputStream == null || !this.Connected)
			{
				throw new ConnectionException("The connection is not open");
			}
			try
			{
				int num = length;
				int num1 = offset;
				if (this.outputStream.BaseStream is NetworkStream)
				{
					this.outputStream.BaseStream.WriteTimeout = 10000;
				}
				while (num > 0)
				{
					int num2 = (num > this.MAX_DATA_TO_WRITE_TO_STREAM_AT_ONCE ? this.MAX_DATA_TO_WRITE_TO_STREAM_AT_ONCE : num);
					this.outputStream.BaseStream.WriteAsync(data, num1, num2).Wait();
					this.WriteToLogStream(data, num1, num2);
					this.outputStream.BaseStream.FlushAsync().Wait();
					Sleeper.Sleep((long)10);
					num1 += num2;
					num -= num2;
				}
			}
			catch (Exception exception)
			{
				throw new ConnectionException(string.Concat("Error writing to connection: ", exception.Message));
			}
		}

		protected virtual void WriteToLogStream(byte[] buffer, int offset, int numBytes)
		{
			if (this.myWriteLogStream != null)
			{
				try
				{
					this.myWriteLogStream.Write(buffer, offset, numBytes);
				}
				catch (Exception exception)
				{
					throw new LogStreamException(string.Concat("Error writing to log: ", exception.Message));
				}
			}
		}
	}
}