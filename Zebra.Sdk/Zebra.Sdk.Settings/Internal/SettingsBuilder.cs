using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Zebra.Sdk.Device;
using Zebra.Sdk.Settings;

namespace Zebra.Sdk.Settings.Internal
{
	internal class SettingsBuilder
	{
		private OrderedDictionary mySettingsMap;

		public SettingsBuilder(OrderedDictionary allSettings)
		{
			this.mySettingsMap = allSettings;
		}

		private string EscapeControlCharacters(string s)
		{
			if (s != null)
			{
				s = s.Replace("\\b", "\\\\b");
				s = s.Replace("\\f", "\\\\f");
				s = s.Replace("\\n", "\\\\n");
				s = s.Replace("\\r", "\\\\r");
				s = s.Replace("\\t", "\\\\t");
			}
			return s;
		}

		protected virtual Encoding GetDefaultCharset()
		{
			return Encoding.GetEncoding(0);
		}

		public void Parse(byte[] settingsJsonData)
		{
			try
			{
				this.ParseJsonToMap(this.GetDefaultCharset().GetString(settingsJsonData));
			}
			catch (Exception)
			{
				try
				{
					this.ParseJsonToMap(Encoding.UTF8.GetString(settingsJsonData));
				}
				catch (Exception exception)
				{
					throw new ZebraIllegalArgumentException(exception.Message);
				}
			}
		}

		private void ParseJsonToMap(string jsonData)
		{
			jsonData = this.EscapeControlCharacters(jsonData);
			foreach (KeyValuePair<string, Setting> item in JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Setting>>>(jsonData)["allconfig"])
			{
				this.mySettingsMap.Add(item.Key, item.Value);
			}
		}

		private string RemoveExtraEscaping(string s)
		{
			if (s != null)
			{
				s = s.Replace("\\\\b", "\\b");
				s = s.Replace("\\\\f", "\\f");
				s = s.Replace("\\\\n", "\\n");
				s = s.Replace("\\\\r", "\\r");
				s = s.Replace("\\\\t", "\\t");
			}
			return s;
		}

		public string ToAllconfigJson()
		{
			string str = JsonConvert.SerializeObject(this.mySettingsMap, Formatting.Indented);
			return string.Concat("{ \"allconfig\":\n", this.RemoveExtraEscaping(str), "}");
		}
	}
}