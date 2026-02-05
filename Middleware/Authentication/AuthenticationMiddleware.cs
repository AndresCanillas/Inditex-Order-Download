//using System;
//using System.Collections.Generic;
//using System.Security.Principal;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.DependencyInjection;
//using Service.Contracts;
//using Service.Contracts.Authentication;

//namespace Middleware
//{
//	public static class AuthenticationMiddleware
//	{
//		private static IFactory factory;
//		private static ITokenService tokenService;


//		public static void UseAuthenticationService(this IApplicationBuilder app)
//		{
//			factory = app.ApplicationServices.GetRequiredService<IFactory>();
//			tokenService = factory.GetInstance<ITokenService>();
//			app.Use(async (context, next) =>
//			{
//				if (String.Compare(context.Request.Path, "/auth", true) == 0)
//				{
//					if (await InitializeIdentityFromAuthorizationHeader(context))
//						context.Response.StatusCode = 200;
//					else
//						context.Response.StatusCode = 403;
//				}
//				else if (InitializeIdentityFromCookie(context))
//					await next.Invoke();
//				else if (await InitializeIdentityFromAuthorizationHeader(context))
//					await next.Invoke();
//				else if (String.Compare(context.Request.Path, "/login", true) == 0)
//					await next.Invoke();
//				else
//					context.Response.Redirect($"/login");
//			});
//		}


//		private static bool InitializeIdentityFromCookie(HttpContext context)
//		{
//			if (context.Request.Cookies.TryGetValue("bearerToken", out var token))
//			{
//				if (tokenService.ValidateToken(token, out var principal))
//				{
//					context.User = principal;
//					return true;
//				}
//			}
//			return false;
//		}


//		private static async Task<bool> InitializeIdentityFromAuthorizationHeader(HttpContext context)
//		{
//			var authHeader = context.Request.Headers["Authorization"];
//			if (authHeader.Count < 1)
//				return false;
//			var authHeaderValue = context.Request.Headers["Authorization"][0];
//			if (authHeaderValue.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
//			{
//				return InitializeIdentityFromToken(context, authHeaderValue.Substring(7));
//			}
//			else if (authHeaderValue.StartsWith("basic", StringComparison.OrdinalIgnoreCase))
//			{
//				return await InitializeIdentityFromCredentials(context, authHeaderValue.Substring(6));
//			}
//			return false;
//		}


//		private static bool InitializeIdentityFromToken(HttpContext context, string token)
//		{
//			if (tokenService.ValidateToken(token, out var principal))
//			{
//				context.User = principal;
//				return true;
//			}
//			else
//			{
//				context.Response.Headers.Add("Reason-Phrase", "Invalid bearer Token or session expired.");
//				return false;
//			}
//		}


//		private static async Task<bool> InitializeIdentityFromCredentials(HttpContext context, string credentials)
//		{
//			if (GetCredentials(credentials, out var userName, out var password))
//			{
//				//var userRepo = factory.GetInstance<IUserRepository>();
//				//AuthenticationResult result = await userRepo.AuthenticateAsync(userName, password);
//				//if (result.Success)
//				//{
//				//	context.User = result.Principal;
//				//	context.Response.Cookies.Append("bearerToken", tokenService.RegisterPrincipal(result.Principal));
//				//	var lang = result.Principal.GetClaimAsString("Language");
//				//	context.Response.Cookies.Append("language", lang, new CookieOptions() { MaxAge = TimeSpan.FromDays(8) });
//				//	return true;
//				//}
//				//else
//				//{
//				//	if (result.MustChangePassword)
//				//	{
//				//		context.Response.Headers.Add("Reason-Phrase", "Must change password");
//				//	}
//				//	else
//				//	{
//				//		context.Response.Headers.Add("Reason-Phrase", "Invalid username or password");
//				//	}
//				//}
//			}
//			return false;
//		}


//		private static bool GetCredentials(string authParameters, out string userName, out string password)
//		{
//			userName = null;
//			password = null;
//			List<string> parameters = ExtractAuthParameters(authParameters);
//			if (parameters == null || parameters.Count < 2)
//				return false;
//			userName = parameters[0];
//			password = parameters[1];
//			return true;
//		}


//		private static List<string> ExtractAuthParameters(string authParameters)
//		{
//			byte[] credentialBytes;
//			try
//			{
//				credentialBytes = Convert.FromBase64String(authParameters);
//			}
//			catch (FormatException)
//			{
//				return null;
//			}

//			Encoding encoding = Encoding.ASCII;
//			encoding = (Encoding)encoding.Clone();
//			encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
//			string decodedCredentials;
//			try
//			{
//				decodedCredentials = encoding.GetString(credentialBytes);
//			}
//			catch (DecoderFallbackException)
//			{
//				return null;
//			}

//			if (String.IsNullOrWhiteSpace(decodedCredentials))
//				return null;

//			string[] tokens = decodedCredentials.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
//			if (tokens.Length <= 1)
//				return null;

//			return new List<string>(tokens);
//		}

//		public static void AddPrintAuthentication(this IServiceCollection services)
//		{
//			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
//			services.AddTransient<IPrincipal>(provider =>
//			{
//				try
//				{
//					var accessor = provider.GetService<IHttpContextAccessor>();
//					if (accessor.HttpContext != null)
//					{
//						if (accessor.HttpContext.User.Identity.IsAuthenticated)
//							return accessor.HttpContext.User;
//						else if (accessor.HttpContext.User.Identity.Name == null)
//							return new SystemIdentity();
//						else
//							return null;
//					}
//					else return new SystemIdentity();
//				}
//				catch (Exception ex)
//				{
//					provider.GetRequiredService<IAppLog>().LogException(ex);
//					return null;
//				}
//			});
//		}
//	}
//}
