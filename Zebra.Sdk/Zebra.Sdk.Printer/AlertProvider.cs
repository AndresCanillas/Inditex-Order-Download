using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       This is an utility class for getting/setting alerts on a printer.
	///       </summary>
	public interface AlertProvider
	{
		/// <summary>
		///       Configures an alert to be triggered when the alert's condition occurs or becomes resolved.
		///       </summary>
		/// <param name="alert">The alert to trigger when it's condition occurs or becomes resolved.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		void ConfigureAlert(PrinterAlert alert);

		/// <summary>
		///       Configures a list of alerts to be triggered when their conditions occur or become resolved.
		///       </summary>
		/// <param name="alerts">The list of alerts to trigger when their conditions occur or become resolved.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		void ConfigureAlerts(List<PrinterAlert> alerts);

		/// <summary>
		///       A list of objects detailing the alert configuration of a printer.
		///       </summary>
		/// <returns>A list of alert objects currently configured on the printer.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		/// <exception cref="T:Zebra.Sdk.Device.ZebraIllegalArgumentException"></exception>
		List<PrinterAlert> GetConfiguredAlerts();

		/// <summary>
		///       Removes a configured alert from a printer. They may be reconfigured via the configureAlert(s) methods.
		///       </summary>
		/// <param name="alert">Alert to be removed from the configuration</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		void RemoveAlert(PrinterAlert alert);

		/// <summary>
		///       Removes all alerts currently configured on a printer. They may be reconfigured via the configureAlert(s) methods.
		///       </summary>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException"></exception>
		void RemoveAllAlerts();
	}
}