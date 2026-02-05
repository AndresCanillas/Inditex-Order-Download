using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Authentication
{
	public interface IAuthenticationClient
	{
		string Url { get; set; }
		string Token { get; }
		AuthenticationResult Authenticate(string userName, string password);
		Task<AuthenticationResult> AuthenticateAsync(string userName, string password);
		AuthenticationResult ValidateToken(string token);
		Task<AuthenticationResult> ValidateTokenAsync(string token);
	}


	public class AuthenticationResult
	{
		public bool Success { get; set; }
		public bool MustChangePassword { get; set; }
		public bool UserLockedOut { get; set; }
		public UserData User { get; set; }
	}


	public class UserData
	{
		public string Id { get; set; }
		public string UserName { get; set; }
		public int? CompanyID { get; set; }
		public int? LocationID { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string PhoneNumber { get; set; }
		public string Language { get; set; }
		public bool VisibleUser { get; set; }
		public string BearerToken { get; set; }
		public List<string> Roles { get; set; }
	}


	public class AuthenticationClient : BaseServiceClient, IAuthenticationClient
	{
		public AuthenticationClient(IAppConfig cfg)
		{
			Url = cfg.GetValue<string>("Services.Authentication");
		}

		public AuthenticationResult Authenticate(string userName, string password)
		{
			byte[] authData = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", userName, password));
			string authParam = Convert.ToBase64String(authData);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authParam);
			using (HttpResponseMessage response = client.GetAsync(Url + "api/authenticate").Result)
			{
				if (response.IsSuccessStatusCode)
				{
					string message = response.Content.ReadAsStringAsync().Result;
					var result = JsonConvert.DeserializeObject<AuthenticationResult>(message);
					if (result.Success)
						Token = result.User.BearerToken;
					else
						throw new Exception("Invalid user name or password");
					return result;
				}
				else
				{
					throw new Exception("Operation can't be performed. Reason: " + response.ReasonPhrase);
				}
			}
		}

		public async Task<AuthenticationResult> AuthenticateAsync(string userName, string password)
		{
			byte[] authData = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", userName, password));
			string authParam = Convert.ToBase64String(authData);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authParam);
			using (HttpResponseMessage response = await client.GetAsync(Url + "api/authenticate"))
			{
				if (response.IsSuccessStatusCode)
				{
					string message = response.Content.ReadAsStringAsync().Result;
					var result = JsonConvert.DeserializeObject<AuthenticationResult>(message);
					if (result.Success)
						Token = result.User.BearerToken;
					else
						throw new Exception("Invalid user name or password");
					return result;
				}
				else
				{
					throw new Exception("Operation can't be performed. Reason: " + response.ReasonPhrase);
				}
			}
		}


		public AuthenticationResult ValidateToken(string token)
		{
			throw new NotImplementedException();
		}

		public Task<AuthenticationResult> ValidateTokenAsync(string token)
		{
			throw new NotImplementedException();
		}
	}
}
