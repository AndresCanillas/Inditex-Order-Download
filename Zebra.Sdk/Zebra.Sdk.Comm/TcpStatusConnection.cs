using System;
using Zebra.Sdk.Comm.Internal;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Establishes a status only TCP connection to a device
	///       </summary>
	public class TcpStatusConnection : TcpConnection, StatusConnectionWithWriteLogging, StatusConnection, Connection
	{
		/// <summary>
		///       The default Status TCP port for ZPL devices.
		///       </summary>
		public readonly static int DEFAULT_STATUS_TCP_PORT;

		protected override string ConnectionBuilderPrefix
		{
			get
			{
				return "TCP_STATUS";
			}
		}

		/// <summary>
		///       Returns the IP address and the status port as the description.
		///       </summary>
		public override string SimpleConnectionName
		{
			get
			{
				return base.Address;
			}
		}

		static TcpStatusConnection()
		{
			TcpStatusConnection.DEFAULT_STATUS_TCP_PORT = 9200;
		}

		protected TcpStatusConnection(ConnectionInfo connectionInfo) : base(connectionInfo)
		{
		}

		/// <summary>
		///       Initializes a new status only instance of the <c>TcpStatusConnection</c> class using the default status port of <see cref="F:Zebra.Sdk.Comm.TcpStatusConnection.DEFAULT_STATUS_TCP_PORT" /> 9200.
		///       </summary>
		/// <param name="address">The IP Address or DNS Hostname.</param>
		public TcpStatusConnection(string address) : this(address, TcpStatusConnection.DEFAULT_STATUS_TCP_PORT, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA)
		{
		}

		/// <summary>
		///       Initializes a new status only instance of the <c>TcpStatusConnection</c> class.
		///       </summary>
		/// <param name="address">The IP Address or DNS Hostname.</param>
		/// <param name="port">The port number.</param>
		public TcpStatusConnection(string address, int port) : this(address, port, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA)
		{
		}

		/// <summary>
		///       Initializes a new status only instance of the <c>TcpStatusConnection</c> class.
		///       </summary>
		/// <param name="address">The IP Address or DNS Hostname.</param>
		/// <param name="port">The port number.</param>
		/// <param name="maxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received.</param>
		/// <param name="timeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read.</param>
		public TcpStatusConnection(string address, int port, int maxTimeoutForRead, int timeToWaitForMoreData) : base(address, port, maxTimeoutForRead, timeToWaitForMoreData)
		{
		}

		protected override int GetDefaultPort()
		{
			return TcpStatusConnection.DEFAULT_STATUS_TCP_PORT;
		}

		/// <summary>
		///       The <c>address</c> and <c>port number</c> are the parameters which were passed into the constructor.
		///       </summary>
		/// <returns>
		///   <c>TCP_STATUS</c>:[address]:[port number]</returns>
		public override string ToString()
		{
			return string.Concat(new string[] { this.ConnectionBuilderPrefix, ":", base.Address, ":", base.PortNumber });
		}
	}
}