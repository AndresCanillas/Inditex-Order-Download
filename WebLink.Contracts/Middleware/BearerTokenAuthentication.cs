using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using WebLink.Contracts.Models;


namespace WebLink.Contracts.Middleware
{
	public class BearerTokenAuthentication
	{
		private readonly RequestDelegate next;
		private IActiveTokenService tokenService;

		public BearerTokenAuthentication(RequestDelegate next, IActiveTokenService tokenService)
		{
			this.next = next;
			this.tokenService = tokenService;
		}

		public async Task Invoke(HttpContext context)
		{
			ClaimsPrincipal principal;
			var authHeader = context.Request.Headers["Authorization"];
			if (authHeader.Count >= 1)
			{
				if(authHeader[0].StartsWith("basic", StringComparison.OrdinalIgnoreCase))
				{
					var creds = authHeader[0].Substring(6);
					var result = await tokenService.Authenticate(creds);
					if (result.Success)
					{
						context.User = result.Principal;
						await context.Response.WriteAsync(result.Token);
					}
					else
					{
						if (result.MustChangePassword)
						{
							context.Response.StatusCode = 401;
							context.Response.Headers.Add("Reason-Phrase", "Must change password");
						}
						else if (result.UserLockedOut)
						{
							context.Response.StatusCode = 401;
							context.Response.Headers.Add("Reason-Phrase", "Account locked");
						}
						else
						{
							context.Response.StatusCode = 403;
							context.Response.Headers.Add("Reason-Phrase", "Invalid username or password");
						}
					}
				}
				else if (authHeader[0].StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
				{
					var token = authHeader[0].Substring(7);
					if (tokenService.ValidateToken(token, out principal))
					{
						context.User = principal;
						await next(context);
					}
					else
					{
						context.Response.StatusCode = 403;
						context.Response.Headers.Add("Reason-Phrase", "Invalid bearer Token or session expired.");
					}
				}
			}
			else await next(context);
		}
	}
}