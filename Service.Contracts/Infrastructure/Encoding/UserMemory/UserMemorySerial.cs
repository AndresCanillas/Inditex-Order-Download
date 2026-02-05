using Newtonsoft.Json.Linq;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public class UserMemorySerial : IUserMemoryMethod, IConfigurable<UserMemorySerialConfig>
	{
		private UserMemorySerialConfig config;

		public UserMemorySerial()
		{
			config = new UserMemorySerialConfig() { WriteUserMemory = true };
		}

		public bool WriteUserMemory
		{
			get { return config.WriteUserMemory; }
		}

		public bool IsCompatible(ITagEncoding encoding)
		{
			return encoding.ContainsField(config.FieldName);
		}

		public string GetContent(ITagEncoding encoding, JObject data)
		{
			var serial = Int64.Parse(encoding[config.FieldName].Value);
			return serial.ToString($"X{config.MemorySize / 4}");
		}

		public UserMemorySerialConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(UserMemorySerialConfig config)
		{
			this.config = config;
		}
	}

	public class UserMemorySerialConfig
	{
		public bool WriteUserMemory;      // Specifies if the command to update the user memory bank should be emmited or not
		[Caption("Memory Size (bits)")]
		public int MemorySize = 32;
		public string FieldName = "SerialNumber";
	}
}
