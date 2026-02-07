using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;

namespace WebLink.Contracts.Models
{
	public interface IAuthenticationCookieProtector
	{
		string Protect(string userId);
		string Unprotect(string data);
	}

	public class AuthenticationCookieProtector : IAuthenticationCookieProtector
	{
		private readonly IDataProtector dp;
		private readonly Random rnd;

		public AuthenticationCookieProtector(IDataProtectionProvider dataProtectionProvider)
		{
			dp = dataProtectionProvider.CreateProtector("PrintCentral.AuthenticationCookieProtector");
			rnd = new Random();
		}

		public string Protect(string userId)
		{
			if (userId == null)
				throw new Exception($"Argument {nameof(userId)} cannot be null");

			var buffer = new byte[rnd.Next(50, 80)];
			rnd.NextBytes(buffer);

			var info = new SessionIdentityInfo()
			{
				SessionToken = Convert.ToBase64String(buffer),
				UserId = userId
			};

			var json = JsonConvert.SerializeObject(info);
			var encoded = Encoding.UTF8.GetBytes(json);
			var encrypted = dp.Protect(encoded);
			return Convert.ToBase64String(encrypted);
		}


		public string Unprotect(string data)
		{
			var payload = Convert.FromBase64String(data);
			var decrypted = dp.Unprotect(payload);
			var decoded = Encoding.UTF8.GetString(decrypted);
			var identity = JsonConvert.DeserializeObject<SessionIdentityInfo>(decoded);
			if (String.IsNullOrWhiteSpace(identity.UserId))
				throw new Exception("UserId not set");
			return identity.UserId;
		}
	}


	public class SessionIdentityInfo
	{
		public string SessionToken;
		public string UserId;
	}
}
