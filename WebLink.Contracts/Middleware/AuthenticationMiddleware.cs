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

namespace WebLink.Contracts.Middleware
{
	public class AuthenticationMiddleware
	{
		private readonly RequestDelegate next;
		private readonly IUserManager userManager;
		private readonly IAuthenticationCookieProtector dp;

		public AuthenticationMiddleware(
			RequestDelegate next,
			IUserManager userManager,
			IAuthenticationCookieProtector dp)
		{
			this.next = next;
			this.userManager = userManager;
			this.dp = dp;
		}


		public async Task Invoke(HttpContext context)
		{
			bool failedAuthentication = false;
			if (context.Request.Cookies.ContainsKey("Session.Data"))
			{
				try
				{
					var userId = dp.Unprotect(context.Request.Cookies["Session.Data"]);
					var user = await userManager.FindByIdAsync(userId);
					if (user != null)
					{
						var roles = await userManager.GetRolesAsync(user);
						var identity = new UserIdentity(user, roles);
						var principal = new UserPrincipal(identity);
						context.User = principal;
					}
				}
				catch
				{
					failedAuthentication = true;
				}
			}

			await next(context);

			if (failedAuthentication)
				context.Response.Cookies.Delete("Session.Data");
		}
	}
}
