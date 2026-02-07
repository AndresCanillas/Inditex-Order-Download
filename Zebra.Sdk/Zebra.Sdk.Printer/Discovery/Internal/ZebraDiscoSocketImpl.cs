using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class ZebraDiscoSocketImpl : ZebraDiscoSocket
	{
		private UdpClient mySocket;

		public ZebraDiscoSocketImpl()
		{
			try
			{
				this.mySocket = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
			}
			catch (Exception exception)
			{
				throw new DiscoveryException(exception.Message);
			}
		}

		public void Close()
		{
			this.mySocket.Dispose();
		}

		public void JoinGroup(string host)
		{
			this.mySocket.JoinMulticastGroup(IPAddress.Parse(host));
		}

		public void Receive(ref byte[] packet)
		{
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			try
			{
				try
				{
					cancellationTokenSource.CancelAfter(this.mySocket.Client.ReceiveTimeout);
					Task<UdpReceiveResult> task = this.mySocket.ReceiveAsync();
					task.Wait(cancellationTokenSource.Token);
					packet = task.Result.Buffer;
				}
				catch (OperationCanceledException operationCanceledException1)
				{
					OperationCanceledException operationCanceledException = operationCanceledException1;
					throw new TimeoutException(operationCanceledException.Message, operationCanceledException);
				}
			}
			finally
			{
				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
			}
		}

		public void Send(byte[] packet, IPEndPoint endPoint)
		{
			this.mySocket.SendAsync(packet, (int)packet.Length, endPoint);
		}

		public void SetInterface(IPAddress inf)
		{
			this.mySocket.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.MulticastInterface, inf.GetAddressBytes());
		}

		public void SetSoTimeout(int timeout)
		{
			this.mySocket.Client.ReceiveTimeout = timeout;
		}

		public void SetTimeToLive(int hops)
		{
			this.mySocket.Ttl = (short)hops;
		}
	}
}