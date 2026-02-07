using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Class for describing the status of ports open on a Zebra printer.
	///       </summary>
	public class TcpPortStatus
	{
		private string printerPort;

		private string portName;

		private string remoteIpAddress;

		private string remotePort;

		private string status;

		/// <summary>
		///       The name of the protocol associated with that port, for example, HTTP for 80, FTP for 21.
		///       </summary>
		public string PortName
		{
			get
			{
				return this.portName;
			}
		}

		/// <summary>
		///       The port number open on the printer.
		///       </summary>
		public string PrinterPort
		{
			get
			{
				return this.printerPort;
			}
		}

		/// <summary>
		///       The remote IP connected to the printer's port, will be 0.0.0.0 if not connected.
		///       </summary>
		public string RemoteIpAddress
		{
			get
			{
				return this.remoteIpAddress;
			}
		}

		/// <summary>
		///       The port number of the remote connected to the printer, will be 0 if no connection.
		///       </summary>
		public string RemotePort
		{
			get
			{
				return this.remotePort;
			}
		}

		/// <summary>
		///       The status of the printer's port, such as {@code LISTEN}, {@code ESTABLISHED}.
		///       </summary>
		public string Status
		{
			get
			{
				return this.status;
			}
		}

		/// <summary>
		///       Creates a container which describes the status of a specific port on a Zebra printer.
		///       </summary>
		/// <param name="printerPort">The printer's port.</param>
		/// <param name="portName">The name of the protocol used by the port.</param>
		/// <param name="remoteIpAddress">Remote IP connected to the port.</param>
		/// <param name="remotePort">Remote port number.</param>
		/// <param name="status">Port status.</param>
		public TcpPortStatus(string printerPort, string portName, string remoteIpAddress, string remotePort, string status)
		{
			this.printerPort = printerPort;
			this.portName = portName;
			this.remoteIpAddress = remoteIpAddress;
			this.remotePort = remotePort;
			this.status = status;
		}

		/// <summary>
		///       String description of the port status, prints as "PORT(NAME) REMOTE-IP:REMOTE-PORT STATUS"
		///       </summary>
		/// <returns>Description of the port status.</returns>
		public override string ToString()
		{
			return string.Concat(new string[] { this.printerPort, "(", this.portName, ") ", this.remoteIpAddress, ":", this.remotePort, " ", this.status });
		}
	}
}