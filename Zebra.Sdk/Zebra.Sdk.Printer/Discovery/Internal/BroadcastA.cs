using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zebra.Sdk.Printer.Discovery;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal abstract class BroadcastA
	{
		private Dictionary<string, string> discoveredPrinters;

		protected DiscoveryHandler discoveryHandler;

		private readonly int DISCOVERY_PORT = 4201;

		public readonly static int MAX_DATAGRAM_SIZE;

		private static byte[] ADVANCED_DISCOVERY_REQUEST_PACKET;

		public static byte[] DISCOVERY_REQUEST_PACKET;

		protected readonly static int DEFAULT_LATE_ARRIVAL_DELAY;

		private int waitForResponsesTimeout = BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY;

		protected IPAddress[] broadcastIpAddresses;

		static BroadcastA()
		{
			BroadcastA.MAX_DATAGRAM_SIZE = 600;
			BroadcastA.ADVANCED_DISCOVERY_REQUEST_PACKET = new byte[] { 46, 44, 58, 1, 0, 0, 0, 1, 164, 237, 0, 0, 0 };
			BroadcastA.DISCOVERY_REQUEST_PACKET = new byte[] { 46, 44, 58, 1, 0, 0 };
			BroadcastA.DEFAULT_LATE_ARRIVAL_DELAY = 6000;
		}

		protected BroadcastA(int waitForResponsesTimeout)
		{
			this.waitForResponsesTimeout = waitForResponsesTimeout;
			this.discoveredPrinters = new Dictionary<string, string>();
		}

		protected virtual ZebraDiscoSocket CreateDiscoSocket()
		{
			return new ZebraDiscoSocketImpl();
		}

		protected byte[] CreateDiscoveryRequestPacket()
		{
			return BroadcastA.ADVANCED_DISCOVERY_REQUEST_PACKET;
		}

		private ZebraDiscoSocket CreateDiscoverySocket()
		{
			ZebraDiscoSocket zebraDiscoSocket = this.CreateDiscoSocket();
			this.SetSocketOptions(zebraDiscoSocket);
			zebraDiscoSocket.SetSoTimeout(this.waitForResponsesTimeout);
			return zebraDiscoSocket;
		}

		public void DoBroadcast(DiscoveryHandler discoveryHandler)
		{
			DiscoveryHandler discoveryHandler1 = discoveryHandler;
			if (discoveryHandler1 == null)
			{
				throw new DiscoveryException("A DiscoveryHandler must be supplied");
			}
			this.discoveryHandler = discoveryHandler1;
			this.StartDiscoveryInBackground();
		}

		protected virtual bool DoDiscovery()
		{
			bool flag;
			try
			{
				ZebraDiscoSocket zebraDiscoSocket = this.CreateDiscoverySocket();
				byte[] numArray = new byte[BroadcastA.MAX_DATAGRAM_SIZE];
				this.SendDiscoveryRequest(zebraDiscoSocket, this.CreateDiscoveryRequestPacket());
				this.GetDiscoveryResponses(zebraDiscoSocket, ref numArray);
				zebraDiscoSocket.Close();
				flag = true;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				string message = exception.GetBaseException().Message ?? exception.Message;
				this.discoveryHandler.DiscoveryError(message);
				return false;
			}
			return flag;
		}

		private void GetDiscoveryResponses(ZebraDiscoSocket bcastSocket, ref byte[] incomingPacket)
		{
			while (true)
			{
				try
				{
					bcastSocket.Receive(ref incomingPacket);
					DiscoveredPrinterNetwork discoveredPrinterNetwork = DiscoveredPrinterNetworkFactory.GetDiscoveredPrinterNetwork(incomingPacket);
					if (!this.discoveredPrinters.ContainsKey(discoveredPrinterNetwork.Address) && !discoveredPrinterNetwork.Address.Equals("0.0.0.0"))
					{
						this.discoveryHandler.FoundPrinter(discoveredPrinterNetwork);
						this.discoveredPrinters.Add(discoveredPrinterNetwork.Address, discoveredPrinterNetwork.DiscoveryDataMap["DNS_NAME"]);
					}
					if (this.ShouldExitOnceAPrinterIsFound())
					{
						break;
					}
				}
				catch (AggregateException aggregateException) when (aggregateException.InnerException is SocketException)
				{
				}
				catch (SocketException)
				{
				}
				catch (TimeoutException)
				{
					break;
				}
				catch (IOException)
				{
					break;
				}
			}
		}

		private void SendDiscoveryRequest(ZebraDiscoSocket bcastSocket, byte[] outgoingPacket)
		{
			for (int i = 0; i < (int)this.broadcastIpAddresses.Length; i++)
			{
				bcastSocket.Send(outgoingPacket, new IPEndPoint(this.broadcastIpAddresses[i], this.DISCOVERY_PORT));
			}
		}

		protected abstract void SetSocketOptions(ZebraDiscoSocket sock);

		protected bool ShouldExitOnceAPrinterIsFound()
		{
			return false;
		}

		private void StartDiscoveryInBackground()
		{
			Task.Run(() => {
				if (this.DoDiscovery())
				{
					this.discoveryHandler.DiscoveryFinished();
				}
			});
		}
	}
}