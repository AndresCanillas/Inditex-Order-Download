using System;

namespace Zebra.Sdk.Util.Internal
{
	internal class SGDUtilities
	{
		public readonly static string APPL_NAME;

		public readonly static string HOST_STATUS;

		public readonly static string DISCOVERY_NAME;

		public readonly static string DEVICE_LANGUAGES;

		public readonly static string DEVICE_RESET;

		public readonly static string PRINTER_RESET_JSON;

		public readonly static string NETWORK_RESET;

		public readonly static string NETWORK_RESET_JSON;

		public readonly static string PRINTER_DEFAULT;

		public readonly static string PRINTER_DEFAULT_JSON;

		public readonly static string NETWORK_DEFAULT;

		public readonly static string NETWORK_DEFAULT_JSON;

		public readonly static string CALIBRATE_PRINTER;

		public readonly static string CALIBRATE_PRINTER_JSON;

		static SGDUtilities()
		{
			SGDUtilities.APPL_NAME = "appl.name";
			SGDUtilities.HOST_STATUS = "device.host_status";
			SGDUtilities.DISCOVERY_NAME = "ip.discovery_packet";
			SGDUtilities.DEVICE_LANGUAGES = "device.languages";
			SGDUtilities.DEVICE_RESET = "device.reset";
			SGDUtilities.PRINTER_RESET_JSON = string.Concat("{}{\"", SGDUtilities.DEVICE_RESET, "\":\"\"}");
			SGDUtilities.NETWORK_RESET = "device.prompted_network_reset";
			SGDUtilities.NETWORK_RESET_JSON = string.Concat("{}{\"", SGDUtilities.NETWORK_RESET, "\":\"y\"}");
			SGDUtilities.PRINTER_DEFAULT = "ezpl.restore_defaults";
			SGDUtilities.PRINTER_DEFAULT_JSON = string.Concat("{}{\"", SGDUtilities.PRINTER_DEFAULT, "\":\"reload printer\"}");
			SGDUtilities.NETWORK_DEFAULT = "device.prompted_default_network";
			SGDUtilities.NETWORK_DEFAULT_JSON = string.Concat("{}{\"", SGDUtilities.NETWORK_DEFAULT, "\":\"y\"}");
			SGDUtilities.CALIBRATE_PRINTER = "zpl.calibrate";
			SGDUtilities.CALIBRATE_PRINTER_JSON = string.Concat("{}{\"", SGDUtilities.CALIBRATE_PRINTER, "\":\"\"}");
		}

		public SGDUtilities()
		{
		}

		public static string DecorateWithGetCommand(string command)
		{
			return string.Concat("! U1 getvar \"", command, "\"", StringUtilities.CRLF);
		}
	}
}