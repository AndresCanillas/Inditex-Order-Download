using System;
using System.Collections.Generic;
using System.Net;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Comm.Internal
{
	internal class TcpConnectionReestablisher : ConnectionReestablisherBase, ConnectionReestablisher
	{
		public readonly static int INITIAL_DISCOVERY_TIMEOUT;

		private string macAddr;

		protected string PortNumber
		{
			get
			{
				return ((TcpConnection)this.zebraPrinterConnection).PortNumber;
			}
		}

		static TcpConnectionReestablisher()
		{
			TcpConnectionReestablisher.INITIAL_DISCOVERY_TIMEOUT = 62;
		}

		public TcpConnectionReestablisher(Zebra.Sdk.Comm.Connection c, long thresholdTime) : base(c, thresholdTime)
		{
			try
			{
				this.macAddr = this.GetPrinterMacAddressViaDiscovery();
			}
			catch (Exception)
			{
			}
		}

		protected string GetConnectionAddress()
		{
			if (this.zebraPrinterConnection is TcpConnection)
			{
				return ((TcpConnection)this.zebraPrinterConnection).Address;
			}
			if (!(this.zebraPrinterConnection is MultichannelTcpConnection))
			{
				return "";
			}
			return ((TcpConnection)((MultichannelTcpConnection)this.zebraPrinterConnection).PrintingChannel).Address;
		}

		protected virtual Zebra.Sdk.Comm.Connection GetNewConnection(string destinationAddress)
		{
			if (this.zebraPrinterConnection is TcpStatusConnection)
			{
				return new TcpStatusConnection(destinationAddress, int.Parse(this.PortNumber));
			}
			return new TcpConnection(destinationAddress, int.Parse(this.PortNumber));
		}

		protected string GetPrinterMacAddressViaDiscovery()
		{
			string item = "";
			string connectionAddress = this.GetConnectionAddress();
			TcpConnectionReestablisher.SinglePrinterDiscoveryHandler singlePrinterDiscoveryHandler = new TcpConnectionReestablisher.SinglePrinterDiscoveryHandler();
			NetworkUtil.StartSinglePrinterDiscovery(connectionAddress, singlePrinterDiscoveryHandler);
			for (int i = 0; i < TcpConnectionReestablisher.INITIAL_DISCOVERY_TIMEOUT && !singlePrinterDiscoveryHandler.isFinished; i++)
			{
				Sleeper.Sleep((long)100);
			}
			if (singlePrinterDiscoveryHandler.printer != null)
			{
				item = singlePrinterDiscoveryHandler.printer.DiscoveryDataMap["HARDWARE_ADDRESS"];
			}
			return item;
		}

		protected string GetUpdatedFwVersion(string address)
		{
			TcpConnectionReestablisher.SinglePrinterDiscoveryHandler singlePrinterDiscoveryHandler = new TcpConnectionReestablisher.SinglePrinterDiscoveryHandler();
			NetworkUtil.StartSinglePrinterDiscovery(address, singlePrinterDiscoveryHandler);
			while (!singlePrinterDiscoveryHandler.isFinished)
			{
				base.TimeoutCheck();
				Sleeper.Sleep((long)100);
				if (!singlePrinterDiscoveryHandler.isFinished || singlePrinterDiscoveryHandler.printer != null)
				{
					continue;
				}
				singlePrinterDiscoveryHandler = new TcpConnectionReestablisher.SinglePrinterDiscoveryHandler();
				NetworkUtil.StartSinglePrinterDiscovery(address, singlePrinterDiscoveryHandler);
			}
			return singlePrinterDiscoveryHandler.printer.DiscoveryDataMap["FIRMWARE_VER"];
		}

		public override void ReestablishConnection(PrinterReconnectionHandler handler)
		{
			string comeOnlineViaSnmpAndSubnetDiscovery = this.WaitForPrinterToComeOnlineViaSnmpAndSubnetDiscovery(this.macAddr);
			Sleeper.Sleep((long)2500);
			this.WaitForConnectionToSucceed(comeOnlineViaSnmpAndSubnetDiscovery);
			Zebra.Sdk.Comm.Connection newConnection = this.GetNewConnection(comeOnlineViaSnmpAndSubnetDiscovery);
			newConnection.Open();
			string str = SGD.GET("appl.name", newConnection);
			ZebraPrinterLinkOs zebraPrinterLinkO = ZebraPrinterFactory.CreateLinkOsPrinter(ZebraPrinterFactory.GetInstance(newConnection));
			handler.PrinterOnline(zebraPrinterLinkO, str);
		}

		protected void WaitForConnectionToSucceed(string address)
		{
			bool flag = true;
			while (flag)
			{
				Zebra.Sdk.Comm.Connection newConnection = this.GetNewConnection(address);
				try
				{
					try
					{
						newConnection.Open();
						flag = false;
					}
					catch (ConnectionException)
					{
						base.TimeoutCheck();
					}
				}
				finally
				{
					try
					{
						newConnection.Close();
					}
					catch (ConnectionException)
					{
					}
				}
			}
		}

		protected string WaitForPrinterToComeOnlineViaSnmpAndSubnetDiscovery(string macAddress)
		{
			this.startTime = (long)Math.Abs(Environment.TickCount);
			TcpConnectionReestablisher.FullSubnetDiscoveryHandler fullSubnetDiscoveryHandler = new TcpConnectionReestablisher.FullSubnetDiscoveryHandler(macAddress);
			TcpConnectionReestablisher.SinglePrinterDiscoveryHandler singlePrinterDiscoveryHandler = new TcpConnectionReestablisher.SinglePrinterDiscoveryHandler();
			while (true)
			{
				base.TimeoutCheck();
				fullSubnetDiscoveryHandler.isFinished = false;
				singlePrinterDiscoveryHandler.isFinished = false;
				NetworkDiscoverer.FindPrinters(fullSubnetDiscoveryHandler);
				try
				{
					string simpleConnectionName = this.zebraPrinterConnection.SimpleConnectionName;
					NetworkDiscoverer.SubnetSearch(singlePrinterDiscoveryHandler, IPAddress.Parse(simpleConnectionName).ToString());
				}
				catch (Exception exception)
				{
					throw new DiscoveryException(string.Concat("Unknown Host: ", exception.Message));
				}
				while (!fullSubnetDiscoveryHandler.isFinished || !singlePrinterDiscoveryHandler.isFinished)
				{
					Sleeper.Sleep((long)100);
					if (fullSubnetDiscoveryHandler.isFinished && fullSubnetDiscoveryHandler.address != null)
					{
						return fullSubnetDiscoveryHandler.address;
					}
					if (!singlePrinterDiscoveryHandler.isFinished || singlePrinterDiscoveryHandler.printer == null)
					{
						continue;
					}
					return singlePrinterDiscoveryHandler.printer.Address;
				}
			}
		}

		private class FullSubnetDiscoveryHandler : DiscoveryHandler
		{
			public bool isFinished;

			public string address;

			private string macAddress;

			public FullSubnetDiscoveryHandler(string macAddress)
			{
				this.macAddress = macAddress;
			}

			public void DiscoveryError(string message)
			{
				this.isFinished = true;
			}

			public void DiscoveryFinished()
			{
				this.isFinished = true;
			}

			public void FoundPrinter(DiscoveredPrinter printer)
			{
				if (this.macAddress != null && printer.DiscoveryDataMap["HARDWARE_ADDRESS"].Equals(this.macAddress))
				{
					this.address = printer.Address;
					this.isFinished = true;
				}
			}
		}

		private class SinglePrinterDiscoveryHandler : DiscoveryHandler
		{
			public bool isFinished;

			public DiscoveredPrinter printer;

			public SinglePrinterDiscoveryHandler()
			{
			}

			public void DiscoveryError(string message)
			{
				this.isFinished = true;
			}

			public void DiscoveryFinished()
			{
				this.isFinished = true;
			}

			public void FoundPrinter(DiscoveredPrinter printer)
			{
				this.printer = printer;
				this.isFinished = true;
			}
		}
	}
}