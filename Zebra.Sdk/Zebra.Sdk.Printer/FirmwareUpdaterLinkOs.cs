using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This is the interface for updating firmware on a Link-OS printer.
	///       </summary>
	public interface FirmwareUpdaterLinkOs
	{
		/// <summary>
		///       Update firmware on the printer using the default timeout of 10 minutes.
		///       </summary>
		/// <param name="firmwareFilePath">File path of firmware file.</param>
		/// <param name="handler">Callback for firmware updating status</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection can not be opened, is closed prematurely, or connection cannot be 
		///       established after firmware download is complete.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language can not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If an invalid firmware file is specified for the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while waiting for the printer to come back online.</exception>
		/// <exception cref="T:System.TimeoutException">If the maximum timeout is reached prior to the printer coming back online with the new
		///       firmware.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">Firmware file not found.</exception>
		void UpdateFirmware(string firmwareFilePath, FirmwareUpdateHandler handler);

		/// <summary>
		///       Update firmware on the printer using the specified <c>timeout</c>.
		///       </summary>
		/// <param name="firmwareFilePath">File path of firmware file.</param>
		/// <param name="timeout">Timeout in milliseconds. The minimum allowed timeout is 10 minutes (600000ms) due to the need to 
		///       reset the printer after flashing the firmware. If a timeout value less than the minimum is provided, the minimum will be 
		///       used instead.</param>
		/// <param name="handler">Callback for firmware updating status.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection can not be opened, is closed prematurely, or connection cannot be 
		///       established after firmware download is complete.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language can not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If an invalid firmware file is specified for the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while waiting for the printer to come back online.</exception>
		/// <exception cref="T:System.TimeoutException">If the maximum timeout is reached prior to the printer coming back online with the new
		///       firmware.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">Firmware file not found.</exception>
		void UpdateFirmware(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler);

		/// <summary>
		///       Update firmware on the printer, using the default timeout of 10 minutes, regardless of the firmware version
		///       currently on the printer.
		///       </summary>
		/// <param name="firmwareFilePath">File path of firmware file.</param>
		/// <param name="handler">Callback for firmware updating status.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection can not be opened, is closed prematurely, or connection cannot be 
		///       established after firmware download is complete.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language can not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If an invalid firmware file is specified for the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while waiting for the printer to come back online.</exception>
		/// <exception cref="T:System.TimeoutException">If the maximum timeout is reached prior to the printer coming back online with the new
		///       firmware.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">Firmware file not found.</exception>
		void UpdateFirmwareUnconditionally(string firmwareFilePath, FirmwareUpdateHandler handler);

		/// <summary>
		///       Update firmware on the printer, using the specified <c>timeout</c>, regardless of the firmware version 
		///       currently on the printer.
		///       </summary>
		/// <param name="firmwareFilePath">File path of firmware file.</param>
		/// <param name="timeout">Timeout in milliseconds. The minimum allowed timeout is 10 minutes (600000ms) due to the need to 
		///       reset the printer after flashing the firmware. If a timeout value less than the minimum is provided, the minimum will be 
		///       used instead.</param>
		/// <param name="handler">Callback for firmware updating status.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If the connection can not be opened, is closed prematurely, or connection cannot be 
		///       established after firmware download is complete.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException">If the printer language can not be determined.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If an invalid firmware file is specified for the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Printer.Discovery.DiscoveryException">If an error occurs while waiting for the printer to come back online.</exception>
		/// <exception cref="T:System.TimeoutException">If the maximum timeout is reached prior to the printer coming back online with the new
		///       firmware.</exception>
		/// <exception cref="T:System.IO.FileNotFoundException">Firmware file not found.</exception>
		void UpdateFirmwareUnconditionally(string firmwareFilePath, long timeout, FirmwareUpdateHandler handler);
	}
}