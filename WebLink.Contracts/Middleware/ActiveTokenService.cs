using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Middleware
{
	public interface IActiveTokenService
	{
		Task<AuthTokenResult> Authenticate(string credentials);
		bool ValidateToken(string token, out ClaimsPrincipal principal);
	}

	public class ActiveTokenService : IActiveTokenService
	{
		private ConcurrentDictionary<string, TokenInfo> tokens;
		private RNGCryptoServiceProvider rnd;
		private Timer timer;
		private IFactory factory;
		private IUserManager userManager;
		private ISignInManager signInManager;

		public ActiveTokenService(IFactory factory, IUserManager userManager, ISignInManager signInManager)
		{
			tokens = new ConcurrentDictionary<string, TokenInfo>();
			rnd = new RNGCryptoServiceProvider();
			timer = new Timer(expireTokens, null, (int)TimeSpan.FromMinutes(1).TotalMilliseconds, Timeout.Infinite);
			this.factory = factory;
			this.userManager = userManager;
			this.signInManager = signInManager;
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
						if (principal.Date.Add(TimeSpan.FromMinutes(15)) <= DateTime.Now)
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


		public async Task<AuthTokenResult> Authenticate(string credentials)
		{
			var result = new AuthTokenResult();
			List<string> parameters = ExtractParameters(credentials);
			if (parameters == null)
				return result;

			string userName = parameters[0];
			string password = parameters[1];
			string newPassword = null;
			if (parameters.Count > 2)
				newPassword = parameters[2];

			AppUser user = await userManager.FindByNameAsync(userName);
			if (user != null)
			{
				if ((await signInManager.SignInAsync(user, password)).Success)
				{
					var roles = await userManager.GetRolesAsync(user);
					var identity = new UserIdentity(user, roles);
					var principal = new UserPrincipal(identity);
					var tokenInfo = new TokenInfo(principal);
					result.Success = true;
					result.Principal = principal;
					string token;
					byte[] tokenBytes = new byte[64];
					do
					{
						rnd.GetBytes(tokenBytes);
						token = Convert.ToBase64String(tokenBytes);
					} while (!tokens.TryAdd(token, tokenInfo));
					result.Token = token;
				}
			}
			return result;
		}


		private List<string> ExtractParameters(string authorizationParameter)
		{
			byte[] credentialBytes;
			try
			{
				credentialBytes = Convert.FromBase64String(authorizationParameter);
			}
			catch (FormatException)
			{
				return null;
			}

			Encoding encoding = Encoding.ASCII;
			encoding = (Encoding)encoding.Clone();
			encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
			string decodedCredentials;
			try
			{
				decodedCredentials = encoding.GetString(credentialBytes);
			}
			catch (DecoderFallbackException)
			{
				return null;
			}

			if (String.IsNullOrWhiteSpace(decodedCredentials))
				return null;

			string[] tokens = decodedCredentials.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length <= 1)
				return null;

			return new List<string>(tokens);
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
				if (info.Date.Add(TimeSpan.FromMinutes(30)) > DateTime.Now)
				{
					info.Renew(); // Renew session token to give it another N minutes of life time
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
	}



	public class AuthTokenResult
	{
		public bool Success { get; set; }
		public bool MustChangePassword { get; set; }
		public bool UserLockedOut { get; set; }
		public string Token { get; set; }
		public ClaimsPrincipal Principal { get; set; }
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
