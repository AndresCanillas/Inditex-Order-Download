using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Printer.Discovery;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Printer.Discovery.Internal
{
	internal class DiscoveryPacketDecoderAdvanced
	{
		private readonly static int DISCOVERY_VERSION_OFFSET;

		private readonly static int ADVANCED_PACKET_FORMAT_OFFSET;

		private readonly static int COMPANY_ABBERVIATION_OFFSET;

		private readonly static int COMPANY_ABBERVIATION_SIZE;

		private readonly static int SYSTEM_NAME_OFFSET;

		private readonly static int SYSTEM_NAME_SIZE;

		private readonly static int PRODUCT_NAME_OFFSET;

		private readonly static int PRODUCT_NAME_SIZE;

		private readonly static int FW_VERSION_OFFSET;

		private readonly static int FW_VERSION_SIZE;

		private readonly static int LOCATION_OFFSET;

		private readonly static int LOCATION_SIZE;

		private readonly static int ERRORS_SEGMENT0_OFFSET;

		private readonly static int ERRORS_SEGMENT0_SIZE;

		private readonly static int ERRORS_SEGMENT1_OFFSET;

		private readonly static int ERRORS_SEGMENT1_SIZE;

		private readonly static int ERRORS_SEGMENT2_OFFSET;

		private readonly static int ERRORS_SEGMENT2_SIZE;

		private readonly static int WARNINGS_SEGMENT0_OFFSET;

		private readonly static int WARNINGS_SEGMENT0_SIZE;

		private readonly static int WARNINGS_SEGMENT1_OFFSET;

		private readonly static int WARNINGS_SEGMENT1_SIZE;

		private readonly static int WARNINGS_SEGMENT2_OFFSET;

		private readonly static int WARNINGS_SEGMENT2_SIZE;

		private readonly static int AVAILABLE_INTERFACES_BITFIELD_OFFSET;

		private readonly static int AVAILABLE_INTERFACES_BITFIELD_SIZE;

		private readonly static int DEVICE_UNIQUE_ID_OFFSET;

		private readonly static int DEVICE_UNIQUE_ID_SIZE;

		private readonly static int DNS_DOMAIN_OFFSET;

		private readonly static int DNS_DOMAIN_SIZE;

		private readonly static int ACTIVE_INTERFACE_OFFSET;

		private readonly static int ACTIVE_INTERFACE_SIZE;

		private readonly static int MAC_ADDRESS_OFFSET;

		private readonly static int MAC_ADDRESS_SIZE;

		private readonly static int IP_ACQUISITION_PROTO_OFFSET;

		private readonly static int IP_ACQUISITION_PROTO_SIZE;

		private readonly static int IP_ADDRESS_OFFSET;

		private readonly static int IP_ADDRESS_SIZE;

		private readonly static int SUBNET_MASK_OFFSET;

		private readonly static int SUBNET_MASK_SIZE;

		private readonly static int GATEWAY_MASK_OFFSET;

		private readonly static int GATEWAY_MASK_SIZE;

		private readonly static int PORT_OFFSET;

		private readonly static int PORT_SIZE;

		private readonly static int AVAILABLE_PROTOCOLS_OFFSET;

		private readonly static int AVAILABLE_PROTOCOLS_SIZE;

		private readonly static int PRIMARY_LANGUAGE_OFFSET;

		private readonly static int PRIMARY_LANGUAGE_SIZE;

		private readonly static int AVAILABLE_LANGUAGES_BITFIELD_OFFSET;

		private readonly static int AVAILABLE_LANGUAGES_BITFIELD_SIZE;

		private readonly static int AVAILABLE_SECONDARY_LANGUAGES_BITFIELD_OFFSET;

		private readonly static int AVAILABLE_SECONDARY_LANGUAGES_BITFIELD_SIZE;

		private readonly static int DOTS_PER_MM_OFFSET;

		private readonly static int DOTS_PER_MM_SIZE;

		private readonly static int DOTS_PER_DOT_ROW_OFFSET;

		private readonly static int DOTS_PER_DOT_ROW_SIZE;

		private readonly static int LABEL_LENGTH_OFFSET;

		private readonly static int LABEL_LENGTH_SIZE;

		private readonly static int LABEL_WIDTH_OFFSET;

		private readonly static int LABEL_WIDTH_SIZE;

		private readonly static int DARKNESS_OFFSET;

		private readonly static int DARKNESS_SIZE;

		private readonly static int MEDIA_TYPE_OFFSET;

		private readonly static int MEDIA_TYPE_SIZE;

		private readonly static int PRINT_METHOD_OFFSET;

		private readonly static int PRINT_METHOD_SIZE;

		private readonly static int PRINT_MODE_OFFSET;

		private readonly static int PRINT_MODE_SIZE;

		private readonly static int ODOMETER_TOTAL_OFFSET;

		private readonly static int ODOMETER_TOTAL_SIZE;

		private readonly static int ODOMETER_MARKER_ONE_OFFSET;

		private readonly static int ODOMETER_MARKER_ONE_SIZE;

		private readonly static int ODOMETER_MARKER_TWO_OFFSET;

		private readonly static int ODOMETER_MARKER_TWO_SIZE;

		private readonly static int NUM_OF_LABELS_IN_BATCH_OFFSET;

		private readonly static int NUM_OF_LABELS_IN_BATCH_SIZE;

		private readonly static int LABELS_QUEUED_OFFSET;

		private readonly static int LABELS_QUEUED_SIZE;

		private readonly static int ZBI_ENABLED_OFFSET;

		private readonly static int ZBI_ENABLED_SIZE;

		private readonly static int ZBI_STATE_OFFSET;

		private readonly static int ZBI_STATE_SIZE;

		private readonly static int ZBI_MAJOR_VERSION_OFFSET;

		private readonly static int ZBI_MINOR_VERSION_OFFSET;

		private readonly static int PRINT_HEAD_WIDTH_OFFSET;

		private readonly static int PRINT_HEAD_WIDTH_SIZE;

		private readonly static int JSON_PORT_OFFSET;

		private readonly static int JSON_PORT_SIZE;

		private readonly static int LINK_OS_MAJOR_VER_OFFSET;

		private readonly static int LINK_OS_MINOR_VER_OFFSET;

		private readonly static int AVS_INI_VERSION_OFFSET;

		private readonly static int AVS_INI_VERSION_SIZE;

		private readonly static int PROCESSOR_ID_OFFSET;

		private readonly static int PROCESSOR_ID_SIZE;

		private readonly static int TLS_RAW_PORT_OFFSET;

		private readonly static int TLS_RAW_PORT_SIZE;

		private readonly static int TLS_JSON_PORT_OFFSET;

		private readonly static int TLS_JSON_PORT_SIZE;

		private readonly static int WIRED_8021X_SECURITY_SETTING_OFFSET;

		private readonly static int WIRED_8021X_SECURITY_SETTING_SIZE;

		private readonly static int MIN_PACKET_SIZE;

		private byte[] rawDiscoveryPacket;

		static DiscoveryPacketDecoderAdvanced()
		{
			DiscoveryPacketDecoderAdvanced.DISCOVERY_VERSION_OFFSET = 3;
			DiscoveryPacketDecoderAdvanced.ADVANCED_PACKET_FORMAT_OFFSET = 4;
			DiscoveryPacketDecoderAdvanced.COMPANY_ABBERVIATION_OFFSET = 8;
			DiscoveryPacketDecoderAdvanced.COMPANY_ABBERVIATION_SIZE = 5;
			DiscoveryPacketDecoderAdvanced.SYSTEM_NAME_OFFSET = 13;
			DiscoveryPacketDecoderAdvanced.SYSTEM_NAME_SIZE = 63;
			DiscoveryPacketDecoderAdvanced.PRODUCT_NAME_OFFSET = 76;
			DiscoveryPacketDecoderAdvanced.PRODUCT_NAME_SIZE = 32;
			DiscoveryPacketDecoderAdvanced.FW_VERSION_OFFSET = 108;
			DiscoveryPacketDecoderAdvanced.FW_VERSION_SIZE = 16;
			DiscoveryPacketDecoderAdvanced.LOCATION_OFFSET = 124;
			DiscoveryPacketDecoderAdvanced.LOCATION_SIZE = 36;
			DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT0_OFFSET = 160;
			DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT0_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT1_OFFSET = 164;
			DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT1_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT2_OFFSET = 168;
			DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT2_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT0_OFFSET = 172;
			DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT0_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT1_OFFSET = 176;
			DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT1_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT2_OFFSET = 180;
			DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT2_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_INTERFACES_BITFIELD_OFFSET = 184;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_INTERFACES_BITFIELD_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.DEVICE_UNIQUE_ID_OFFSET = 188;
			DiscoveryPacketDecoderAdvanced.DEVICE_UNIQUE_ID_SIZE = 32;
			DiscoveryPacketDecoderAdvanced.DNS_DOMAIN_OFFSET = 220;
			DiscoveryPacketDecoderAdvanced.DNS_DOMAIN_SIZE = 100;
			DiscoveryPacketDecoderAdvanced.ACTIVE_INTERFACE_OFFSET = 320;
			DiscoveryPacketDecoderAdvanced.ACTIVE_INTERFACE_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.MAC_ADDRESS_OFFSET = 324;
			DiscoveryPacketDecoderAdvanced.MAC_ADDRESS_SIZE = 6;
			DiscoveryPacketDecoderAdvanced.IP_ACQUISITION_PROTO_OFFSET = 330;
			DiscoveryPacketDecoderAdvanced.IP_ACQUISITION_PROTO_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.IP_ADDRESS_OFFSET = 332;
			DiscoveryPacketDecoderAdvanced.IP_ADDRESS_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.SUBNET_MASK_OFFSET = 336;
			DiscoveryPacketDecoderAdvanced.SUBNET_MASK_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.GATEWAY_MASK_OFFSET = 340;
			DiscoveryPacketDecoderAdvanced.GATEWAY_MASK_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.PORT_OFFSET = 344;
			DiscoveryPacketDecoderAdvanced.PORT_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_PROTOCOLS_OFFSET = 346;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_PROTOCOLS_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.PRIMARY_LANGUAGE_OFFSET = 348;
			DiscoveryPacketDecoderAdvanced.PRIMARY_LANGUAGE_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_LANGUAGES_BITFIELD_OFFSET = 352;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_LANGUAGES_BITFIELD_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_SECONDARY_LANGUAGES_BITFIELD_OFFSET = 356;
			DiscoveryPacketDecoderAdvanced.AVAILABLE_SECONDARY_LANGUAGES_BITFIELD_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.DOTS_PER_MM_OFFSET = 360;
			DiscoveryPacketDecoderAdvanced.DOTS_PER_MM_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.DOTS_PER_DOT_ROW_OFFSET = 362;
			DiscoveryPacketDecoderAdvanced.DOTS_PER_DOT_ROW_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.LABEL_LENGTH_OFFSET = 364;
			DiscoveryPacketDecoderAdvanced.LABEL_LENGTH_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.LABEL_WIDTH_OFFSET = 366;
			DiscoveryPacketDecoderAdvanced.LABEL_WIDTH_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.DARKNESS_OFFSET = 368;
			DiscoveryPacketDecoderAdvanced.DARKNESS_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.MEDIA_TYPE_OFFSET = 370;
			DiscoveryPacketDecoderAdvanced.MEDIA_TYPE_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.PRINT_METHOD_OFFSET = 372;
			DiscoveryPacketDecoderAdvanced.PRINT_METHOD_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.PRINT_MODE_OFFSET = 374;
			DiscoveryPacketDecoderAdvanced.PRINT_MODE_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.ODOMETER_TOTAL_OFFSET = 376;
			DiscoveryPacketDecoderAdvanced.ODOMETER_TOTAL_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_ONE_OFFSET = 380;
			DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_ONE_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_TWO_OFFSET = 384;
			DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_TWO_SIZE = 4;
			DiscoveryPacketDecoderAdvanced.NUM_OF_LABELS_IN_BATCH_OFFSET = 388;
			DiscoveryPacketDecoderAdvanced.NUM_OF_LABELS_IN_BATCH_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.LABELS_QUEUED_OFFSET = 390;
			DiscoveryPacketDecoderAdvanced.LABELS_QUEUED_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.ZBI_ENABLED_OFFSET = 392;
			DiscoveryPacketDecoderAdvanced.ZBI_ENABLED_SIZE = 1;
			DiscoveryPacketDecoderAdvanced.ZBI_STATE_OFFSET = 393;
			DiscoveryPacketDecoderAdvanced.ZBI_STATE_SIZE = 1;
			DiscoveryPacketDecoderAdvanced.ZBI_MAJOR_VERSION_OFFSET = 394;
			DiscoveryPacketDecoderAdvanced.ZBI_MINOR_VERSION_OFFSET = 395;
			DiscoveryPacketDecoderAdvanced.PRINT_HEAD_WIDTH_OFFSET = 396;
			DiscoveryPacketDecoderAdvanced.PRINT_HEAD_WIDTH_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.JSON_PORT_OFFSET = 398;
			DiscoveryPacketDecoderAdvanced.JSON_PORT_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.LINK_OS_MAJOR_VER_OFFSET = 400;
			DiscoveryPacketDecoderAdvanced.LINK_OS_MINOR_VER_OFFSET = 401;
			DiscoveryPacketDecoderAdvanced.AVS_INI_VERSION_OFFSET = 402;
			DiscoveryPacketDecoderAdvanced.AVS_INI_VERSION_SIZE = 6;
			DiscoveryPacketDecoderAdvanced.PROCESSOR_ID_OFFSET = 408;
			DiscoveryPacketDecoderAdvanced.PROCESSOR_ID_SIZE = 8;
			DiscoveryPacketDecoderAdvanced.TLS_RAW_PORT_OFFSET = 416;
			DiscoveryPacketDecoderAdvanced.TLS_RAW_PORT_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.TLS_JSON_PORT_OFFSET = 418;
			DiscoveryPacketDecoderAdvanced.TLS_JSON_PORT_SIZE = 2;
			DiscoveryPacketDecoderAdvanced.WIRED_8021X_SECURITY_SETTING_OFFSET = 420;
			DiscoveryPacketDecoderAdvanced.WIRED_8021X_SECURITY_SETTING_SIZE = 1;
			DiscoveryPacketDecoderAdvanced.MIN_PACKET_SIZE = 395;
		}

		public DiscoveryPacketDecoderAdvanced(byte[] discoveryPacket)
		{
			this.rawDiscoveryPacket = discoveryPacket;
		}

		private int GetAdvancedDiscoveryVer()
		{
			return Convert.ToInt32(this.rawDiscoveryPacket[DiscoveryPacketDecoderAdvanced.ADVANCED_PACKET_FORMAT_OFFSET]);
		}

		private HashSet<PrinterInterface> GetAvailableInterfaces()
		{
			return PrinterInterface.GetEnumSetFromBitmask(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.AVAILABLE_INTERFACES_BITFIELD_OFFSET, DiscoveryPacketDecoderAdvanced.AVAILABLE_INTERFACES_BITFIELD_SIZE));
		}

		private HashSet<DiscoveredPrinterLanguage> GetAvailableLanguages()
		{
			return DiscoveredPrinterLanguage.GetEnumSetFromBitmask(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.AVAILABLE_LANGUAGES_BITFIELD_OFFSET, DiscoveryPacketDecoderAdvanced.AVAILABLE_LANGUAGES_BITFIELD_SIZE));
		}

		private HashSet<NetworkProtocol> GetAvailableNetworkProtocols()
		{
			return NetworkProtocol.GetEnumSetFromBitmask(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.AVAILABLE_PROTOCOLS_OFFSET, DiscoveryPacketDecoderAdvanced.AVAILABLE_PROTOCOLS_SIZE));
		}

		private HashSet<SecondaryPrinterLanguage> GetAvailableSecondaryLanguages()
		{
			return SecondaryPrinterLanguage.GetEnumSetFromBitmask(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.AVAILABLE_SECONDARY_LANGUAGES_BITFIELD_OFFSET, DiscoveryPacketDecoderAdvanced.AVAILABLE_SECONDARY_LANGUAGES_BITFIELD_SIZE));
		}

		private string GetAvsIniVersion()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.AVS_INI_VERSION_OFFSET, DiscoveryPacketDecoderAdvanced.AVS_INI_VERSION_SIZE);
		}

		private string GetCompanyAbbreviation()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.COMPANY_ABBERVIATION_OFFSET, DiscoveryPacketDecoderAdvanced.COMPANY_ABBERVIATION_SIZE);
		}

		private PrinterInterface GetCurrentlyActiveNetworkInterface()
		{
			return this.ParseNetworkInterface(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ACTIVE_INTERFACE_OFFSET, DiscoveryPacketDecoderAdvanced.ACTIVE_INTERFACE_SIZE);
		}

		private short GetDarkness()
		{
			return (short)PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.DARKNESS_OFFSET, DiscoveryPacketDecoderAdvanced.DARKNESS_SIZE);
		}

		private string GetDeviceUniqueId()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.DEVICE_UNIQUE_ID_OFFSET, DiscoveryPacketDecoderAdvanced.DEVICE_UNIQUE_ID_SIZE);
		}

		public DiscoveredPrinterNetwork GetDiscoveredPrinterNetworkAdvanced()
		{
			if ((int)this.rawDiscoveryPacket.Length < DiscoveryPacketDecoderAdvanced.MIN_PACKET_SIZE)
			{
				throw new DiscoveryPacketDecodeException("Unable to parse the supplied discovery packet due to an invalid discovery packet length");
			}
			return new DiscoveredPrinterNetwork(this.GetDiscoveryDataMap());
		}

		private Dictionary<string, string> GetDiscoveryDataMap()
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			int advancedDiscoveryVer = this.GetAdvancedDiscoveryVer();
			strs.Add("DISCOVERY_VER", Convert.ToString(this.GetDiscoveryVersion()));
			strs.Add("ADVANCED_DISCOVERY_VER", Convert.ToString(advancedDiscoveryVer));
			strs.Add("COMPANY_ABBREVIATION", this.GetCompanyAbbreviation());
			strs.Add("SYSTEM_NAME", this.GetSystemName());
			strs.Add("PRODUCT_NAME", this.GetProductName());
			strs.Add("FIRMWARE_VER", this.GetFirmwareVersion());
			strs.Add("LOCATION", this.GetLocation());
			strs.Add("ERRORS", this.IterateSetAndStringConcatValues<PrinterError>(this.GetErrors()));
			strs.Add("WARNINGS", this.IterateSetAndStringConcatValues<PrinterWarning>(this.GetWarnings()));
			strs.Add("ACTIVE_NETWORK_INTERFACE", this.GetCurrentlyActiveNetworkInterface().ToString());
			string deviceUniqueId = this.GetDeviceUniqueId();
			strs.Add("SERIAL_NUMBER", deviceUniqueId);
			strs.Add("DEVICE_UNIQUE_ID", deviceUniqueId);
			strs.Add("DNS_DOMAIN", this.GetDnsDomain());
			strs.Add("HARDWARE_ADDRESS", this.GetMacAddress());
			strs.Add("USING_NET_PROTOCOL", (this.GetUsingNetProtocol() ? "true" : "false"));
			strs.Add("DNS_NAME", this.GetSystemName());
			strs.Add("IP_ACQUISITION_PROTOCOL", this.GetIpAcquisitionProtocol().ToString());
			strs.Add("ADDRESS", this.GetIpAddress());
			strs.Add("SUBNET_MASK", this.GetSubnetMask());
			strs.Add("GATEWAY", this.GetGateway());
			strs.Add("PORT_NUMBER", Convert.ToString(this.GetPort()));
			strs.Add("AVAILABLE_NETWORK_PROTOCOLS", this.IterateSetAndStringConcatValues<NetworkProtocol>(this.GetAvailableNetworkProtocols()));
			strs.Add("AVAILABLE_INTERFACES", this.IterateSetAndStringConcatValues<PrinterInterface>(this.GetAvailableInterfaces()));
			strs.Add("PRIMARY_LANGUAGE", this.GetPrimaryLanguage().ToString());
			strs.Add("AVAILABLE_LANGUAGES", this.IterateSetAndStringConcatValues<DiscoveredPrinterLanguage>(this.GetAvailableLanguages()));
			strs.Add("SECONDARY_PRINTER_LANGUAGE", this.IterateSetAndStringConcatValues<SecondaryPrinterLanguage>(this.GetAvailableSecondaryLanguages()));
			strs.Add("DOTS_PER_MM", Convert.ToString(this.GetDotsPerMM()));
			strs.Add("DOTS_PER_ROW", Convert.ToString(this.GetDotsPerDotRow()));
			strs.Add("LABEL_LENGTH", Convert.ToString(this.GetLabelLength()));
			strs.Add("LABEL_WIDTH", Convert.ToString(this.GetLabelWidth()));
			strs.Add("DARKNESS", Convert.ToString(this.GetDarkness()));
			strs.Add("PRINTER_MEDIA_TYPE", this.GetMediaType().ToString());
			strs.Add("PRINT_METHOD", this.GetPrintMethod().ToString());
			strs.Add("PRINT_MODE", this.GetPrintMode().ToString());
			strs.Add("ODOMETER_TOTAL_LABEL_COUNT", Convert.ToString(this.GetOdometerTotalLabelCount()));
			strs.Add("ODOMETER_MEDIAMARKER_COUNT_ONE", Convert.ToString(this.GetOdometerMarkerCountOne()));
			strs.Add("ODOMETER_MEDIAMARKER_COUNT_TWO", Convert.ToString(this.GetOdometerMarkerCountTwo()));
			strs.Add("NUMBER_LABELS_REMAIN_IN_BATCH", Convert.ToString(this.GetNumberOfLabelsRemainingInBatch()));
			strs.Add("NUMBER_LABELS_QUEUED", Convert.ToString(this.GetNumberOfLabelsQueued()));
			strs.Add("ZBI_ENABLED", (this.GetZbiEnabled() ? "true" : "false"));
			strs.Add("ZBI_STATE", this.GetZbiState().ToString());
			strs.Add("ZBI_MAJOR_VER", Convert.ToString(this.GetZbiMajorVersion()));
			strs.Add("ZBI_MINOR_VER", Convert.ToString(this.GetZbiMinorVersion()));
			if (advancedDiscoveryVer >= 1)
			{
				strs.Add("PRINT_HEAD_WIDTH", Convert.ToString(this.GetPrintHeadWidth()));
			}
			if (advancedDiscoveryVer >= 2)
			{
				strs.Add("JSON_PORT_NUMBER", Convert.ToString(this.GetJsonPort()));
				strs.Add("LINK_OS_MAJOR_VER", Convert.ToString(this.GetLinkOsMajorVer()));
				strs.Add("LINK_OS_MINOR_VER", Convert.ToString(this.GetLinkOsMinorVer()));
			}
			if (advancedDiscoveryVer >= 3)
			{
				strs.Add("AVS_INI_VER", this.GetAvsIniVersion());
			}
			if (advancedDiscoveryVer >= 4)
			{
				strs.Add("PROCESSOR_ID", this.GetProcessorId());
				strs.Add("TLS_RAW_PORT_NUMBER", Convert.ToString(this.GetTlsRawPortNumber()));
				strs.Add("TLS_JSON_PORT_NUMBER", Convert.ToString(this.GetTlsJsonPortNumber()));
				strs.Add("WIRED_8021X_SECURITY_SETTING", this.GetWired8021xSecuritySetting().ToString());
			}
			strs.Add("PORT_STATUS", this.GetPortStatus().ToString());
			strs.Add("PRODUCT_NUMBER", "");
			strs.Add("PORT_NAME", "");
			strs.Add("DATE_CODE", "");
			return strs;
		}

		private int GetDiscoveryVersion()
		{
			return Convert.ToByte(this.rawDiscoveryPacket[DiscoveryPacketDecoderAdvanced.DISCOVERY_VERSION_OFFSET]);
		}

		private string GetDnsDomain()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.DNS_DOMAIN_OFFSET, DiscoveryPacketDecoderAdvanced.DNS_DOMAIN_SIZE);
		}

		private short GetDotsPerDotRow()
		{
			return (short)PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.DOTS_PER_DOT_ROW_OFFSET, DiscoveryPacketDecoderAdvanced.DOTS_PER_DOT_ROW_SIZE);
		}

		private short GetDotsPerMM()
		{
			return (short)PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.DOTS_PER_MM_OFFSET, DiscoveryPacketDecoderAdvanced.DOTS_PER_MM_SIZE);
		}

		private HashSet<PrinterError> GetErrors()
		{
			int num = PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT0_OFFSET, DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT0_SIZE);
			int num1 = PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT1_OFFSET, DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT1_SIZE);
			int num2 = PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT2_OFFSET, DiscoveryPacketDecoderAdvanced.ERRORS_SEGMENT2_SIZE);
			HashSet<PrinterError> enumSetFromBitmask = PrinterError.GetEnumSetFromBitmask(0, num);
			enumSetFromBitmask.UnionWith(PrinterError.GetEnumSetFromBitmask(1, num1));
			enumSetFromBitmask.UnionWith(PrinterError.GetEnumSetFromBitmask(2, num2));
			return enumSetFromBitmask;
		}

		private string GetFirmwareVersion()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.FW_VERSION_OFFSET, DiscoveryPacketDecoderAdvanced.FW_VERSION_SIZE);
		}

		private string GetGateway()
		{
			return PacketParsingUtil.ParseAddress(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.GATEWAY_MASK_OFFSET, DiscoveryPacketDecoderAdvanced.GATEWAY_MASK_SIZE);
		}

		private IPAcquisitionProtocol GetIpAcquisitionProtocol()
		{
			return IPAcquisitionProtocol.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.IP_ACQUISITION_PROTO_OFFSET, DiscoveryPacketDecoderAdvanced.IP_ACQUISITION_PROTO_SIZE));
		}

		private string GetIpAddress()
		{
			return PacketParsingUtil.ParseAddress(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.IP_ADDRESS_OFFSET, DiscoveryPacketDecoderAdvanced.IP_ADDRESS_SIZE);
		}

		private int GetJsonPort()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.JSON_PORT_OFFSET, DiscoveryPacketDecoderAdvanced.JSON_PORT_SIZE);
		}

		private short GetLabelLength()
		{
			return (short)PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.LABEL_LENGTH_OFFSET, DiscoveryPacketDecoderAdvanced.LABEL_LENGTH_SIZE);
		}

		private short GetLabelWidth()
		{
			return (short)PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.LABEL_WIDTH_OFFSET, DiscoveryPacketDecoderAdvanced.LABEL_WIDTH_SIZE);
		}

		private int GetLinkOsMajorVer()
		{
			return Convert.ToInt32(this.rawDiscoveryPacket[DiscoveryPacketDecoderAdvanced.LINK_OS_MAJOR_VER_OFFSET]);
		}

		private int GetLinkOsMinorVer()
		{
			return Convert.ToInt32(this.rawDiscoveryPacket[DiscoveryPacketDecoderAdvanced.LINK_OS_MINOR_VER_OFFSET]);
		}

		private string GetLocation()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.LOCATION_OFFSET, DiscoveryPacketDecoderAdvanced.LOCATION_SIZE);
		}

		private string GetMacAddress()
		{
			return PacketParsingUtil.ParseGeneralByte(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.MAC_ADDRESS_OFFSET, DiscoveryPacketDecoderAdvanced.MAC_ADDRESS_SIZE);
		}

		private PrinterMediaType GetMediaType()
		{
			return PrinterMediaType.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.MEDIA_TYPE_OFFSET, DiscoveryPacketDecoderAdvanced.MEDIA_TYPE_SIZE));
		}

		private int GetNumberOfLabelsQueued()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.LABELS_QUEUED_OFFSET, DiscoveryPacketDecoderAdvanced.LABELS_QUEUED_SIZE);
		}

		private int GetNumberOfLabelsRemainingInBatch()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.NUM_OF_LABELS_IN_BATCH_OFFSET, DiscoveryPacketDecoderAdvanced.NUM_OF_LABELS_IN_BATCH_SIZE);
		}

		private int GetOdometerMarkerCountOne()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_ONE_OFFSET, DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_ONE_SIZE);
		}

		private int GetOdometerMarkerCountTwo()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_TWO_OFFSET, DiscoveryPacketDecoderAdvanced.ODOMETER_MARKER_TWO_SIZE);
		}

		private int GetOdometerTotalLabelCount()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ODOMETER_TOTAL_OFFSET, DiscoveryPacketDecoderAdvanced.ODOMETER_TOTAL_SIZE);
		}

		private int GetPort()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PORT_OFFSET, DiscoveryPacketDecoderAdvanced.PORT_SIZE);
		}

		private PrinterPortStatus GetPortStatus()
		{
			HashSet<PrinterError> errors = this.GetErrors();
			if (errors.Contains(PrinterError.HEAD_OPEN))
			{
				return PrinterPortStatus.DOOR_OPEN;
			}
			if (errors.Contains(PrinterError.MEDIA_OUT))
			{
				return PrinterPortStatus.PAPER_OUT;
			}
			if (errors.Contains(PrinterError.PAPER_FEED_ERROR))
			{
				return PrinterPortStatus.PAPER_JAMMED;
			}
			if (errors.Count == 0)
			{
				return PrinterPortStatus.ONLINE;
			}
			return PrinterPortStatus.PRINTER_ERROR;
		}

		private DiscoveredPrinterLanguage GetPrimaryLanguage()
		{
			return DiscoveredPrinterLanguage.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PRIMARY_LANGUAGE_OFFSET, DiscoveryPacketDecoderAdvanced.PRIMARY_LANGUAGE_SIZE));
		}

		private int GetPrintHeadWidth()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PRINT_HEAD_WIDTH_OFFSET, DiscoveryPacketDecoderAdvanced.PRINT_HEAD_WIDTH_SIZE);
		}

		private PrintMethod GetPrintMethod()
		{
			return PrintMethod.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PRINT_METHOD_OFFSET, DiscoveryPacketDecoderAdvanced.PRINT_METHOD_SIZE));
		}

		private PrintMode GetPrintMode()
		{
			return PrintMode.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PRINT_MODE_OFFSET, DiscoveryPacketDecoderAdvanced.PRINT_MODE_SIZE));
		}

		private string GetProcessorId()
		{
			return PacketParsingUtil.ParseGeneralByte(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PROCESSOR_ID_OFFSET, DiscoveryPacketDecoderAdvanced.PROCESSOR_ID_SIZE);
		}

		private string GetProductName()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.PRODUCT_NAME_OFFSET, DiscoveryPacketDecoderAdvanced.PRODUCT_NAME_SIZE);
		}

		private string GetSubnetMask()
		{
			return PacketParsingUtil.ParseAddress(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.SUBNET_MASK_OFFSET, DiscoveryPacketDecoderAdvanced.SUBNET_MASK_SIZE);
		}

		private string GetSystemName()
		{
			return PacketParsingUtil.ParseGeneralString(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.SYSTEM_NAME_OFFSET, DiscoveryPacketDecoderAdvanced.SYSTEM_NAME_SIZE);
		}

		private int GetTlsJsonPortNumber()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.TLS_JSON_PORT_OFFSET, DiscoveryPacketDecoderAdvanced.TLS_JSON_PORT_SIZE);
		}

		private int GetTlsRawPortNumber()
		{
			return PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.TLS_RAW_PORT_OFFSET, DiscoveryPacketDecoderAdvanced.TLS_RAW_PORT_SIZE);
		}

		private bool GetUsingNetProtocol()
		{
			return this.GetIpAcquisitionProtocol() != IPAcquisitionProtocol.STATIC;
		}

		private HashSet<PrinterWarning> GetWarnings()
		{
			int num = PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT0_OFFSET, DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT0_SIZE);
			int num1 = PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT1_OFFSET, DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT1_SIZE);
			int num2 = PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT2_OFFSET, DiscoveryPacketDecoderAdvanced.WARNINGS_SEGMENT2_SIZE);
			HashSet<PrinterWarning> enumSetFromBitmask = PrinterWarning.GetEnumSetFromBitmask(0, num);
			enumSetFromBitmask.UnionWith(PrinterWarning.GetEnumSetFromBitmask(1, num1));
			enumSetFromBitmask.UnionWith(PrinterWarning.GetEnumSetFromBitmask(2, num2));
			return enumSetFromBitmask;
		}

		private Wired8021xSecuritySetting GetWired8021xSecuritySetting()
		{
			return Wired8021xSecuritySetting.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.WIRED_8021X_SECURITY_SETTING_OFFSET, DiscoveryPacketDecoderAdvanced.WIRED_8021X_SECURITY_SETTING_SIZE));
		}

		private bool GetZbiEnabled()
		{
			return PacketParsingUtil.ParseBoolean(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ZBI_ENABLED_OFFSET, DiscoveryPacketDecoderAdvanced.ZBI_ENABLED_SIZE);
		}

		private int GetZbiMajorVersion()
		{
			return this.rawDiscoveryPacket[DiscoveryPacketDecoderAdvanced.ZBI_MAJOR_VERSION_OFFSET];
		}

		private int GetZbiMinorVersion()
		{
			return this.rawDiscoveryPacket[DiscoveryPacketDecoderAdvanced.ZBI_MINOR_VERSION_OFFSET];
		}

		private ZbiState GetZbiState()
		{
			return ZbiState.IntToEnum(PacketParsingUtil.ParseInteger(this.rawDiscoveryPacket, DiscoveryPacketDecoderAdvanced.ZBI_STATE_OFFSET, DiscoveryPacketDecoderAdvanced.ZBI_STATE_SIZE));
		}

		private string IterateSetAndStringConcatValues<T>(HashSet<T> set)
		{
			StringBuilder stringBuilder = new StringBuilder();
			HashSet<T>.Enumerator enumerator = set.GetEnumerator();
			if (enumerator.MoveNext())
			{
				T current = enumerator.Current;
				stringBuilder.Append(current.ToString());
				while (enumerator.MoveNext())
				{
					stringBuilder.Append(",");
					current = enumerator.Current;
					stringBuilder.Append(current.ToString());
					if (!enumerator.MoveNext())
					{
						continue;
					}
					stringBuilder.Append(",");
					current = enumerator.Current;
					stringBuilder.Append(current.ToString());
				}
			}
			return stringBuilder.ToString();
		}

		public static void Main(string[] args)
		{
			Console.WriteLine((new DiscoveryPacketDecoderAdvanced(Convert.FromBase64String("OiwuBAIBAAFaQlIAAFhYUUxWMTIzNAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAFpUQyBRTG4yMjAtMjAzZHBpIENQQ0wAAAAAAAAAAAAAVjY4LjIwLjAyUDM0Nzc2LQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAW1YWFFMVjEyMzQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHplYnJhLmxhbgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAdNRjzhAAAKUBYn////AApQFgEX1QH/AAAAAgAAAAcAAAADAAgR8AfuAKgAZAABAAAAAAAAAukAAAKaAAAnzgAAAAABAgIBAkAj8AQA"))).GetZbiState());
		}

		private PrinterInterface ParseNetworkInterface(byte[] rawDiscoveryPacket, int activeInterfaceOffset, int activeInterfaceSize)
		{
			return PrinterInterface.IntToEnum(PacketParsingUtil.ParseInteger(rawDiscoveryPacket, activeInterfaceOffset, activeInterfaceSize));
		}
	}
}