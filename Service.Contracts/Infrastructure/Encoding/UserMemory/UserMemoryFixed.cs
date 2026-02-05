using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
	public class UserMemoryFixed : IUserMemoryMethod, IConfigurable<FixedUserMemoryConfig>
	{
		private FixedUserMemoryConfig config;

		public UserMemoryFixed()
		{
			config = new FixedUserMemoryConfig() { WriteUserMemory = false };
		}

		public bool WriteUserMemory
		{
			get { return config.WriteUserMemory; }
		}

		public bool IsCompatible(ITagEncoding encoding)
		{
			return true;
		}

		public string GetContent(ITagEncoding encoding, JObject data)
		{
			switch (config.Type)
			{
				case FixedUserMemoryType.Hexadecimal:
					return config.Value.PadLeft(config.MemorySize / 4, '0');
				case FixedUserMemoryType.Decimal:
					return Int32.Parse(config.Value).ToString($"X8{config.MemorySize / 4}");
				default:
					return "00000000".PadLeft(config.MemorySize / 4, '0');
			}
		}

		public FixedUserMemoryConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(FixedUserMemoryConfig config)
		{
			this.config = config;
		}
	}

	public class FixedUserMemoryConfig
	{
		public bool WriteUserMemory;      // Specifies if the command to update the user memory bank should be emmited or not
		[Caption("Memory Size (bits)")]
		public int MemorySize = 32;
		public FixedUserMemoryType Type = FixedUserMemoryType.Hexadecimal;
		public string Value = "00000000";
	}

	public enum FixedUserMemoryType
	{
		Hexadecimal,
		Decimal
	}
}
