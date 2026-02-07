using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public interface IUserManager
	{
		IPasswordHasher<AppUser> PasswordHasher { get; }
		Task<AppUser> CreateAsync(AppUser user, string password);
		Task UpdateAsync(AppUser user);
		Task DeleteAsync(AppUser user);
		Task<AppUser> FindByIdAsync(string userid);
		Task<AppUser> FindByNameAsync(string userName);
		Task<List<string>> GetRolesAsync(AppUser user);
		Task AddToRoleAsync(AppUser user, string role);
		Task RemoveFromRoleAsync(AppUser user, string role);
		Task ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
		Task<AppUser> ResetPasswordAsync(string token, string password);
	}

	public class UserManager : IUserManager
	{
		private IFactory factory;
		private IPasswordHasher<AppUser> passwordHasher;
		private ILookupNormalizer normalizer;
		private PasswordValidator passwordValidator;


		public UserManager(IFactory factory, PasswordValidator passwordValidator, IPasswordHasher<AppUser> passwordHasher)
		{
			this.factory = factory;
			this.passwordValidator = passwordValidator;
			this.passwordHasher = passwordHasher;
			normalizer = new UpperInvariantLookupNormalizer();
		}


		public IPasswordHasher<AppUser> PasswordHasher { get => passwordHasher; }


		public async Task<AppUser> CreateAsync(AppUser user, string password)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var existing = await ctx.Users.Where(u => u.UserName == user.UserName).FirstOrDefaultAsync();
				if (existing != null)
					throw new InvalidOperationException($"User {user.UserName} already exists");

				user.Id = Guid.NewGuid().ToString();
				user.PasswordHash = passwordHasher.HashPassword(user, password);
				user.NormalizedUserName = normalizer.Normalize(user.UserName);
				user.NormalizedEmail = normalizer.Normalize(user.Email);
				ctx.Users.Add(user);
				await ctx.SaveChangesAsync();

				return user;
			}
		}

		public async Task UpdateAsync(AppUser user)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				user.NormalizedUserName = normalizer.Normalize(user.UserName);
				user.NormalizedEmail = normalizer.Normalize(user.Email);
				ctx.Users.Update(user);
				await ctx.SaveChangesAsync();
			}
		}

		public async Task DeleteAsync(AppUser user)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				ctx.Users.Remove(user);
				await ctx.SaveChangesAsync();
			}
		}

		public async Task<AppUser> FindByIdAsync(string userid)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return await ctx.Users.Where(u => u.Id == userid).FirstOrDefaultAsync();
			}
		}

		public async Task<AppUser> FindByNameAsync(string userName)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return await ctx.Users.Where(u => u.UserName == userName).FirstOrDefaultAsync();
			}
		}

		public async Task<List<string>> GetRolesAsync(AppUser user)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return await (from ur in ctx.UserRoles
							  join r in ctx.Roles on ur.RoleId equals r.Id
							  where ur.UserId == user.Id
							  select r.Name).ToListAsync();
			}
		}

		public async Task AddToRoleAsync(AppUser user, string role)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var r = await ctx.Roles.Where(p => p.Name == role).FirstOrDefaultAsync();
				if (r == null)
					throw new InvalidOperationException($"Role {role} is invalid.");

				var existing = await (from ur in ctx.UserRoles
									  where ur.UserId == user.Id && ur.RoleId == r.Id
									  select ur).FirstOrDefaultAsync();

				if (existing == null)
				{
					AppUserRole ur = new AppUserRole()
					{
						UserId = user.Id,
						RoleId = r.Id
					};
					ctx.UserRoles.Add(ur);
					await ctx.SaveChangesAsync();
				}
			}
		}

		public async Task RemoveFromRoleAsync(AppUser user, string role)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var r = await ctx.Roles.Where(p => p.Name == role).FirstOrDefaultAsync();
				if (r == null)
					throw new InvalidOperationException($"Role {role} is invalid.");

				var existing = await (from ur in ctx.UserRoles
									  where ur.UserId == user.Id && ur.RoleId == r.Id
									  select ur).FirstOrDefaultAsync();

				if (existing != null)
				{
					ctx.UserRoles.Remove(existing);
					await ctx.SaveChangesAsync();
				}
			}
		}

		public async Task ChangePasswordAsync(AppUser user, string currentPassword, string newPassword)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var validationResult = passwordValidator.Validate(newPassword);
				if (!validationResult.Success)
					throw new InvalidOperationException(validationResult.ErrorMessage);

				var actual = await ctx.Users.Where(u => u.Id == user.Id).FirstOrDefaultAsync();
				if (actual == null)
					throw new InvalidOperationException($"User {user.UserName} could not be found.");

				if (passwordHasher.VerifyHashedPassword(actual, actual.PasswordHash, currentPassword) != PasswordVerificationResult.Success)
					throw new InvalidOperationException("The supplied current password is invalid");

				actual.PasswordHash = passwordHasher.HashPassword(user, newPassword);
				ctx.Users.Update(actual);
				await ctx.SaveChangesAsync();
			}
		}


		public async Task<AppUser> ResetPasswordAsync(string tokenid, string password)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var validationResult = passwordValidator.Validate(password);
				if (!validationResult.Success)
					throw new InvalidOperationException(validationResult.ErrorMessage);

				bool validToken = false;
				var token = await ctx.ResetTokens.Where(t => t.ID == tokenid).FirstOrDefaultAsync();
				if (token != null)
					validToken = token.ValidUntil > DateTime.Now;

				if (!validToken)
					throw new InvalidOperationException("The link to reset the password has expired.");

				AppUser user = await FindByIdAsync(token.UserName);
				if (user != null)
				{
					ctx.ResetTokens.Remove(token);
					user.PasswordHash = passwordHasher.HashPassword(user, password);
					ctx.Users.Update(user);
					await ctx.SaveChangesAsync();
					return user;
				}
				throw new InvalidOperationException("User could not be found!");
			}
		}
	}
}
