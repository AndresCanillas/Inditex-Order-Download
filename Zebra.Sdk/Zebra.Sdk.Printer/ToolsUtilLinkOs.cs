using System;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       Utility class for performing Link-OS printer actions.
	///       </summary>
	public interface ToolsUtilLinkOs
	{
		/// <summary>
		///       Send the print directory label command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void PrintDirectoryLabel();

		/// <summary>
		///       Send the print network configuration command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void PrintNetworkConfigurationLabel();

		/// <summary>
		///       Sends the network reset command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void ResetNetwork();

		/// <summary>
		///       Send the restore network defaults command to the printer.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		void RestoreNetworkDefaults();

		/// <summary>
		///       Set the RTC time and date on the printer.
		///       </summary>
		/// <param name="dateTime">Date and or time in the proper format (MM-dd-yyyy, HH:mm:ss, or MM-dd-yyyy HH:mm:ss).</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException">If the format of <c>dateTime</c> is invalid.</exception>
		void SetClock(string dateTime);
	}
}