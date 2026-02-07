using System;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       An interface defining methods associated with a device that may be addressed via an IP connection.
	///       </summary>
	public interface IpAddressable
	{
		/// <summary>
		///       Returns the address which was passed into the constructor.
		///       </summary>
		/// <returns>the address used to establish this connection. This can be either a DNS Hostname or an IP address.</returns>
		string Address
		{
			get;
		}

		/// <summary>
		///       Returns the port number which was passed into the constructor.
		///       </summary>
		/// <returns>the port number associated with the connection.</returns>
		string PortNumber
		{
			get;
		}
	}
}