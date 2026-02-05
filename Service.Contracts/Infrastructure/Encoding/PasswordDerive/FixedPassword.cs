using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	public class FixedPassword: IPasswordDeriveMethod, IConfigurable<PasswordDeriveMethodConfig>
	{
		private PasswordDeriveMethodConfig config = new PasswordDeriveMethodConfig() { WritePassword = true, Seed = "00000000" };

		public int ID { get { return 0; } }
		public string Name { get { return "Fixed Password";  } }
		public string Seed { get; set; }

		public string DerivePassword(ITagEncoding encoding)
		{
			if (Seed.Length > 8)
				Seed = Seed.Substring(0, 8);
			if (Seed.Length < 8)
				return new String('0', 8 - Seed.Length) + Seed;
			else
				return Seed;
		}

		public bool WritePassword
		{
			get { return config.WritePassword; }
		}

		public PasswordDeriveMethodConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(PasswordDeriveMethodConfig config)
		{
			this.config = config;
			Seed = config.Seed;
		}
	}
}
