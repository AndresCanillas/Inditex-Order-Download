using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
	public static class JObjectExtensions
	{
		public static T GetValue<T>(this JObject root, string key)
		{
			JToken jvalue = root;
			string[] tokens = key.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in tokens)
			{
                if (jvalue.HasValues)
                {
                    jvalue = jvalue[t];
                    if (jvalue == null || jvalue.Type == JTokenType.Null)
                        return default(T); // throw new Exception($"Could not find the specified key: {key}");
                }
                else return default(T);
            }
			if(jvalue.Type == JTokenType.Null)
				return default(T);
			else
				return jvalue.Value<T>();
		}


		public static T GetValue<T>(this JObject root, string key, T defaultValue)
		{
			if (root == null)
				return defaultValue;
			JToken jvalue = root;
			string[] tokens = key.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in tokens)
			{
				jvalue = jvalue[t];
				if (jvalue == null) return defaultValue;
			}
			return jvalue.Value<T>();
		}


        public static void SetValue<T>(this JObject root, string key, T value)
        {
            JToken jvalue = root;
            string[] tokens = key.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
            JObject currentJvalue = new JObject();
            var property = string.Empty;
            foreach (string t in tokens)
            {
                jvalue = jvalue[t];
                property = t;
                if (jvalue != null && jvalue.Type == JTokenType.Object)
                    currentJvalue = (JObject)jvalue;
                if (jvalue == null)
                {
                    currentJvalue.Add(t, new JValue(value));
                    jvalue = currentJvalue.GetProperty(t);
                }
            }
            if (jvalue.Type == JTokenType.Null)
                return;
            else
                jvalue = JToken.FromObject(value);
        }


        public static string GetProperty(this JObject root, string key)
		{
			JToken jvalue = root;
			string[] tokens = key.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in tokens)
			{
				jvalue = jvalue[t];
				if (jvalue == null) return null;
			}
			return jvalue.ToString();
		}

		public static T Bind<T>(this JObject root, string key)
		{
			var json = root.GetProperty(key);
			if (json != null)
				return JsonConvert.DeserializeObject<T>(json);
			else
				return default(T);
		}

		public static JObject Bind(this JObject root, string key)
		{
			JToken jvalue = root;
			string[] tokens = key.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in tokens)
			{
				jvalue = jvalue[t];
				if (jvalue == null) return null;
			}
			return jvalue as JObject;
		}
	}


	public static class JArrayExtensions
	{
		public static JArray Append(this JArray array, JArray other)
		{
			foreach (var row in other)
				array.Add(row);
			return array;
		}
	}
}
