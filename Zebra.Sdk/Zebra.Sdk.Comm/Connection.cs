using System;
using System.IO;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       A connection to a device.
	///       </summary>
	public interface Connection
	{
		/// <summary>
		///       Returns true if the connection is open.
		///       </summary>
		bool Connected
		{
			get;
		}

		/// <summary>
		///       Gets or sets the maximum time, in milliseconds, to wait for any data to be received.
		///       </summary>
		int MaxTimeoutForRead
		{
			get;
			set;
		}

		/// <summary>
		///       Gets a human-readable description of the connection.
		///       </summary>
		string SimpleConnectionName
		{
			get;
		}

		/// <summary>
		///       Gets or sets the maximum time, in milliseconds, to wait in-between reads after the initial read.
		///       </summary>
		int TimeToWaitForMoreData
		{
			get;
			set;
		}

		/// <summary>
		///       Returns an estimate of the number of bytes that can be read from this connection without blocking.
		///       </summary>
		/// <returns>The estimated number of bytes available.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		int BytesAvailable();

		/// <summary>
		///       Closes this connection and releases any system resources associated with the connection.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void Close();

		/// <summary>
		///       Returns a <c>ConnectionReestablisher</c> which allows for easy recreation of a connection which may have been closed.
		///       </summary>
		/// <param name="thresholdTime">How long the Connection reestablisher will wait before attempting to reconnection to the printer.</param>
		/// <returns>Instance of <see cref="T:Zebra.Sdk.Comm.ConnectionReestablisher" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the ConnectionReestablisher could not be created.</exception>
		ConnectionReestablisher GetConnectionReestablisher(long thresholdTime);

		/// <summary>
		///       Opens the connection to a device.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection cannot be established.</exception>
		void Open();

		/// <summary>
		///       Reads all the available data from the connection. This call is non-blocking.
		///       </summary>
		/// <returns>Data read from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		byte[] Read();

		/// <summary>
		///       Reads all the available data from the connection.
		///       </summary>
		/// <param name="destinationStream">For read data.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void Read(BinaryWriter destinationStream);

		/// <summary>
		///       Reads the next byte of data from the connection.
		///       </summary>
		/// <returns>The next byte from the connection.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		int ReadChar();

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
		byte[] SendAndWaitForResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, string terminator);

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
		void SendAndWaitForResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, string terminator);

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
		byte[] SendAndWaitForValidResponse(byte[] dataToSend, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator);

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
		void SendAndWaitForValidResponse(BinaryWriter destinationStream, BinaryReader sourceStream, int initialResponseTimeout, int responseCompletionTimeout, ResponseValidator validator);

		/// <summary>
		///       See the classes which implement this method for the format of the description string.
		///       </summary>
		/// <returns>The connection description string.</returns>
		string ToString();

		/// <summary>
		///       Causes the currently executing thread to sleep until <see cref="M:Zebra.Sdk.Comm.Connection.BytesAvailable" /> &gt; 0, or for a maximum of 
		///       <c>maxTimeout</c> milliseconds.
		///       </summary>
		/// <param name="maxTimeout">The maximum time in milliseconds to wait for an initial response from the printer.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void WaitForData(int maxTimeout);

		/// <summary>
		///       Writes <c>data.Length</c> bytes from the specified byte array to this output stream.
		///       </summary>
		/// <param name="data">Data to write</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void Write(byte[] data);

		/// <summary>
		///       Writes <c>length</c> bytes from <c>data</c> starting at <c>offset</c>.
		///       </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The start offset in the <c>data</c>.</param>
		/// <param name="length">The number of bytes to write.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">if an I/O error occurs.</exception>
		void Write(byte[] data, int offset, int length);

		/// <summary>
		///        Writes all available bytes from the data source to this output stream.
		///       </summary>
		/// <param name="dataSource">The data.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		void Write(BinaryReader dataSource);
	}
}