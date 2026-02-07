using System;
using System.IO;
using System.Net.Sockets;
using Zebra.Sdk.Comm.Internal;

namespace Zebra.Sdk.Comm
{
	internal class TcpZebraConnectorImpl : ZebraConnector, IDisposable
	{
		private int port;

		private string address;

		private ZebraSocket networkSocket;

		private bool disposedValue;

		public string Address
		{
			get
			{
				return this.address;
			}
		}

		public bool DataAvailable
		{
			get
			{
				return ((NetworkStream)this.networkSocket.GetInputStream().BaseStream).DataAvailable;
			}
		}

		public TcpZebraConnectorImpl(string ipAddress, int port)
		{
			this.address = ipAddress;
			this.port = port;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing && this.networkSocket != null)
				{
					this.networkSocket.Close();
					((IDisposable)this.networkSocket).Dispose();
				}
				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		public int GetPort()
		{
			return this.port;
		}

		public ZebraSocket Open()
		{
			ZebraSocket zebraSocket;
			try
			{
				this.networkSocket = new ZebraNetworkSocket(this.address, this.port);
				this.networkSocket.Connect();
				zebraSocket = this.networkSocket;
			}
			catch (Exception exception)
			{
				throw new ConnectionException(exception.Message);
			}
			return zebraSocket;
		}

		public void SetReadTimeout(int readTimeout)
		{
			((ZebraNetworkSocket)this.networkSocket).SetReadTimeout(readTimeout);
		}
	}
}