using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Service.Contracts
{
	public class InditexMD5PasswordDerive: IPasswordDeriveMethod, IConfigurable<PasswordDeriveMethodConfig>
	{
		private PasswordDeriveMethodConfig config = new PasswordDeriveMethodConfig() { WritePassword=true, Seed = "0" };

		public int ID { get { return 1; } }
		public string Name { get { return "MD5"; } }
		public string Seed { get; set; }

		public string DerivePassword(ITagEncoding encoding)
		{
			if (String.IsNullOrWhiteSpace(Seed))
                return "00000000";
			int seed = Int32.Parse(Seed);
			var field = encoding["SerialNumber"];
			if (field == null || field.BitLength > 64)
				throw new Exception("MD5Password requires a tag encoding that includes a serial number with a maximum of 64 bits.");
			using (var hash = MD5.Create())
		    {
				var serial = (int)(Int64.Parse(encoding["SerialNumber"].Value));  // NOTE: Discard the most significative bits of the serial if it is longer than 32 bits...
				var step1 = xtConvert.Int32ToByteArray(seed ^ serial);
				Array.Reverse(step1);
				var step2 = hash.ComputeHash(step1);
				return xtConvert.ByteArrayToHexString(step2, 0, 4);
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
		}
	}
}
