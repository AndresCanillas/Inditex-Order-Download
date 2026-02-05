using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;


namespace Middleware
{
	public interface ITokenService
	{
		string RegisterPrincipal(ClaimsPrincipal principal);
		bool ValidateToken(string token, out ClaimsPrincipal principal);
		void RemoveToken(string token);
	}


	class TokenService: ITokenService
	{
		private ConcurrentDictionary<string, TokenInfo> tokens;
		private RNGCryptoServiceProvider rnd;
		private Timer timer;

		public TokenService()
		{
			tokens = new ConcurrentDictionary<string, TokenInfo>();
			rnd = new RNGCryptoServiceProvider();
			timer = new Timer(expireTokens, null, (int)TimeSpan.FromMinutes(1).TotalMilliseconds, Timeout.Infinite);
		}


		private void expireTokens(object state)
		{
			try
			{
				TokenInfo principal;
				List<string> expiredTokens = new List<string>();
				foreach (string key in tokens.Keys)
				{
					if (tokens.TryGetValue(key, out principal))
					{
						if (principal.Date.Add(TimeSpan.FromHours(12)) <= DateTime.Now)
							expiredTokens.Add(key);
					}
				}
				foreach (string key in expiredTokens)
					tokens.TryRemove(key, out principal);
			}
			catch { }
			finally
			{
				timer.Change((int)TimeSpan.FromMinutes(1).TotalMilliseconds, Timeout.Infinite);
			}
		}


		public string RegisterPrincipal(ClaimsPrincipal principal)
		{
			var tokenInfo = new TokenInfo(principal);
			string token;
			byte[] tokenBytes = new byte[64];
			do
			{
				rnd.GetBytes(tokenBytes);
				token = Convert.ToBase64String(tokenBytes);
			} while (!tokens.TryAdd(token, tokenInfo));
			return token;
		}


		public bool ValidateToken(string token, out ClaimsPrincipal principal)
		{
			TokenInfo info;
			principal = null;
			if (String.IsNullOrWhiteSpace(token))
			{
				return false;
			}
			if (tokens.TryGetValue(token, out info))
			{
				if (info.Date.Add(TimeSpan.FromHours(3)) > DateTime.Now)
				{
					info.Renew(); // Renew session token to give it another 15 minutes of life time
					principal = info.Principal;
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		public void RemoveToken(string token)
		{
			tokens.TryRemove(token, out _);
		}
	}


	public class TokenInfo
	{
		private DateTime date = DateTime.Now;
		private ClaimsPrincipal principal;

		public DateTime Date { get { return date; } }
		public ClaimsPrincipal Principal { get { return principal; } }

		public TokenInfo(ClaimsPrincipal principal)
		{
			this.principal = principal;
		}

		public void Renew()
		{
			date = DateTime.Now;
		}
	}
}
