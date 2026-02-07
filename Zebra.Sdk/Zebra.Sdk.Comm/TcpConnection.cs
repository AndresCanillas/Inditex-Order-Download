using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Establishes a TCP connection to a device
	///       </summary>
	public class TcpConnection : ConnectionA, IpAddressable, ConnectionI
	{
		/// <summary>
		///       The default TCP port for ZPL devices.
		///       </summary>
		public readonly static int DEFAULT_ZPL_TCP_PORT;

		/// <summary>
		///       The default TCP port for CPCL devices.
		///       </summary>
		public readonly static int DEFAULT_CPCL_TCP_PORT;

		/// <summary>
		///       Returns the address which was passed into the constructor.
		///       </summary>
		/// <returns>the address used to establish this connection. This can be either a DNS Hostname or an IP address.</returns>
		public string Address
		{
			get
			{
				return ((TcpZebraConnectorImpl)this.zebraConnector).Address;
			}
		}

		protected virtual string ConnectionBuilderPrefix
		{
			get
			{
				return "TCP";
			}
		}

		internal bool IsCardPrinter
		{
			get;
			set;
		}

		/// <summary>
		///       Returns the port number which was passed into the constructor.
		///       </summary>
		/// <returns>the port number associated with the connection.</returns>
		public string PortNumber
		{
			get
			{
				return Convert.ToString(((TcpZebraConnectorImpl)this.zebraConnector).GetPort());
			}
		}

		internal string SerialNumber
		{
			get;
			set;
		}

		/// <summary>
		///       Gets the IP address as the description.
		///       </summary>
		public override string SimpleConnectionName
		{
			get
			{
				return this.Address;
			}
		}

		static TcpConnection()
		{
			TcpConnection.DEFAULT_ZPL_TCP_PORT = 9100;
			TcpConnection.DEFAULT_CPCL_TCP_PORT = 6101;
		}

		protected TcpConnection()
		{
		}

		protected TcpConnection(ConnectionInfo connectionInfo)
		{
			string myData = connectionInfo.GetMyData();
			List<string> matches = RegexUtil.GetMatches(string.Concat("^\\s*((?i)", this.ConnectionBuilderPrefix, ":)?([\\d]{1,3}.[\\d]{1,3}.[\\d]{1,3}.[\\d]{1,3})(:([\\d]{1,5}))?\\s*$"), myData);
			if (!matches.Any<string>())
			{
				if (myData.Contains("zebra.com/apps/r/nfc?") && myData.Contains("mB="))
				{
					throw new NotMyConnectionDataException(string.Concat("TCP Connection doesn't understand ", myData));
				}
				matches = RegexUtil.GetMatches(string.Concat("^\\s*((?i)", this.ConnectionBuilderPrefix, ":)?([^:]+)(:([\\d]{1,5}))?\\s*$"), myData);
				if (!matches.Any<string>())
				{
					throw new NotMyConnectionDataException(string.Concat(this.ConnectionBuilderPrefix, " Connection doesn't understand ", myData));
				}
			}
			string item = matches[2];
			int defaultPort = this.GetDefaultPort();
			try
			{
				defaultPort = int.Parse(matches[4]);
			}
			catch (Exception)
			{
			}
			this.zebraConnector = new TcpZebraConnectorImpl(item, defaultPort);
			this.maxTimeoutForRead = ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ;
			this.timeToWaitForMoreData = ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA;
		}

		/// <summary>
		///       Initializes a new instance of the <c>TcpConnection</c> class.
		///       </summary>
		/// <param name="address">the IP Address or DNS Hostname.</param>
		/// <param name="port">the port number.</param>
		public TcpConnection(string address, int port) : this(address, port, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA)
		{
		}

		/// <summary>
		///       Initializes a new instance of the <c>TcpConnection</c> class.
		///       </summary>
		/// <param name="address">The IP Address or DNS Hostname.</param>
		/// <param name="port">The port number.</param>
		/// <param name="maxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received.</param>
		/// <param name="timeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read.</param>
		public TcpConnection(string address, int port, int maxTimeoutForRead, int timeToWaitForMoreData) : this(new TcpZebraConnectorImpl(address, port), maxTimeoutForRead, timeToWaitForMoreData)
		{
		}

		private TcpConnection(ZebraConnector zebraConnector, int maxTimeoutForRead, int timeToWaitForMoreData)
		{
			this.zebraConnector = zebraConnector;
			this.maxTimeoutForRead = maxTimeoutForRead;
			this.timeToWaitForMoreData = timeToWaitForMoreData;
		}

		/// <summary>
		///       Returns an estimate of the number of bytes that can be read from this connection without blocking.
		///       </summary>
		/// <returns>The estimated number of bytes available.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If an I/O error occurs.</exception>
		public override int BytesAvailable()
		{
			if (!((TcpZebraConnectorImpl)this.zebraConnector).DataAvailable)
			{
				return 0;
			}
			return ConnectionA.SIZE_OF_STREAM_BUFFERS;
		}

		/// <summary>
		///       Returns a <c>ConnectionReestablisher</c> which allows for easy recreation of a connection which may have been closed.
		///       </summary>
		/// <param name="thresholdTime">How long the Connection reestablisher will wait before attempting to reconnection to the printer.</param>
		/// <returns>Instance of <see cref="T:Zebra.Sdk.Comm.ConnectionReestablisher" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the ConnectionReestablisher could not be created.</exception>
		public override ConnectionReestablisher GetConnectionReestablisher(long thresholdTime)
		{
			if (!this.IsCardPrinter)
			{
				return new TcpConnectionReestablisher(this, thresholdTime);
			}
			return ReflectionUtil.LoadTcpCardConnectionReestablisher(this, (int)thresholdTime);
		}

		protected virtual int GetDefaultPort()
		{
			return TcpConnection.DEFAULT_ZPL_TCP_PORT;
		}

		/// <summary>
		///       Sets the read timeout on the underlying socket.
		///       </summary>
		/// <param name="readTimeout">The read timeout in milliseconds</param>
		public override void SetReadTimeout(int readTimeout)
		{
			((TcpZebraConnectorImpl)this.zebraConnector).SetReadTimeout(readTimeout);
		}

		/// <summary>
		///       Returns <c>TCP</c>:[Address]:[PortNumber].
		///       </summary>
		public override string ToString()
		{
			return string.Format("TCP:{0}:{1}", this.Address, this.PortNumber);
		}
	}
}