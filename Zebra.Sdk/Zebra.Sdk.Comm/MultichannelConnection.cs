using System;
using System.IO;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Base class for Link-OS printers which support separate printing and status channels.
	///       </summary>
	public abstract class MultichannelConnection : ConnectionWithWriteLogging, Connection
	{
		protected ConnectionWithWriteLogging raw;

		protected StatusConnectionWithWriteLogging settings;

		/// <summary>
		///       Returns true if the connection is open.
		///       </summary>
		public virtual bool Connected
		{
			get
			{
				if (this.raw.Connected)
				{
					return true;
				}
				return this.settings.Connected;
			}
		}

		/// <summary>
		///       Gets or sets the maximum time, in milliseconds, to wait for any data to be received.
		///       </summary>
		public int MaxTimeoutForRead
		{
			get
			{
				return this.raw.MaxTimeoutForRead;
			}
			set
			{
				this.raw.MaxTimeoutForRead = value;
				this.settings.MaxTimeoutForRead = value;
			}
		}

		/// <summary>
		///       Gets the underlying printing <see cref="T:Zebra.Sdk.Comm.Connection" /> of this MultichannelConnection.
		///       </summary>
		public virtual Connection PrintingChannel
		{
			get
			{
				return this.raw;
			}
		}

		/// <summary>
		///       Gets a human-readable description of the connection.
		///       </summary>
		public virtual string SimpleConnectionName
		{
			get
			{
				return this.raw.SimpleConnectionName;
			}
		}

		/// <summary>
		///       Gets the underlying status <see cref="T:Zebra.Sdk.Comm.StatusConnection" /> of this MultichannelConnection.
		///       </summary>
		public virtual StatusConnection StatusChannel
		{
			get
			{
				return this.settings;
			}
		}

		/// <summary>
		///       Gets or sets the maximum time, in milliseconds, to wait in-between reads after the initial read.
		///       </summary>
		public int TimeToWaitForMoreData
		{
			get
			{
				return this.raw.TimeToWaitForMoreData;
			}
			set
			{
				this.raw.TimeToWaitForMoreData = value;
				this.settings.TimeToWaitForMoreData = value;
			}
		}

		protected MultichannelConnection()
		{
		}

		/// <summary>
		///       Sets the stream to log the write data to.
		///       </summary>
		/// <param name="logStream">Log all write data to this stream.</param>
		public void AddWriteLogStream(BinaryWriter logStream)
		{
			this.raw.AddWriteLogStream(logStream);
			this.settings.AddWriteLogStream(logStream);
		}

		/// <summary>
		///       Returns an estimate of the number of bytes that can be read from this connection without blocking.
		///       </summary>
		/// <returns>The estimated number of bytes available.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public int BytesAvailable()
		{
			return this.raw.BytesAvailable();
		}

		/// <summary>
		///       Closes both the printing and status channels of this MultichannelConnection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		public virtual void Close()
		{
			this.raw.Close();
			this.settings.Close();
		}

		/// <summary>
		///       Closes the printing channel of this MultichannelConnection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		public void ClosePrintingChannel()
		{
			this.raw.Close();
		}

		/// <summary>
		///        Closes the status channel of this MultichannelConnection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		public void CloseStatusChannel()
		{
			this.settings.Close();
		}

		/// <summary>
		///       Returns a <c>ConnectionReestablisher</c> which allows for easy recreation of a connection which may have been closed.
		///       </summary>
		/// <param name="thresholdTime">How long the Connection reestablisher will wait before attempting to reconnection to the printer.</param>
		/// <returns>Instance of <see cref="T:Zebra.Sdk.Comm.ConnectionReestablisher" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the ConnectionReestablisher could not be created.</exception>
		public virtual ConnectionReestablisher GetConnectionReestablisher(long thresholdTime)
		{
			return this.raw.GetConnectionReestablisher(thresholdTime);
		}

		/// <summary>
		///       Opens both the printing and status channel of this Multichannel connection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		public virtual void Open()
		{
			bool flag = false;
			try
			{
				this.OpenPrintingChannel();
				flag = true;
			}
			catch (ConnectionException)
			{
			}
			try
			{
				this.OpenStatusChannel();
				flag = true;
			}
			catch (ConnectionException)
			{
			}
			if (!flag)
			{
				throw new ConnectionException("Could not open connection");
			}
		}

		/// <summary>
		///       Opens the printing channel of this Multichannel connection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if the connection cannot be established.</exception>
		public virtual void OpenPrintingChannel()
		{
			this.raw.Open();
		}

		/// <summary>
		///       Opens the status channel of this Multichannel connection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if the connection cannot be established.</exception>
		public virtual void OpenStatusChannel()
		{
			this.settings.Open();
		}

		/// <summary>
		///       Reads all the available data from the connection. This call is non-blocking.
		///       </summary>
		/// <returns>Data read from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public byte[] Read()
		{
			return this.raw.Read();
		}

		/// <summary>
		///       Reads all the available data from the connection.
		///       </summary>
		/// <param name="destinationStream">For read data.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void Read(BinaryWriter destinationStream)
		{
			this.raw.Read(destinationStream);
		}

		/// <summary>
		///       Reads the next byte of data from the connection.
		///       </summary>
		/// <returns>The next byte from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public int ReadChar()
		{
			return this.raw.ReadChar();
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
		public byte[] SendAndWaitForResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, string terminator)
		{
			this.ThrowIfOnlyStatusOpen();
			return this.raw.SendAndWaitForResponse(dataToSend, initialResponseTimeout, responseCompletionTimeout, terminator);
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
		public void SendAndWaitForResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, string terminator)
		{
			this.ThrowIfOnlyStatusOpen();
			this.raw.SendAndWaitForResponse(destinationStream, sourceStream, initialResponseTimeout, responseCompletionTimeout, terminator);
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
		public byte[] SendAndWaitForValidResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator)
		{
			this.ThrowIfOnlyStatusOpen();
			return this.raw.SendAndWaitForValidResponse(dataToSend, initialResponseTimeout, responseCompletionTimeout, validator);
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
		public void SendAndWaitForValidResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator)
		{
			this.ThrowIfOnlyStatusOpen();
			this.raw.SendAndWaitForValidResponse(destinationStream, sourceStream, initialResponseTimeout, responseCompletionTimeout, validator);
		}

		private void ThrowIfOnlyStatusOpen()
		{
			if (!this.raw.Connected && this.settings.Connected)
			{
				throw new ConnectionException("Operation cannot be performed with only the status channel open");
			}
		}

		/// <summary>
		///       Causes the currently executing thread to sleep until <see cref="M:Zebra.Sdk.Comm.Connection.BytesAvailable" /> &gt; 0, or for a maximum of 
		///       <c>maxTimeout</c> milliseconds.
		///       </summary>
		/// <param name="maxTimeout">The maximum time in milliseconds to wait for an initial response from the printer.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void WaitForData(int maxTimeout)
		{
			this.raw.WaitForData(maxTimeout);
		}

		/// <summary>
		///       Writes <c>data.Length</c> bytes from the specified byte array to this output stream.
		///       </summary>
		/// <param name="data">Data to write</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void Write(byte[] data)
		{
			this.ThrowIfOnlyStatusOpen();
			this.raw.Write(data);
		}

		/// <summary>
		///       Writes <c>length</c> bytes from <c>data</c> starting at <c>offset</c>.
		///       </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The start offset in the <c>data</c>.</param>
		/// <param name="length">The number of bytes to write.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		public void Write(byte[] data, int offset, int length)
		{
			this.ThrowIfOnlyStatusOpen();
			this.raw.Write(data, offset, length);
		}

		/// <summary>
		///        Writes all available bytes from the data source to this output stream.
		///       </summary>
		/// <param name="dataSource">The data.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public void Write(BinaryReader dataSource)
		{
			this.ThrowIfOnlyStatusOpen();
			this.raw.Write(dataSource);
		}
	}
}