using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Settings.Internal
{
	internal interface ZebraSettingsListI
	{
		HashSet<string> GetAllSettingIds();

		OrderedDictionary GetAllSettings();

		Dictionary<string, string> GetAllSettingValues();

		Setting GetSetting(string settingId);

		string GetSettingRange(string settingId);

		string GetSettingType(string settingId);

		string GetValue(string id);

		Dictionary<string, string> GetValues(List<string> settingIds);

		bool IsSettingArchivable(string id);

		bool IsSettingClonable(string id);

		bool IsSettingReadOnly(string settingId);

		bool IsSettingValid(string settingId, string value);

		bool IsSettingWriteOnly(string settingId);

		Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs);

		void SetAllSettings(Dictionary<string, Setting> settings);

		void SetSetting(string settingId, string value);

		void SetSetting(string settingId, Setting setting);

		void SetSettings(Dictionary<string, string> settingValuePairs);
	}
}