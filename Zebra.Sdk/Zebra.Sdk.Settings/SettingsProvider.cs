using System;
using System.Collections.Generic;

namespace Zebra.Sdk.Settings
{
	/// <summary>
	///       Interface that provides access to device related settings.
	///       </summary>
	public interface SettingsProvider
	{
		/// <summary>
		///       Retrieve all settings and their attributes.
		///       </summary>
		/// <returns>Map of setting IDs and setting attributes contained in the profile</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be retrieved</exception>
		Dictionary<string, Setting> GetAllSettings();

		/// <summary>
		///       Retrieves all of the device's setting values.
		///       </summary>
		/// <returns>Values of all the settings provided by the device.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be loaded</exception>
		Dictionary<string, string> GetAllSettingValues();

		/// <summary>
		///       Retrieve all of the setting identifiers for a device.
		///       </summary>
		/// <returns>Set of identifiers available for a device.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be loaded</exception>
		HashSet<string> GetAvailableSettings();

		/// <summary>
		///       Retrieves the allowable range for a setting.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <returns>The setting's range as a string</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		string GetSettingRange(string settingId);

		/// <summary>
		///       Retrieves the device's setting values for a list of setting IDs.
		///       </summary>
		/// <param name="listOfSettings">List of setting IDs.</param>
		/// <returns>The settings' values.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings could not be retrieved.</exception>
		Dictionary<string, string> GetSettingsValues(List<string> listOfSettings);

		/// <summary>
		///       Returns the data type of the setting.
		///       </summary>
		/// <param name="settingId">The setting id</param>
		/// <returns>The data type of the setting (e.g. string, bool, enum, etc.)</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		string GetSettingType(string settingId);

		/// <summary>
		///       Retrieves the device's setting value for a setting id.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <returns>The setting's value.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting could not be retrieved.</exception>
		string GetSettingValue(string settingId);

		/// <summary>
		///       Returns true if the setting is read only.
		///       </summary>
		/// <param name="settingId">The setting id</param>
		/// <returns>True if the setting is read only</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		bool IsSettingReadOnly(string settingId);

		/// <summary>
		///       Returns true if value is valid for the given setting.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <param name="value">The setting's value</param>
		/// <returns>True if value is valid for the given setting.</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		bool IsSettingValid(string settingId, string value);

		/// <summary>
		///       Returns true if the setting is write only.
		///       </summary>
		/// <param name="settingId">The setting id</param>
		/// <returns>True if the setting is write only</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting does not exist</exception>
		bool IsSettingWriteOnly(string settingId);

		/// <summary>
		///       Change or retrieve printer settings. 
		///       </summary>
		/// <param name="settingValuePairs">The settings to change or retrieve</param>
		/// <returns>Results of the setting commands</returns>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If a setting is malformed, or one or more settings could not be set.</exception>
		Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs);

		/// <summary>
		///       Sets the setting to the given value.
		///       </summary>
		/// <param name="settingId">The setting id.</param>
		/// <param name="value">The setting's value</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the setting is read only, does not exist, or if the setting could not be set</exception>
		void SetSetting(string settingId, string value);

		/// <summary>
		///       Set more than one setting.
		///       </summary>
		/// <param name="settingValuePairs">Map a setting ID to the new value for the setting.</param>
		/// <exception cref="T:Zebra.Sdk.Comm.ConnectionException">If there is an error communicating with the printer.</exception>
		/// <exception cref="T:Zebra.Sdk.Settings.SettingsException">If the settings cannot be sent to the device.</exception>
		void SetSettings(Dictionary<string, string> settingValuePairs);
	}
}