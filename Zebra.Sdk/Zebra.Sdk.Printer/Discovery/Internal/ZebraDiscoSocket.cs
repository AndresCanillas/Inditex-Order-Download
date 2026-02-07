using System;
using System.Net;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	public interface ZebraDiscoSocket
	{
		void Close();

		void JoinGroup(string host);

		void Receive(ref byte[] p);

		void Send(byte[] p, IPEndPoint endPoint);

		void SetInterface(IPAddress inf);

		void SetSoTimeout(int timeout);

		void SetTimeToLive(int hops);
	}
}