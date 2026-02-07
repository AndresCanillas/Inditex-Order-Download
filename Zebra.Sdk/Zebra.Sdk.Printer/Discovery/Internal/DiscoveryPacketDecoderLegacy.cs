using System;
using System.Collections.Generic;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class DiscoveryPacketDecoderLegacy
	{
		private readonly static int DISCOVERY_VERSION_OFFSET;

		private readonly static int PRODUCT_NUMBER_OFFSET;

		private readonly static int PRODUCT_NUMBER_SIZE;

		private readonly static int PRODUCT_NAME_OFFSET;

		private readonly static int PRODUCT_NAME_SIZE;

		private readonly static int DATE_CODE_OFFSET;

		private readonly static int DATE_CODE_SIZE;

		private readonly static int FW_VERSION_OFFSET;

		private readonly static int FW_VERSION_SIZE;

		private readonly static int COMPANY_ABBREVIATION_OFFSET;

		private readonly static int COMPANY_ABBREVIATION_SIZE;

		private readonly static int HW_ADDRESS_OFFSET;

		private readonly static int HW_ADDRESS_SIZE;

		private readonly static int SERIAL_NUM_OFFSET;

		private readonly static int SERIAL_NUM_SIZE;

		private readonly static int USING_NET_PROTOCOL_OFFSET;

		private readonly static int USING_NET_PROTOCOL_SIZE;

		private readonly static int IP_ADDRESS_OFFSET;

		private readonly static int IP_ADDRESS_SIZE;

		private readonly static int SUBNET_MASK_OFFSET;

		private readonly static int SUBNET_MASK_SIZE;

		private readonly static int DEFAULT_GATEWAY_OFFSET;

		private readonly static int DEFAULT_GATEWAY_SIZE;

		private readonly static int SYSTEM_NAME_OFFSET;

		private readonly static int SYSTEM_NAME_SIZE;

		private readonly static int GET_COMMUNITY_NAME_OFFSET;

		private readonly static int GET_COMMUNITY_NAME_SIZE;

		private readonly static int SET_COMMUNITY_NAME_OFFSET;

		private readonly static int SET_COMMUNITY_NAME_SIZE;

		private readonly static int PORT_STATUS_OFFSET;

		private readonly static int PORT_STATUS_SIZE;

		private readonly static int PORT_NAME_OFFSET;

		private readonly static int PORT_NAME_SIZE;

		private readonly static int MIN_PACKET_SIZE;

		private byte[] rawDiscoveryPacket;

		static DiscoveryPacketDecoderLegacy()
		{
			DiscoveryPacketDecoderLegacy.DISCOVERY_VERSION_OFFSET = 3;
			DiscoveryPacketDecoderLegacy.PRODUCT_NUMBER_OFFSET = 4;
			DiscoveryPacketDecoderLegacy.PRODUCT_NUMBER_SIZE = 8;
			DiscoveryPacketDecoderLegacy.PRODUCT_NAME_OFFSET = 12;
			DiscoveryPacketDecoderLegacy.PRODUCT_NAME_SIZE = 20;
			DiscoveryPacketDecoderLegacy.DATE_CODE_OFFSET = 32;
			DiscoveryPacketDecoderLegacy.DATE_CODE_SIZE = 7;
			DiscoveryPacketDecoderLegacy.FW_VERSION_OFFSET = 39;
			DiscoveryPacketDecoderLegacy.FW_VERSION_SIZE = 10;
			DiscoveryPacketDecoderLegacy.COMPANY_ABBREVIATION_OFFSET = 49;
			DiscoveryPacketDecoderLegacy.COMPANY_ABBREVIATION_SIZE = 5;
			DiscoveryPacketDecoderLegacy.HW_ADDRESS_OFFSET = 54;
			DiscoveryPacketDecoderLegacy.HW_ADDRESS_SIZE = 6;
			DiscoveryPacketDecoderLegacy.SERIAL_NUM_OFFSET = 60;
			DiscoveryPacketDecoderLegacy.SERIAL_NUM_SIZE = 10;
			DiscoveryPacketDecoderLegacy.USING_NET_PROTOCOL_OFFSET = 70;
			DiscoveryPacketDecoderLegacy.USING_NET_PROTOCOL_SIZE = 2;
			DiscoveryPacketDecoderLegacy.IP_ADDRESS_OFFSET = 72;
			DiscoveryPacketDecoderLegacy.IP_ADDRESS_SIZE = 4;
			DiscoveryPacketDecoderLegacy.SUBNET_MASK_OFFSET = 76;
			DiscoveryPacketDecoderLegacy.SUBNET_MASK_SIZE = 4;
			DiscoveryPacketDecoderLegacy.DEFAULT_GATEWAY_OFFSET = 80;
			DiscoveryPacketDecoderLegacy.DEFAULT_GATEWAY_SIZE = 4;
			DiscoveryPacketDecoderLegacy.SYSTEM_NAME_OFFSET = 84;
			DiscoveryPacketDecoderLegacy.SYSTEM_NAME_SIZE = 25;
			DiscoveryPacketDecoderLegacy.GET_COMMUNITY_NAME_OFFSET = 212;
			DiscoveryPacketDecoderLegacy.GET_COMMUNITY_NAME_SIZE = 32;
			DiscoveryPacketDecoderLegacy.SET_COMMUNITY_NAME_OFFSET = 244;
			DiscoveryPacketDecoderLegacy.SET_COMMUNITY_NAME_SIZE = 32;
			DiscoveryPacketDecoderLegacy.PORT_STATUS_OFFSET = 358;
			DiscoveryPacketDecoderLegacy.PORT_STATUS_SIZE = 1;
			DiscoveryPacketDecoderLegacy.PORT_NAME_OFFSET = 359;
			DiscoveryPacketDecoderLegacy.PORT_NAME_SIZE = 16;
			DiscoveryPacketDecoderLegacy.MIN_PACKET_SIZE = 375;
		}

		public DiscoveryPacketDecoderLegacy(byte[] discoveryPacket)
		{
			this.rawDiscoveryPacket = discoveryPacket;
		}

		private byte[] CopyOfRange(byte[] originalData, int from, int to)
		{
			byte[] numArray = new byte[to - from + 1];
			for (int i = from; i <= to; i++)
			{
				numArray[i - from] = originalData[i];
			}
			return numArray;
		}

		private string GetCompanyAbbreviation()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.COMPANY_ABBREVIATION_OFFSET, DiscoveryPacketDecoderLegacy.COMPANY_ABBREVIATION_SIZE);
		}

		private string GetDateCode()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.DATE_CODE_OFFSET, DiscoveryPacketDecoderLegacy.DATE_CODE_SIZE);
		}

		public DiscoveredPrinterNetwork GetDiscoveredPrinterNetwork()
		{
			if ((int)this.rawDiscoveryPacket.Length < DiscoveryPacketDecoderLegacy.MIN_PACKET_SIZE)
			{
				throw new DiscoveryPacketDecodeException("Unable to parse the supplied discovery packet due to an invalid discovery packet length");
			}
			return new DiscoveredPrinterNetwork(this.GetDiscoveryDataMap());
		}

		private Dictionary<string, string> GetDiscoveryDataMap()
		{
			return new Dictionary<string, string>()
			{
				{ "PORT_NUMBER", Convert.ToString(this.GetPrinterPort()) },
				{ "DNS_NAME", this.GetDnsName() },
				{ "ADDRESS", this.GetIpAddress() },
				{ "COMPANY_ABBREVIATION", this.GetCompanyAbbreviation() },
				{ "DISCOVERY_VER", Convert.ToString(this.GetDiscoveryVersion()) },
				{ "PRODUCT_NUMBER", this.GetProductNumber() },
				{ "PRODUCT_NAME", this.GetProductName() },
				{ "DATE_CODE", this.GetDateCode() },
				{ "FIRMWARE_VER", this.GetFirmwareVersion() },
				{ "HARDWARE_ADDRESS", this.GetHardwareAddress() },
				{ "SERIAL_NUMBER", this.GetSerialNumber() },
				{ "USING_NET_PROTOCOL", (this.GetUsingNetProtocol() ? "true" : "false") },
				{ "SUBNET_MASK", this.GetSubnetmask() },
				{ "GATEWAY", this.GetGateway() },
				{ "SYSTEM_NAME", this.GetSystemName() },
				{ "PORT_NAME", this.GetPortName() },
				{ "PORT_STATUS", this.GetPortStatus().ToString() },
				{ "ENCRYPTED_GET_COMMUNITY_NAME", this.GetGetCommunityNameAsHexString() },
				{ "ENCRYPTED_SET_COMMUNITY_NAME", this.GetSetCommunityNameAsHexString() }
			};
		}

		private int GetDiscoveryVersion()
		{
			return Convert.ToInt32(this.rawDiscoveryPacket[DiscoveryPacketDecoderLegacy.DISCOVERY_VERSION_OFFSET]);
		}

		private string GetDnsName()
		{
			return this.GetSystemName();
		}

		private string GetFirmwareVersion()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.FW_VERSION_OFFSET, DiscoveryPacketDecoderLegacy.FW_VERSION_SIZE);
		}

		private string GetGateway()
		{
			return PacketParsingUtil.ParseAddress(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.DEFAULT_GATEWAY_OFFSET, DiscoveryPacketDecoderLegacy.DEFAULT_GATEWAY_SIZE);
		}

		private string GetGetCommunityNameAsHexString()
		{
			return StringUtilities.ByteArrayToHexString(this.CopyOfRange(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.GET_COMMUNITY_NAME_OFFSET, DiscoveryPacketDecoderLegacy.GET_COMMUNITY_NAME_OFFSET + DiscoveryPacketDecoderLegacy.GET_COMMUNITY_NAME_SIZE));
		}

		private string GetHardwareAddress()
		{
			return PacketParsingUtil.ParseGeneralByte(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.HW_ADDRESS_OFFSET, DiscoveryPacketDecoderLegacy.HW_ADDRESS_SIZE);
		}

		private string GetIpAddress()
		{
			return PacketParsingUtil.ParseAddress(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.IP_ADDRESS_OFFSET, DiscoveryPacketDecoderLegacy.IP_ADDRESS_SIZE);
		}

		private string GetPortName()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.PORT_NAME_OFFSET, DiscoveryPacketDecoderLegacy.PORT_NAME_SIZE);
		}

		private PrinterPortStatus GetPortStatus()
		{
			return DiscoveryPacketDecoderLegacy.ParseStatus(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.PORT_STATUS_OFFSET, DiscoveryPacketDecoderLegacy.PORT_STATUS_SIZE);
		}

		private int GetPrinterPort()
		{
			string productName = this.GetProductName();
			int num = 9100;
			if (productName.StartsWith("QL") || productName.StartsWith("RW") || productName.StartsWith("MZ") || productName.StartsWith("P4T") || productName.StartsWith("MQ") || productName.StartsWith("MU"))
			{
				num = 6101;
			}
			return num;
		}

		private string GetProductName()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.PRODUCT_NAME_OFFSET, DiscoveryPacketDecoderLegacy.PRODUCT_NAME_SIZE);
		}

		private string GetProductNumber()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.PRODUCT_NUMBER_OFFSET, DiscoveryPacketDecoderLegacy.PRODUCT_NUMBER_SIZE);
		}

		private string GetSerialNumber()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.SERIAL_NUM_OFFSET, DiscoveryPacketDecoderLegacy.SERIAL_NUM_SIZE);
		}

		private string GetSetCommunityNameAsHexString()
		{
			return StringUtilities.ByteArrayToHexString(this.CopyOfRange(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.SET_COMMUNITY_NAME_OFFSET, DiscoveryPacketDecoderLegacy.SET_COMMUNITY_NAME_OFFSET + DiscoveryPacketDecoderLegacy.SET_COMMUNITY_NAME_SIZE));
		}

		private string GetSubnetmask()
		{
			return PacketParsingUtil.ParseAddress(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.SUBNET_MASK_OFFSET, DiscoveryPacketDecoderLegacy.SUBNET_MASK_SIZE);
		}

		private string GetSystemName()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.SYSTEM_NAME_OFFSET, DiscoveryPacketDecoderLegacy.SYSTEM_NAME_SIZE);
		}

		private bool GetUsingNetProtocol()
		{
			return PacketParsingUtil.ParseBoolean(this.rawDiscoveryPacket, DiscoveryPacketDecoderLegacy.USING_NET_PROTOCOL_OFFSET, DiscoveryPacketDecoderLegacy.USING_NET_PROTOCOL_SIZE);
		}

		private static PrinterPortStatus ParseStatus(byte[] data, int index, int length)
		{
			return PrinterPortStatus.IntToEnum(Convert.ToInt32(data[index]));
		}
	}
}