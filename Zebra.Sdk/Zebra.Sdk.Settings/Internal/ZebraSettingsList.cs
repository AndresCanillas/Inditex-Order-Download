using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Settings.Internal
{
	internal abstract class ZebraSettingsList : ZebraSettingsListI
	{
		protected OrderedDictionary allSettings = new OrderedDictionary();

		protected ZebraSettingsList()
		{
		}

		protected void CheckSettingsList(Dictionary<string, string> settingValuePairs)
		{
			string str = "";
			OrderedDictionary allSettings = this.GetAllSettings();
			foreach (string key in settingValuePairs.Keys)
			{
				if (allSettings.Contains(key))
				{
					continue;
				}
				str = string.Concat(str, key, ", ");
			}
			if (!string.IsNullOrEmpty(str))
			{
				throw new SettingsException(string.Concat("The following settings are invalid: [", str.Substring(0, str.Length - 2), "]"));
			}
		}

		protected List<string> FilterOutUnreadableSettings(List<string> settingIds)
		{
			List<string> strs = new List<string>();
			foreach (string settingId in settingIds)
			{
				if (!this.GetAllSettings().Contains(settingId) || this.GetSettingById(settingId).IsWriteOnly)
				{
					continue;
				}
				strs.Add(settingId);
			}
			if (strs.Count == 0)
			{
				throw new SettingsException("Found no valid settings to retrieve.");
			}
			return strs;
		}

		public HashSet<string> GetAllSettingIds()
		{
			return this.GetSettingsKeys();
		}

		public OrderedDictionary GetAllSettings()
		{
			if (this.allSettings == null || this.allSettings.Count == 0)
			{
				this.Refresh();
			}
			return this.allSettings;
		}

		public Dictionary<string, string> GetAllSettingValues()
		{
			this.Refresh();
			Dictionary<string, string> strs = new Dictionary<string, string>();
			foreach (string settingsKey in this.GetSettingsKeys())
			{
				strs.Add(settingsKey, ((Setting)this.GetAllSettings()[settingsKey]).Value);
			}
			return strs;
		}

		public Setting GetSetting(string settingId)
		{
			return this.GetSettingById(settingId);
		}

		protected Setting GetSettingById(string settingId)
		{
			OrderedDictionary allSettings = this.GetAllSettings();
			if (!allSettings.Contains(settingId))
			{
				throw new SettingsException(string.Concat("Setting [", settingId, "] not found."));
			}
			return (Setting)allSettings[settingId];
		}

		public string GetSettingRange(string settingId)
		{
			return this.GetSettingById(settingId).Range;
		}

		private HashSet<string> GetSettingsKeys()
		{
			OrderedDictionary allSettings = this.GetAllSettings();
			string[] strArrays = new string[allSettings.Keys.Count];
			allSettings.Keys.CopyTo(strArrays, 0);
			return new HashSet<string>(strArrays);
		}

		public string GetSettingType(string settingId)
		{
			return this.GetSettingById(settingId).Type.ToString();
		}

		protected abstract byte[] GetUpdatedJsonData();

		public string GetValue(string id)
		{
			if (this.GetSettingById(id).IsWriteOnly)
			{
				throw new SettingsException(string.Concat("Setting [", id, "] is write only"));
			}
			Dictionary<string, string> values = this.GetValues(new List<string>(new string[] { id }));
			if (values == null || !values.ContainsKey(id))
			{
				throw new SettingsException(string.Concat("Setting ", id, " not availble from device"));
			}
			return values[id];
		}

		public abstract Dictionary<string, string> GetValues(List<string> settingIds);

		public bool IsSettingArchivable(string settingId)
		{
			return this.GetSettingById(settingId).Archive;
		}

		public bool IsSettingClonable(string settingId)
		{
			return this.GetSettingById(settingId).Clone;
		}

		public bool IsSettingReadOnly(string settingId)
		{
			return this.GetSettingById(settingId).IsReadOnly;
		}

		public bool IsSettingValid(string settingId, string value)
		{
			return this.GetSettingById(settingId).IsValid(value);
		}

		public bool IsSettingWriteOnly(string settingId)
		{
			return this.GetSettingById(settingId).IsWriteOnly;
		}

		public abstract Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs);

		protected void Refresh()
		{
			try
			{
				byte[] updatedJsonData = this.GetUpdatedJsonData();
				if (updatedJsonData.Length != 0)
				{
					OrderedDictionary orderedDictionaries = new OrderedDictionary();
					(new SettingsBuilder(orderedDictionaries)).Parse(updatedJsonData);
					this.allSettings = orderedDictionaries;
				}
			}
			catch (ConnectionException)
			{
				throw;
			}
			catch
			{
				throw new SettingsException("Malformed settings data");
			}
		}

		public abstract void SetAllSettings(Dictionary<string, Setting> settings);

		public abstract void SetSetting(string settingId, string value);

		public abstract void SetSetting(string settingId, Setting setting);

		public abstract void SetSettings(Dictionary<string, string> settingValuePairs);

		protected void UpdateInternalState(string settingId, string value)
		{
			try
			{
				Setting settingById = this.GetSettingById(settingId);
				if (settingById != null)
				{
					if (settingById.IsReadOnly)
					{
						throw new SettingsException(string.Concat("Setting [", settingId, "] is read only."));
					}
					try
					{
						if (!settingById.IsValid(value))
						{
							throw new SettingsException(string.Concat(new string[] { "Setting [", settingId, "] is not in range [", settingById.Range, "]" }));
						}
					}
					catch (FormatException)
					{
						throw new SettingsException(string.Concat(new string[] { "Error in range check for [", settingId, "] with value [", value, "]" }));
					}
					if (settingById.Value == null || !settingById.Value.Equals(value) || settingById.IsWriteOnly)
					{
						settingById.Value = value;
						OrderedDictionary allSettings = this.GetAllSettings();
						if (allSettings != null)
						{
							allSettings.Remove(settingId);
							allSettings.Add(settingId, settingById);
						}
					}
				}
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			catch (NotSupportedException notSupportedException1)
			{
				NotSupportedException notSupportedException = notSupportedException1;
				throw new SettingsException(notSupportedException.Message, notSupportedException);
			}
		}
	}
}