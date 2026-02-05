using System;
using System.Collections.Generic;

namespace Service.Contracts
{
    public class ConfigMeta
	{
		public List<ConfigField> Fields = new List<ConfigField>();
	}

	public class ConfigField
	{
		public string Name;
		public string Caption;
		public string Description;
		public string Type;
        public bool Nullable;
		public Constraints Constraints;
		public List<ConfigField> SubFields;
	}

	public class Constraints
	{
		public List<Constraint> Items = new List<Constraint>();
	}


	public class Constraint
	{
		public string Type;
		public object Data;

        public Constraint() { }

		public Constraint(string type, FixedOptions data)
		{
			Type = type;
			Data = ParseFixedOptions(data.Options);
		}

		private List<FixedOptionMeta> ParseFixedOptions(string options)
		{
			List<FixedOptionMeta> result = new List<FixedOptionMeta>();
			string[] tokens = options.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string t in tokens)
			{
				string[] subtokens = t.Split('=');
				if (subtokens.Length != 2) continue;
				result.Add(new FixedOptionMeta()
				{
					Value = subtokens[0].Trim(),
					Text = subtokens[1].Trim()
				});
			}
			return result;
		}

		public Constraint(string type, string data = null)
		{
			Type = type;
			Data = data;
		}
	}

	public class FixedOptionMeta
	{
		public string Value;
		public string Text;
	}

	public class ComponentMeta
	{
		public string Contract;
		public List<ComponentConfigMeta> Implementations = new List<ComponentConfigMeta>();
	}

	public class ComponentConfigMeta
	{
		public string Contract;
		public string Implementation;
		public string DisplayName;
		public string Description;
		public List<ConfigField> Config = new List<ConfigField>();
	}
}
