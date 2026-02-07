using System;
using System.Collections.Generic;
using Zebra.Sdk.Device;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This interface defines increased capabilities of a Zebra Link-OS™ printer. Link-OS™ printers support many 
	///       features not supported by non-Link-OS™ Zebra printers.
	///       </summary>
	public interface ZebraPrinterLinkOs : ZebraPrinter, FileUtil, GraphicsUtil, FormatUtil, ToolsUtil, Zebra.Sdk.Device.Device, SettingsProvider, ProfileUtil, FontUtil, AlertProvider, FileUtilLinkOs, FormatUtilLinkOs, ToolsUtilLinkOs, FirmwareUpdaterLinkOs
	{
		/// <summary>
		///       Gets/sets the printer's SNMP get community name.
		///       </summary>
		string CommunityName
		{
			get;
			set;
		}

		/// <summary>
		///       Returns specific Link-OS™ information.
		///       </summary>
		Zebra.Sdk.Printer.LinkOsInformation LinkOsInformation
		{
			get;
		}

		/// <summary>
		///       Retrieve the TCP port status of the printer and returns a list of <c>TcpPortStatus</c> describing the open ports on the printer.
		///       </summary>
		/// <returns>List of open ports on the ZebraPrinter. Note: The open connection from the SDK will be listed.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an issue communicating with the printer (e.g. the connection is not open.)</exception>
		List<TcpPortStatus> GetPortStatus();
	}
}