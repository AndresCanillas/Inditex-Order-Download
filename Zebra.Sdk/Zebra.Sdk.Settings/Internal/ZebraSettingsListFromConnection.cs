using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Device;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Settings.Internal
{
	internal class ZebraSettingsListFromConnection : ZebraSettingsList
	{
		private Connection connection;

		public ZebraSettingsListFromConnection(Connection connection)
		{
			this.connection = connection;
		}

		private Dictionary<string, string> DoJsonQuery(List<string> settingsToRetrieve)
		{
			byte[] numArray = JsonHelper.BuildQuery(settingsToRetrieve);
			Connection connection = ConnectionUtil.SelectConnection(this.connection);
			return JsonHelper.ParseGetResponse(connection.SendAndWaitForValidResponse(numArray, connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, new JsonValidator()));
		}

		protected override byte[] GetUpdatedJsonData()
		{
			Connection connection = ConnectionUtil.SelectConnection(this.connection);
			return connection.SendAndWaitForValidResponse(Encoding.UTF8.GetBytes("{}{\"allconfig\":null}"), connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, new JsonValidator());
		}

		public override Dictionary<string, string> GetValues(List<string> settingIds)
		{
			return this.DoJsonQuery(base.FilterOutUnreadableSettings(settingIds));
		}

		public override Dictionary<string, string> ProcessSettingsViaMap(Dictionary<string, string> settingValuePairs)
		{
			Dictionary<string, string> strs = null;
			byte[] numArray = this.UpdateSettingsWithResponse(settingValuePairs);
			try
			{
				strs = JsonHelper.ParseGetResponse(numArray);
			}
			catch (ZebraIllegalArgumentException zebraIllegalArgumentException)
			{
				throw new SettingsException(zebraIllegalArgumentException.Message);
			}
			return strs;
		}

		public override void SetAllSettings(Dictionary<string, Setting> settings)
		{
		}

		public void SetConnection(Connection newConnection)
		{
			this.connection = newConnection;
		}

		public override void SetSetting(string settingId, string value)
		{
			base.UpdateInternalState(settingId, value);
			this.StoreSettingValues(new Dictionary<string, string>()
			{
				{ settingId, value }
			});
		}

		public override void SetSetting(string settingId, Setting setting)
		{
		}

		public override void SetSettings(Dictionary<string, string> settingValuePairs)
		{
			this.UpdateSettingsWithResponse(settingValuePairs);
		}

		private byte[] StoreSettingValues(Dictionary<string, string> settingValuePairs)
		{
			byte[] numArray;
			try
			{
				byte[] numArray1 = JsonHelper.BuildSetCommand(settingValuePairs);
				Connection connection = ConnectionUtil.SelectConnection(this.connection);
				numArray = connection.SendAndWaitForValidResponse(numArray1, connection.MaxTimeoutForRead, connection.TimeToWaitForMoreData, new JsonValidator());
			}
			catch (ArgumentException argumentException1)
			{
				ArgumentException argumentException = argumentException1;
				throw new SettingsException(argumentException.Message, argumentException);
			}
			catch (ConnectionException connectionException1)
			{
				ConnectionException connectionException = connectionException1;
				throw new SettingsException(connectionException.Message, connectionException);
			}
			return numArray;
		}

		private byte[] UpdateSettingsWithResponse(Dictionary<string, string> settingValuePairs)
		{
			base.CheckSettingsList(settingValuePairs);
			foreach (string key in settingValuePairs.Keys)
			{
				try
				{
					if (settingValuePairs[key] != null)
					{
						base.UpdateInternalState(key, settingValuePairs[key]);
					}
				}
				catch (SettingsException)
				{
				}
			}
			return this.StoreSettingValues(settingValuePairs);
		}
	}
}