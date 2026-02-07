using System;
using Zebra.Sdk.Comm;

namespace Zebra.Sdk.Settings.Internal
{
	internal class ConnectionUtil
	{
		public ConnectionUtil()
		{
		}

		public static Connection SelectConnection(Connection printerConnection)
		{
			Connection printingChannel = printerConnection;
			if (printerConnection is MultichannelConnection)
			{
				if (!((MultichannelConnection)printerConnection).StatusChannel.Connected)
				{
					printingChannel = ((MultichannelConnection)printerConnection).PrintingChannel;
				}
				else
				{
					printingChannel = ((MultichannelConnection)printerConnection).StatusChannel;
				}
			}
			return printingChannel;
		}
	}
}