using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Service.Contracts
{
	public class ConfigJSONConverter : JsonConverter
	{
		private IFactory factory;
		private JsonSerializerSettings settings;

		public ConfigJSONConverter(IFactory factory)
		{
			this.factory = factory;
            settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>(new[] { this })
            };
		}

		public JsonSerializerSettings Settings { get { return settings; } }

		public override bool CanConvert(Type objectType)
		{
            return (objectType.IsInterface && ConfigurationContext.Components.Contains(objectType)) || objectType.Implements(typeof(IConfigurable<>));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if(reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException("Expected StartObject token");

            JObject obj = JObject.Load(reader); // Read the full object

            // Extract the _impl value
            JToken implToken = obj["_impl"];
            if(implToken == null || implToken.Type == JTokenType.Null)
                return null;

            if(implToken.Type != JTokenType.String)
                throw new JsonSerializationException($"_impl property was expected to be of type 'string'");

            string implName = implToken.ToString();

            // Resolve the concrete implementation type
            var implType = ConfigurationContext.Components.GetImplementations(objectType)
                             .FirstOrDefault(p => p.Name == implName);

            if(implType == null)
                throw new JsonSerializationException($"Implementation type '{implName}' not found for {objectType.Name}");

            object instance = factory.GetInstance(implType);

            if(implType.Implements(typeof(IConfigurable<>)))
            {
                var configType = implType.GetConfigurationType();
                var configInstance = Activator.CreateInstance(configType);

                JObject data = obj["_data"] as JObject;
                if(data != null)
                {
                    // Populate config instance from _data
                    PropertyInfo[] props = configType.GetProperties();
                    foreach(var p in props)
                    {
                        if(p.CanRead && p.CanWrite)
                        {
                            JToken token = data.SelectToken(p.Name);
                            if(token != null && token.Type != JTokenType.Null)
                            {
                                object value = token.ToObject(p.PropertyType, serializer);
                                p.SetValue(configInstance, value);
                            }
                        }
                    }

                    FieldInfo[] fields = configType.GetFields();
                    foreach(var f in fields)
                    {
                        JToken token = data.SelectToken(f.Name);
                        if(token != null && token.Type != JTokenType.Null)
                        {
                            object value = token.ToObject(f.FieldType, serializer);
                            f.SetValue(configInstance, value);
                        }
                    }

                    // Apply the configuration
                    MethodInfo configMethod = implType.GetMethod("SetConfiguration");
                    configMethod.Invoke(instance, new object[] { configInstance });
                }
            }

            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Type implType = value.GetType();
			if (implType.Implements(typeof(IConfigurable<>)))
			{
				var configMethod = implType.GetMethod("GetConfiguration");
				object config = configMethod.Invoke(value, null);
				var jo = new JObject();
				jo.Add(new JProperty("_impl", implType.Name));
                jo.Add(new JProperty("_data", JObject.FromObject(config, serializer)));
				jo.WriteTo(writer, this);
			}
		}
	}
}
