using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models 
{
	public interface ISignInManager
	{
		Task<AuthenticationResult> SignInAsync(string userName, string password);
		Task<AuthenticationResult> SignInAsync(AppUser user, string password);
		void SignOut();
	}


	public class SignInManager : ISignInManager
	{
		private IFactory factory;
		private IdentityOptions options;
		private IAuthenticationCookieProtector dp;
		private IPasswordHasher<AppUser> passwordHasher;

		public SignInManager(IFactory factory, IdentityOptions options, IAuthenticationCookieProtector dp, IPasswordHasher<AppUser> passwordHasher)
		{
			this.factory = factory;
			this.options = options;
			this.dp = dp;
			this.passwordHasher = passwordHasher;
		}


		public async Task<AuthenticationResult> SignInAsync(string userName, string password)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
                var __actual = await ctx.Users.Where(x => x.UserName == userName).ToListAsync();
                if (__actual.Count <= 0)
                    return new AuthenticationResult() { Success = false };
                var actual = __actual.Single();
				return await InternalSignInAsync(ctx, actual, password);
			}
		}


		public async Task<AuthenticationResult> SignInAsync(AppUser user, string password)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return await InternalSignInAsync(ctx, user, password);
			}
		}


		public async Task<AuthenticationResult> InternalSignInAsync(IdentityDB ctx, AppUser user, string password)
		{
			//if (user.MustChangePassword)
			//	return new AuthenticationResult() { MustChangePassword = true };

			if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
				return new AuthenticationResult() { UserLockedOut = true };

            
			if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Success)
			{
				user.AccessFailedCount = 0;
				user.LockoutEnd = null;
				await ctx.SaveChangesAsync();

				var hca = factory.GetInstance<IHttpContextAccessor>();
				if (hca.HttpContext != null)
					hca.HttpContext.Response.Cookies.Append("Session.Data", dp.Protect(user.Id), new CookieOptions() { MaxAge = TimeSpan.FromDays(365) });

				return new AuthenticationResult()
				{
					Success = true
				};
			}
			else
			{
				user.AccessFailedCount++;
				if (user.AccessFailedCount >= options.Lockout.MaxFailedAccessAttempts)
					user.LockoutEnd = DateTimeOffset.Now.Add(options.Lockout.DefaultLockoutTimeSpan);
				await ctx.SaveChangesAsync();
				return new AuthenticationResult() { Success = false };
			}
		}


		public void SignOut()
		{
			var hca = factory.GetInstance<IHttpContextAccessor>();
			if (hca.HttpContext != null)
				hca.HttpContext.Response.Cookies.Delete("Session.Data");
		}
	}
}
