using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public interface IFieldDataSource
	{
		T GetValue<T>(JObject variableData);
	}

	public class FixedDataSource : IFieldDataSource, IConfigurable<FixedDataSourceConfig>
	{
		private FixedDataSourceConfig config;

		public FixedDataSource() { }

		public FixedDataSource(string fixedValue)
		{
			config = new FixedDataSourceConfig()
			{
				FixedValue = fixedValue
			};
		}

		public FixedDataSourceConfig Config { get => config; }

		public T GetValue<T>(JObject data)
		{
			var v = Convert.ChangeType(config.FixedValue, typeof(T));
			return (T)v;
		}

		public FixedDataSourceConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(FixedDataSourceConfig config)
		{
			this.config = config;
		}
	}

	public class FixedDataSourceConfig
	{
		public string FixedValue { get; set; }
	}

	public class VariableDataSource : IFieldDataSource, IConfigurable<VariableDataSourceConfig>
	{
		private VariableDataSourceConfig config;

		public VariableDataSource() { }

		public VariableDataSource(string fieldName)
		{
			config = new VariableDataSourceConfig()
			{
				FieldName = fieldName
			};
		}

		public VariableDataSourceConfig Config { get => config; }

		public T GetValue<T>(JObject data)
		{
			JToken jvalue = data;
			string[] tokens = config.FieldName.Split(new char[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in tokens)
			{
				if (jvalue.HasValues)
				{
					jvalue = jvalue[t];
					if (jvalue == null || jvalue.Type == JTokenType.Null)
						throw new Exception($"Could not find the specified field: {config.FieldName}");
				}
				else throw new Exception($"The specified field is null or empty: {config.FieldName}");
			}
			if (jvalue.Type == JTokenType.Null)
				throw new Exception($"The specified field is null or empty: {config.FieldName}");
			else
				return jvalue.Value<T>();
		}

		public VariableDataSourceConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(VariableDataSourceConfig config)
		{
			this.config = config;
		}
	}

	public class VariableDataSourceConfig
	{
		public string FieldName { get; set; }
	}
}
