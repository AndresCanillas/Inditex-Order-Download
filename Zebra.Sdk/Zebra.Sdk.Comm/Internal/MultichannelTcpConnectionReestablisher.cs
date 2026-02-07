using System;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Comm.Internal
{
	internal class MultichannelTcpConnectionReestablisher : TcpConnectionReestablisher, ConnectionReestablisher
	{
		public MultichannelTcpConnectionReestablisher(Connection c, long thresholdTime) : base(c, thresholdTime)
		{
		}

		protected override Connection GetNewConnection(string destinationAddress)
		{
			MultichannelConnection multichannelConnection = (MultichannelConnection)this.zebraPrinterConnection;
			int num = int.Parse(((TcpConnection)multichannelConnection.PrintingChannel).PortNumber);
			int num1 = int.Parse(((TcpStatusConnection)multichannelConnection.StatusChannel).PortNumber);
			return new MultichannelTcpConnection(destinationAddress, num, num1);
		}
	}
}