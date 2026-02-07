using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Printer.Discovery.Internal;
using Zebra.Sdk.Settings.Internal;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Internal
{
	internal class LinkOsPrinterCreatorSgdOrJson
	{
		private LinkOsInformation linkosInfo;

		private PrinterLanguage language;

		public LinkOsPrinterCreatorSgdOrJson(PrinterLanguage language) : this(new LinkOsInformation(-1, -1), language)
		{
		}

		public LinkOsPrinterCreatorSgdOrJson(LinkOsInformation info) : this(info, null)
		{
		}

		public LinkOsPrinterCreatorSgdOrJson(LinkOsInformation info, PrinterLanguage language)
		{
			this.linkosInfo = info;
			this.language = language;
		}

		public ZebraPrinterLinkOs Create(Connection connection)
		{
			return this.Create(new ZebraPrinterZpl(connection));
		}

		public ZebraPrinterLinkOs Create(ZebraPrinter genericPrinter)
		{
			ZebraPrinterLinkOs zebraPrinterLinkO = null;
			try
			{
				zebraPrinterLinkO = (!(genericPrinter.Connection is MultichannelConnection) ? this.CreateLinkOsPrinterFromStandardConnection(genericPrinter) : this.CreateLinkOsPrinterFromMultiChannelConnection(genericPrinter));
			}
			catch (ArgumentNullException)
			{
			}
			catch (ZebraPrinterLanguageUnknownException)
			{
			}
			return zebraPrinterLinkO;
		}

		private ZebraPrinterLinkOs CreateLinkOsPrinterFromMultiChannelConnection(ZebraPrinter genericPrinter)
		{
			MultichannelConnection connection = (MultichannelConnection)genericPrinter.Connection;
			this.language = this.ObtainLanguage(connection);
			this.linkosInfo = this.ObtainVersion(connection);
			return new ZebraPrinterLinkOsImpl(genericPrinter, this.linkosInfo, this.language);
		}

		private ZebraPrinterLinkOs CreateLinkOsPrinterFromStandardConnection(ZebraPrinter genericPrinter)
		{
			this.language = this.ObtainLanguage(genericPrinter.Connection);
			this.linkosInfo = this.ObtainVersion(genericPrinter.Connection);
			return new ZebraPrinterLinkOsImpl(genericPrinter, this.linkosInfo, this.language);
		}

		private Dictionary<string, string> GetDiscoMapViaJson(Connection connection)
		{
			byte[] numArray = JsonHelper.BuildQuery(new List<string>()
			{
				SGDUtilities.DISCOVERY_NAME
			});
			try
			{
				byte[] numArray1 = Convert.FromBase64String(JsonHelper.ParseGetResponse(connection.SendAndWaitForValidResponse(numArray, connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, new JsonValidator()))[SGDUtilities.DISCOVERY_NAME].Split(new char[] { ':' })[0]);
				return this.ParseDiscoPacket(numArray1);
			}
			catch (ArgumentNullException)
			{
			}
			catch (DiscoveryPacketDecodeException)
			{
			}
			catch (ZebraIllegalArgumentException)
			{
			}
			return null;
		}

		private Dictionary<string, string> GetDiscoMapViaSgd(Connection connection)
		{
			Dictionary<string, string> strs;
			try
			{
				byte[] numArray = Convert.FromBase64String(SGD.GET(SGDUtilities.DISCOVERY_NAME, connection).Split(new char[] { ':' })[0]);
				strs = this.ParseDiscoPacket(numArray);
			}
			catch (DiscoveryPacketDecodeException)
			{
				return null;
			}
			return strs;
		}

		private PrinterLanguage GetLanguageViaJson(Connection c)
		{
			PrinterLanguage language;
			byte[] numArray = c.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"device.languages\":null}"), c.MaxTimeoutForRead, c.TimeToWaitForMoreData, new JsonValidator());
			try
			{
				language = PrinterLanguage.GetLanguage(StringUtilities.ConvertKeyValueJsonToMap(numArray)["device.languages"]);
			}
			catch (Exception)
			{
				throw new ZebraPrinterLanguageUnknownException(string.Concat("Zebra printer language could not be determined for ", c.ToString()));
			}
			return language;
		}

		private PrinterLanguage GetLanguageViaSgd(Connection c)
		{
			return PrinterLanguage.GetLanguage(SGD.GET("device.languages", c));
		}

		private LinkOsInformation GetLinkOsVersionFromDiscoMap(Dictionary<string, string> discoveryDataMap)
		{
			return new LinkOsInformation(StringUtilities.GetIntValueForKey(discoveryDataMap, "LINK_OS_MAJOR_VER"), StringUtilities.GetIntValueForKey(discoveryDataMap, "LINK_OS_MINOR_VER"));
		}

		private PrinterLanguage ObtainLanguage(Connection c)
		{
			if (!this.ShouldQueryLanguage())
			{
				return this.language;
			}
			return this.QueryPrinterLanguage(c);
		}

		private LinkOsInformation ObtainVersion(Connection c)
		{
			if (!this.ShouldQueryPrinter())
			{
				return this.linkosInfo;
			}
			return this.QueryVersionNumber(c);
		}

		private Dictionary<string, string> ParseDiscoPacket(byte[] discoveryPacketBytes)
		{
			Dictionary<string, string> discoveryDataMap = null;
			DiscoveredPrinter discoveredPrinterNetwork = DiscoveredPrinterNetworkFactory.GetDiscoveredPrinterNetwork(discoveryPacketBytes);
			if (DiscoveredPrinterNetworkFactory.IsLinkOsPrinter(discoveredPrinterNetwork))
			{
				discoveryDataMap = discoveredPrinterNetwork.DiscoveryDataMap;
			}
			return discoveryDataMap;
		}

		private PrinterLanguage QueryPrinterLanguage(Connection c)
		{
			MultichannelConnection multichannelConnection = c as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection != null)
			{
				if (multichannelConnection1.StatusChannel.Connected)
				{
					return this.GetLanguageViaJson(multichannelConnection1.StatusChannel);
				}
			}
			else if (c is StatusConnection)
			{
				return this.GetLanguageViaJson(c);
			}
			return this.GetLanguageViaSgd(c);
		}

		private Dictionary<string, string> QueryVersionInfoOverSingleChannel(Connection c)
		{
			Dictionary<string, string> discoMapViaJson;
			if (this.language != PrinterLanguage.ZPL)
			{
				discoMapViaJson = (!(c is StatusConnection) ? this.GetDiscoMapViaSgd(c) : this.GetDiscoMapViaJson(c));
			}
			else
			{
				discoMapViaJson = this.GetDiscoMapViaJson(c);
			}
			return discoMapViaJson;
		}

		private LinkOsInformation QueryVersionNumber(Connection c)
		{
			Dictionary<string, string> strs = null;
			MultichannelConnection multichannelConnection = c as MultichannelConnection;
			MultichannelConnection multichannelConnection1 = multichannelConnection;
			if (multichannelConnection == null)
			{
				strs = this.QueryVersionInfoOverSingleChannel(c);
			}
			else
			{
				strs = (!multichannelConnection1.StatusChannel.Connected ? this.QueryVersionInfoOverSingleChannel(multichannelConnection1.PrintingChannel) : this.GetDiscoMapViaJson(multichannelConnection1.StatusChannel));
			}
			return this.GetLinkOsVersionFromDiscoMap(strs);
		}

		private bool ShouldQueryLanguage()
		{
			return this.language == null;
		}

		private bool ShouldQueryPrinter()
		{
			return this.linkosInfo.Major < 0;
		}
	}
}