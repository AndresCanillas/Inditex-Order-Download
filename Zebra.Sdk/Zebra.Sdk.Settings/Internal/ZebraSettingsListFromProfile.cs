using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Zebra.Sdk.Settings;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings.Internal
{
	internal class ZebraSettingsListFromProfile : ZebraSettingsList
	{
		private string pathToProfile;

		public ZebraSettingsListFromProfile(string pathToProfile)
		{
			this.pathToProfile = pathToProfile;
		}

		protected override byte[] GetUpdatedJsonData()
		{
			return (new ZipUtil(this.pathToProfile)).ExtractEntry("settings.json");
		}

		public override Dictionary<string, string> GetValues(List<string> settingIds)
		{
			Dictionary<string, string> strs;
			try
			{
				List<string> strs1 = base.FilterOutUnreadableSettings(settingIds);
				Dictionary<string, string> strs2 = new Dictionary<string, string>();
				foreach (string str in strs1)
				{
					strs2.Add(str, base.GetSettingById(str).Value);
				}
				strs = strs2;
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			return strs;
		}

		public override Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs)
		{
			Dictionary<string, string> values;
			try
			{
				this.SetSettings(settingValuePairs);
				List<string> strs = new List<string>();
				strs.AddRange(settingValuePairs.Keys);
				values = this.GetValues(strs);
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			return values;
		}

		public override void SetAllSettings(Dictionary<string, Setting> settings)
		{
			try
			{
				OrderedDictionary allSettings = base.GetAllSettings();
				foreach (string key in settings.Keys)
				{
					allSettings.Remove(key);
					allSettings.Add(key, settings[key]);
				}
				this.StoreSettingValues();
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

		public override void SetSetting(string settingId, string value)
		{
			base.UpdateInternalState(settingId, value);
			this.StoreSettingValues();
		}

		public override void SetSetting(string settingId, Setting setting)
		{
			try
			{
				OrderedDictionary allSettings = base.GetAllSettings();
				allSettings.Remove(settingId);
				allSettings.Add(settingId, setting);
				this.StoreSettingValues();
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

		public override void SetSettings(Dictionary<string, string> settingValuePairs)
		{
			foreach (string key in settingValuePairs.Keys)
			{
				try
				{
					base.UpdateInternalState(key, settingValuePairs[key]);
				}
				catch (SettingsException)
				{
				}
			}
			this.StoreSettingValues();
		}

		private void StoreSettingValues()
		{
			SettingsBuilder settingsBuilder = new SettingsBuilder(base.GetAllSettings());
			try
			{
				string allconfigJson = settingsBuilder.ToAllconfigJson();
				(new ZipUtil(this.pathToProfile)).AddEntry("settings.json", Encoding.UTF8.GetBytes(allconfigJson));
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			catch (IOException oException1)
			{
				IOException oException = oException1;
				throw new SettingsException(oException.Message, oException);
			}
		}
	}
}