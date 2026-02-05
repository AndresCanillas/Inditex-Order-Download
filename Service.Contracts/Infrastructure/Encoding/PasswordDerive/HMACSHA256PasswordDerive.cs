using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Service.Contracts
{
	public class HMACSHA256PasswordDerive: IPasswordDeriveMethod, IConfigurable<PasswordDeriveMethodConfig>
	{
		private PasswordDeriveMethodConfig config = new PasswordDeriveMethodConfig() { WritePassword = true, Seed = "0" };

		public int ID { get { return 2; } }
		public string Name { get { return "HMACSHA256"; } }
		public string Seed { get; set; }
		public InputFormat SeedFormat { get; set; }
		public InputFormat EPCFormat { get; set; }

		public string DerivePassword(ITagEncoding encoding)
		{
			byte[] epc;
			byte[] key;

			if (SeedFormat == InputFormat.Hexadecimal)
				key = xtConvert.HexStringToByteArray(Seed);
			else
				key = Encoding.ASCII.GetBytes(Seed);

			if (EPCFormat == InputFormat.Hexadecimal)
				epc = xtConvert.HexStringToByteArray(encoding.GetHexadecimal());
			else
				epc = Encoding.ASCII.GetBytes(encoding.GetHexadecimal());

			using (var hash = new HMACSHA256(key))
			{
				byte[] step1 = hash.ComputeHash(epc);
				return xtConvert.ByteArrayToHexString(step1, step1.Length - 4, 4);
			}
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
			SeedFormat = config.SeedFormat;
			EPCFormat = config.EPCFormat;
		}
	}
}
