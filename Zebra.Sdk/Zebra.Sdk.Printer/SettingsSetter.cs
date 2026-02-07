using System;
using System.Collections.Generic;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Comm.Internal;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Printer
{
	/// <summary>
	///       A utility class used to wrap with a map and send settings commands to a connection. 
	///       </summary>
	public class SettingsSetter
	{
		/// <summary>
		///   <markup>
		///     <include item="SMCAutoDocConstructor">
		///       <parameter>Zebra.Sdk.Printer.SettingsSetter</parameter>
		///     </include>
		///   </markup>
		/// </summary>
		public SettingsSetter()
		{
		}

		/// <summary>
		///       Sends the <c>settingsToSet</c> to the <c>destinationDevice</c> and then returns the updated setting values.
		///       </summary>
		/// <param name="destinationDevice">The connection string.</param>
		/// <param name="settingsToSet">The settings map to send to the printer.</param>
		/// <returns>The settings' values after the map has been sent to the printer.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting could not be set or retrieved.</exception>
		public static Dictionary<string, string> Process(string destinationDevice, Dictionary<string, string> settingsToSet)
		{
			Dictionary<string, string> strs = null;
			Connection connection = null;
			try
			{
				try
				{
					connection = ConnectionBuilderInternal.Build(destinationDevice);
					connection.Open();
					strs = ZebraPrinterFactory.CreateLinkOsPrinter(ZebraPrinterFactory.GetInstance(connection)).ProcessSettingsViaMap(settingsToSet);
				}
				catch (ZebraPrinterLanguageUnknownException)
				{
				}
			}
			finally
			{
				if (connection != null)
				{
					connection.Close();
				}
			}
			return strs;
		}
	}
}