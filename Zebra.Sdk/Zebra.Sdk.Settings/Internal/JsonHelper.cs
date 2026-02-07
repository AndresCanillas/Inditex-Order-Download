using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Zebra.Sdk.Device;
using Zebra.Sdk.Util.Internal;

namespace Zebra.Sdk.Settings.Internal
{
	internal class JsonHelper
	{
		public JsonHelper()
		{
		}

		public static byte[] BuildQuery(List<string> ids)
		{
			StringBuilder stringBuilder = new StringBuilder("{}{");
			foreach (string id in ids)
			{
				stringBuilder.Append("\"");
				stringBuilder.Append(id);
				stringBuilder.Append("\":null,");
			}
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
			stringBuilder.Append("}");
			return Encoding.UTF8.GetBytes(stringBuilder.ToString());
		}

		public static byte[] BuildSetCommand(Dictionary<string, string> settingValuePairs)
		{
			StringBuilder stringBuilder = new StringBuilder("{}");
			stringBuilder.Append(JsonConvert.SerializeObject(settingValuePairs));
			return Encoding.UTF8.GetBytes(stringBuilder.ToString());
		}

		public static bool IsValidJson(byte[] response)
		{
			bool hasValues = false;
			try
			{
				hasValues = JToken.Parse(Encoding.UTF8.GetString(response, 0, (int)response.Length)).HasValues;
			}
			catch
			{
			}
			return hasValues;
		}

		public static bool IsValidJson(string response)
		{
			bool flag = false;
			try
			{
				if (response.StartsWith("{}"))
				{
					response = response.Substring(2);
				}
				JToken.Parse(response);
				flag = true;
			}
			catch
			{
			}
			return flag;
		}

		public static Dictionary<string, string> ParseGetResponse(byte[] response)
		{
			Dictionary<string, string> map = null;
			try
			{
				map = StringUtilities.ConvertKeyValueJsonToMap(response);
			}
			catch (Exception)
			{
				throw new ZebraIllegalArgumentException(string.Concat("Error processing response from device: [", Encoding.UTF8.GetString(response, 0, (int)response.Length), "]"));
			}
			return map;
		}
	}
}