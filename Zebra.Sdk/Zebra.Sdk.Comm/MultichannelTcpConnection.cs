using System;
using System.Collections.Generic;
using System.Linq;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Comm
{
	/// <summary>
	///       Establishes a Multichannel TCP connection to a device.
	///       </summary>
	public class MultichannelTcpConnection : MultichannelConnection
	{
		/// <summary>
		///       The default Multichannel printing port for Link-OS devices.
		///       </summary>
		public readonly static int DEFAULT_MULTICHANNEL_PRINTING_PORT;

		/// <summary>
		///       The default Multichannel status port for Link-OS devices.
		///       </summary>
		public readonly static int DEFAULT_MULTICHANNEL_STATUS_PORT;

		protected string ConnectionBuilderPrefix
		{
			get
			{
				return "TCP_MULTI";
			}
		}

		/// <summary>
		///       Return the IP address as the description.
		///       </summary>
		public override string SimpleConnectionName
		{
			get
			{
				return ((TcpConnection)this.raw).SimpleConnectionName;
			}
		}

		static MultichannelTcpConnection()
		{
			MultichannelTcpConnection.DEFAULT_MULTICHANNEL_PRINTING_PORT = TcpConnection.DEFAULT_ZPL_TCP_PORT;
			MultichannelTcpConnection.DEFAULT_MULTICHANNEL_STATUS_PORT = 9200;
		}

		protected MultichannelTcpConnection(ConnectionInfo connectionInfo)
		{
			string myData = connectionInfo.GetMyData();
			List<string> matches = RegexUtil.GetMatches(string.Concat("^\\s*((?i)", this.ConnectionBuilderPrefix, ":)?([\\d]{1,3}.[\\d]{1,3}.[\\d]{1,3}.[\\d]{1,3})(:([\\d]{1,5}))?(:([\\d]{1,5}))?\\s*$"), myData);
			if (!matches.Any<string>())
			{
				if (myData.Contains("zebra.com/apps/r/nfc?") && myData.Contains("mB="))
				{
					throw new NotMyConnectionDataException(string.Concat("TCP Connection doesn't understand ", myData));
				}
				matches = RegexUtil.GetMatches(string.Concat("^\\s*((?i)", this.ConnectionBuilderPrefix, ":)?([^:]+)(:([\\d]{1,5}))?(:([\\d]{1,5}))?\\s*$"), myData);
				if (!matches.Any<string>())
				{
					throw new NotMyConnectionDataException(string.Concat(this.ConnectionBuilderPrefix, " Connection doesn't understand ", myData));
				}
			}
			string item = matches[2];
			int dEFAULTMULTICHANNELPRINTINGPORT = MultichannelTcpConnection.DEFAULT_MULTICHANNEL_PRINTING_PORT;
			try
			{
				dEFAULTMULTICHANNELPRINTINGPORT = int.Parse(matches[4]);
			}
			catch (Exception)
			{
			}
			int dEFAULTMULTICHANNELSTATUSPORT = MultichannelTcpConnection.DEFAULT_MULTICHANNEL_STATUS_PORT;
			try
			{
				dEFAULTMULTICHANNELSTATUSPORT = int.Parse(matches[6]);
			}
			catch (Exception)
			{
			}
			this.Init(item, dEFAULTMULTICHANNELPRINTINGPORT, dEFAULTMULTICHANNELSTATUSPORT, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA);
		}

		/// <summary>
		///       Initializes a new instance of the <c>MultichannelTcpConnection</c> class.
		///       </summary>
		/// <param name="discoveredPrinter">The discovered printer.</param>
		public MultichannelTcpConnection(DiscoveredPrinter discoveredPrinter) : this(discoveredPrinter, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA)
		{
		}

		/// <summary>
		///       Initializes a new instance of the <c>MultichannelTcpConnection</c> class.
		///       </summary>
		/// <param name="discoveredPrinter">The discovered printer.</param>
		/// <param name="maxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received.</param>
		/// <param name="timeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read.</param>
		public MultichannelTcpConnection(DiscoveredPrinter discoveredPrinter, int maxTimeoutForRead, int timeToWaitForMoreData) : this(discoveredPrinter, maxTimeoutForRead, timeToWaitForMoreData, maxTimeoutForRead, timeToWaitForMoreData)
		{
		}

		/// <summary>
		///       Initializes a new instance of the <c>MultichannelTcpConnection</c> class.
		///       </summary>
		/// <param name="discoveredPrinter">The discovered printer.</param>
		/// <param name="printingChannelMaxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received on the printing channel.</param>
		/// <param name="printingChannelTimeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read on the printing channel.</param>
		/// <param name="statusChannelMaxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received on the status channel.</param>
		/// <param name="statusChannelTimeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read on the status channel.</param>
		/// <exception cref="T:System.ArgumentException">If <c>discoveredPrinter</c> is not a valid Link-OS printer.</exception>
		public MultichannelTcpConnection(DiscoveredPrinter discoveredPrinter, int printingChannelMaxTimeoutForRead, int printingChannelTimeToWaitForMoreData, int statusChannelMaxTimeoutForRead, int statusChannelTimeToWaitForMoreData)
		{
			Dictionary<string, string> discoveryDataMap = discoveredPrinter.DiscoveryDataMap;
			if (discoveryDataMap == null || discoveryDataMap.Count == 0)
			{
				throw new ArgumentException("The DiscoveredPrinter argument does not appear to be a Link-OS printer");
			}
			try
			{
				string stringValueForKey = StringUtilities.GetStringValueForKey(discoveryDataMap, "ADDRESS");
				int intValueForKey = StringUtilities.GetIntValueForKey(discoveryDataMap, "PORT_NUMBER");
				int num = StringUtilities.GetIntValueForKey(discoveryDataMap, "JSON_PORT_NUMBER");
				this.Init(stringValueForKey, intValueForKey, num, printingChannelMaxTimeoutForRead, printingChannelTimeToWaitForMoreData, statusChannelMaxTimeoutForRead, statusChannelTimeToWaitForMoreData);
			}
			catch (Exception)
			{
				throw new ArgumentException("The DiscoveredPrinter argument does not appear to be a Link-OS printer");
			}
		}

		/// <summary>
		///       Initializes a new instance of the <c>MultichannelTcpConnection</c> class.
		///       </summary>
		/// <param name="ipAddress">The IP Address or DNS Hostname.</param>
		/// <param name="printingPort">The printing port number.</param>
		/// <param name="statusPort">The status port number.</param>
		public MultichannelTcpConnection(string ipAddress, int printingPort, int statusPort) : this(ipAddress, printingPort, statusPort, ConnectionA.DEFAULT_MAX_TIMEOUT_FOR_READ, ConnectionA.DEFAULT_TIME_TO_WAIT_FOR_MORE_DATA)
		{
		}

		/// <summary>
		///       Initializes a new instance of the <c>MultichannelTcpConnection</c> class.
		///       </summary>
		/// <param name="ipAddress">The IP Address or DNS Hostname.</param>
		/// <param name="printingPort">The printing port number.</param>
		/// <param name="statusPort">The status port number.</param>
		/// <param name="maxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received.</param>
		/// <param name="timeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read.</param>
		public MultichannelTcpConnection(string ipAddress, int printingPort, int statusPort, int maxTimeoutForRead, int timeToWaitForMoreData) : this(ipAddress, printingPort, statusPort, maxTimeoutForRead, timeToWaitForMoreData, maxTimeoutForRead, timeToWaitForMoreData)
		{
		}

		/// <summary>
		///       Initializes a new instance of the <c>MultichannelTcpConnection</c> class.
		///       </summary>
		/// <param name="ipAddress">The IP Address or DNS Hostname.</param>
		/// <param name="printingPort">The printing port number.</param>
		/// <param name="statusPort">The status port number.</param>
		/// <param name="printingChannelMaxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received on the printing channel.</param>
		/// <param name="printingChannelTimeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read on the printing channel.</param>
		/// <param name="statusChannelMaxTimeoutForRead">The maximum time, in milliseconds, to wait for any data to be received on the status channel.</param>
		/// <param name="statusChannelTimeToWaitForMoreData">The maximum time, in milliseconds, to wait in-between reads after the initial read on the status channel.</param>
		public MultichannelTcpConnection(string ipAddress, int printingPort, int statusPort, int printingChannelMaxTimeoutForRead, int printingChannelTimeToWaitForMoreData, int statusChannelMaxTimeoutForRead, int statusChannelTimeToWaitForMoreData)
		{
			this.Init(ipAddress, printingPort, statusPort, printingChannelMaxTimeoutForRead, printingChannelTimeToWaitForMoreData, statusChannelMaxTimeoutForRead, statusChannelTimeToWaitForMoreData);
		}

		/// <summary>
		///       Returns a <c>ConnectionReestablisher</c> which allows for easy recreation of a connection which may have been closed.
		///       </summary>
		/// <param name="thresholdTime">How long the Connection reestablisher will wait before attempting to reconnection to the printer.</param>
		/// <returns>Instance of <see cref="T:Zebra.Sdk.Comm.ConnectionReestablisher" /></returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the ConnectionReestablisher could not be created.</exception>
		public override ConnectionReestablisher GetConnectionReestablisher(long thresholdTime)
		{
			return new MultichannelTcpConnectionReestablisher(this, thresholdTime);
		}

		protected virtual void Init(string ipAddress, int printingPort, int statusPort, int printingChannelMaxTimeoutForRead, int printingChannelTimeToWaitForMoreData, int statusChannelMaxTimeoutForRead, int statusChannelTimeToWaitForMoreData)
		{
			this.raw = new TcpConnection(ipAddress, printingPort, printingChannelMaxTimeoutForRead, printingChannelTimeToWaitForMoreData);
			this.settings = new TcpStatusConnection(ipAddress, statusPort, statusChannelMaxTimeoutForRead, statusChannelTimeToWaitForMoreData);
		}

		/// <summary>
		///       The <c>Address</c>, <c>PrintingPort</c>, and <c>StatusPort</c> are the parameters which were passed into the constructor.
		///       </summary>
		/// <returns>
		///   <c>TCP_MULTI</c>:[Address]:[PrintingPort]:[StatusPort]</returns>
		public override string ToString()
		{
			return string.Concat(new string[] { this.ConnectionBuilderPrefix, ":", ((TcpConnection)this.raw).Address, ":", ((TcpConnection)this.raw).PortNumber, ":", ((TcpStatusConnection)this.settings).PortNumber });
		}
	}
}