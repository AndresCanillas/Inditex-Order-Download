using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Zebra.Sdk.Comm.Internal
{
	internal class ZebraNetworkSocket : ZebraSocket, IDisposable
	{
		private Socket socket;

		private int port = 9100;

		private NetworkStream networkStream;

		private IPAddress inetSocketAddress;

		private const int MAX_TIMEOUT = 15000;

		private bool disposedValue;

		public ZebraNetworkSocket(string address, int port)
		{
			this.inetSocketAddress = new IPAddress(IPAddress.Parse(address).GetAddressBytes());
			this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			this.port = port;
		}

		public void Close()
		{
			if (this.socket != null)
			{
				this.socket.Dispose();
			}
		}

		public void Connect()
		{
			if (this.socket.Connected)
			{
				this.socket.Dispose();
			}
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			try
			{
				try
				{
					cancellationTokenSource.CancelAfter(15000);
					this.socket.ConnectAsync(this.inetSocketAddress, this.port).Wait(cancellationTokenSource.Token);
					this.networkStream = new NetworkStream(this.socket);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					throw new TimeoutException(exception.Message, exception);
				}
			}
			finally
			{
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					if (this.inetSocketAddress != null)
					{
						((IDisposable)this.inetSocketAddress).Dispose();
					}
					if (this.socket != null)
					{
						((IDisposable)this.socket).Dispose();
					}
					if (this.networkStream != null)
					{
						((IDisposable)this.networkStream).Dispose();
					}
				}
				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		public BinaryReader GetInputStream()
		{
			return new BinaryReader(this.networkStream);
		}

		public BinaryWriter GetOutputStream()
		{
			return new BinaryWriter(this.networkStream);
		}

		public void SetReadTimeout(int timeout)
		{
			try
			{
				this.socket.ReceiveTimeout = timeout;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				throw new IOException(exception.Message, exception);
			}
		}
	}
}