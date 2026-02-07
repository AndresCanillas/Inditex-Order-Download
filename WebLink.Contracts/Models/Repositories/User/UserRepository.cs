using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WebLink.Contracts.Models
{
	public class UserRepository : IUserRepository
	{
		private IFactory factory;
		private IUserManager userManager;
		private IRoleManager roleManager;
		private IUserDataCacheService userDataCache;
		private IEventQueue events;
		private IAppConfig config;
		private IEmailTemplateService mailService;
		private ILocalizationService g;
		private IDBConnectionManager connectionManager;

		public UserRepository(
			IFactory factory,
			IUserManager userManager,
			IRoleManager roleManager,
			IUserDataCacheService userDataCache,
			IEventQueue events,
			IAppConfig config,
			IEmailTemplateService mailService,
			ILocalizationService g,
			IDBConnectionManager connectionManager
			)
		{
			this.factory = factory;
			this.userManager = userManager;
			this.roleManager = roleManager;
			this.userDataCache = userDataCache;
			this.events = events;
			this.config = config;
			this.mailService = mailService;
			this.g = g;
			this.connectionManager = connectionManager;
		}

		private void UpdateEntity(IUserData userData, AppUser actual, IAppUser data)
		{
			if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
				actual.CompanyID = data.CompanyID;
			else
				actual.CompanyID = userData.SelectedCompanyID;
			actual.LocationID = data.LocationID;
			actual.ShowAsUser = true;
			if (data.ShowAsUser == false && userData.Principal.IsInRole(Roles.SysAdmin))
				actual.ShowAsUser = false;
			actual.FirstName = String.IsNullOrWhiteSpace(data.FirstName) ? "" : data.FirstName;
			actual.LastName = String.IsNullOrWhiteSpace(data.LastName) ? "" : data.LastName;
			if (!userData.Admin_Users_CanCreateWithoutEmail && String.IsNullOrWhiteSpace(data.Email))
				data.Email = data.UserName;
			actual.Email = String.IsNullOrWhiteSpace(data.Email) ? "" : data.Email;
			actual.PhoneNumber = String.IsNullOrWhiteSpace(data.PhoneNumber) ? "" : data.PhoneNumber;
			actual.Language = String.IsNullOrWhiteSpace(data.Language) ? "en-US" : data.Language;
		}


		public IAppUser Insert(IAppUser data)
		{
			var userData = factory.GetInstance<IUserData>();
			AppUser actual = new AppUser(data.UserName);
			data.ShowAsUser = true;
			UpdateEntity(userData, actual, data);
			return userManager.CreateAsync(actual, GetRandomPassword()).Result;
		}


		private string GetRandomPassword()
		{
			var rnd = new Random();
			var letters = "bcdefghijklmnopqrstuvwxyz";
			var digits = "0123456789";
			var special = "-_.:,;'?!*+=#@$%&()<>[]|";
			var sb = new StringBuilder(30);
			sb.Append(letters[rnd.Next(letters.Length)]);
			sb.Append(digits[rnd.Next(digits.Length)]);
			sb.Append(special[rnd.Next(special.Length)]);
			sb.Append(Char.ToUpper(letters[rnd.Next(letters.Length)]));
			for (var i = 0; i < 6; i++)
			{
				switch (rnd.Next(4))
				{
					case 0:
						sb.Append(letters[rnd.Next(letters.Length)]);
						break;
					case 1:
						sb.Append(Char.ToUpper(letters[rnd.Next(letters.Length)]));
						break;
					case 2:
						sb.Append(digits[rnd.Next(digits.Length)]);
						break;
					case 3:
						sb.Append(special[rnd.Next(special.Length)]);
						break;
				}
			}
			return sb.ToString();
		}


		public IAppUser Update(IAppUser data)
		{
			var userData = factory.GetInstance<IUserData>();
			var actual = GetByID(userManager, data.Id) as AppUser;
			UpdateEntity(userData, actual, data);
			userManager.UpdateAsync(actual).Wait();
			return actual;
		}


		public void Delete(string userid)
		{
			var actual = GetByID(userManager, userid) as AppUser;
			userManager.DeleteAsync(actual).Wait();
			if (actual.CompanyID == 1)
			{
				events.Send(new SmartdotsUserChangedEvent()
				{
					Operation = Operation.UserDeleted,
					Id = actual.Id,
					UserName = actual.UserName
				});
			}
		}


		public IAppUser GetByID(string userid)
		{
			return GetByID(userManager, userid);
		}


		public IAppUser GetByID(IUserManager userManager, string userid)
		{
			var userData = factory.GetInstance<IUserData>();
			int companyid = userData.SelectedCompanyID;
			var actual = userManager.FindByIdAsync(userid).Result;
			if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager, Roles.IDTExtProdManager))
				companyid = 0;
			bool ignoreShowUser = false;
			if (userData.Principal.IsInRole(Roles.SysAdmin))
				ignoreShowUser = true;
			if (actual == null || (!actual.ShowAsUser && !ignoreShowUser) || (actual.CompanyID != companyid && companyid != 0))
				throw new Exception("Not authorized");
			
			return actual; 
		}


		public IAppUser GetByName(string name)
		{
			return GetByName(userManager, name);
		}


		public IAppUser GetByName(IUserManager userManager, string name)
		{
			var userData = factory.GetInstance<IUserData>();
			int companyid = userDataCache.GetByUserName(name).SelectedCompanyID;
			if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
				companyid = 0;
			bool ignoreShowUser = false;
			if (userData.Principal.IsInRole(Roles.SysAdmin))
				ignoreShowUser = true;
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var actual = ctx.Users
					.Where(u => u.UserName == name &&
						(u.CompanyID == companyid || companyid == 0) &&
						((u.ShowAsUser == true || ignoreShowUser)))
					.FirstOrDefault();
				if (actual == null)
					throw new Exception("Not authorized");
				return actual;
			}
		}


		public List<IAppUser> GetByCompanyID(int companyid)
		{
			return GetByCompanyID(userManager, companyid);
		}


		public List<IAppUser> GetByCompanyID(IUserManager userManager, int companyid)
		{
			var userData = factory.GetInstance<IUserData>();
			int usercompany = userData.SelectedCompanyID;
			if (usercompany != companyid && !userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
				throw new Exception("Not Authorized");
			bool showHidden = false;
			if (userData.Principal.IsAnyRole(Roles.SysAdmin))
				showHidden = true;
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return new List<IAppUser>(
				ctx.Users.Where(u =>
					(u.CompanyID == companyid) &&
					(u.ShowAsUser == true || showHidden))
				.OrderBy(u => u.FirstName).ThenBy(u => u.UserName));
			}
		}


		public List<IAppUser> GetList()
		{
			var userData = factory.GetInstance<IUserData>();
			int companyid = userData.SelectedCompanyID;
			if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
				companyid = 0;
			bool showHidden = false;
			if (userData.Principal.IsInRole(Roles.SysAdmin))
				showHidden = true;
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return new List<IAppUser>(ctx.Users.Where(u =>
					(u.CompanyID == companyid || companyid == 0) &&
					(u.ShowAsUser == true || showHidden)));
			}
		}


		public List<IAppUser> GetCustomerList(int companyid, bool isIDT)
		{
			var users = GetByCompanyID(userManager, companyid);
			users.Insert(0, new AppUser { Id = string.Empty, UserName = " " });

			if (isIDT)
			{
				return users.Where(x => string.IsNullOrEmpty(x.Id) || GetUserRoles(userManager, x.Id).Contains(Roles.IDTCostumerService.ToString())).ToList();
			}
			else
				return users;
		}


		public List<IAppRole> GetRoles()
		{
			var userData = factory.GetInstance<IUserData>();
			var roles = new List<IAppRole>(roleManager.Roles.OrderBy(p => p.Name));
			if (!userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
				roles.RemoveAll(p => Roles.IsIDTRole(p.Name));
			if (!userData.Principal.IsInRole(Roles.SysAdmin))
				roles.RemoveAll(p => p.Name == Roles.SysAdmin);
			return roles;
		}


        public List<string> GetUserRoles(string userid)
        {
			return GetUserRoles(userManager, userid);
        }


		public List<string> GetUserRoles(IUserManager userManager, string userid)
		{
			var actual = GetByID(userManager, userid) as AppUser;
			return userManager.GetRolesAsync(actual).Result.ToList();
		}


		public void AddRole(string userid, string role)
		{
			var actual = GetByID(userManager, userid) as AppUser;
			var validRoles = GetRoles();
			var isValidRole = validRoles.FirstOrDefault(p => p.Name == role);
			if (isValidRole == null)
				throw new Exception("Not Authorized");

			userManager.AddToRoleAsync(actual, role).Wait();

			if (actual.CompanyID == 1)
			{
				events.Send(new SmartdotsUserChangedEvent()
				{
					Operation = Operation.RoleAdded,
					Id = actual.Id,
					UserName = actual.UserName,
					Role = role
				});
			}
		}


		public void RemoveRole(string userid, string role)
		{
			var actual = GetByID(userManager, userid) as AppUser;
			var validRoles = GetRoles();
			var isValidRole = validRoles.FirstOrDefault(p => p.Name == role);
			if (isValidRole == null)
				throw new Exception("Not Authorized");

			userManager.RemoveFromRoleAsync(actual, role).Wait();

			if (actual.CompanyID == 1)
			{
				events.Send(new SmartdotsUserChangedEvent()
				{
					Operation = Operation.RoleRemoved,
					Id = actual.Id,
					UserName = actual.UserName,
					Role = role
				});
			}
		}


		public void UpdateProfile(ProfileDTO data)
		{
			var userData = factory.GetInstance<IUserData>();
			AppUser appUser = userManager.FindByNameAsync(userData.UserName).Result;
			if (appUser != null)
			{
				if (data.Id != appUser.Id)
					throw new Exception("Received invalid data (Id)");
				appUser.FirstName = data.FirstName;
				appUser.LastName = data.LastName;
				appUser.Email = data.Email;
				appUser.PhoneNumber = data.PhoneNumber;
				appUser.Language = data.Language;
				userManager.UpdateAsync(appUser).Wait();

				var userRoles = userManager.GetRolesAsync(appUser).Result;
				var eventData = new SmartdotsUserChangedEvent()
				{
					Operation = Operation.ProfileChanged,
					CompanyID = 1,
					Id = appUser.Id,
					UserName = appUser.UserName,
					FirstName = appUser.FirstName,
					LastName = appUser.LastName,
					Email = appUser.Email,
					Language = appUser.Language,
					LocationID = appUser.LocationID,
					PhoneNumber = appUser.PhoneNumber,
					ShowAsUser = appUser.ShowAsUser,
					Roles = userRoles.Merge(",")
				};
				if (data.ChangePassword)
				{
					if (String.IsNullOrWhiteSpace(data.OriginalPassword))
						throw new Exception("Original password is required.");
					if (String.IsNullOrWhiteSpace(data.NewPassword))
						throw new Exception("New password is required.");
					if (data.NewPassword != data.PasswordConfirmation)
						throw new Exception("Password confirmation does not match.");
					userManager.ChangePasswordAsync(appUser, data.OriginalPassword, data.NewPassword).Wait();

					var hash = HashFunctions.SHA512($"{appUser.UserName.ToLower()}:{data.NewPassword}");
					eventData.PwdHash = hash;
				}
				if (appUser.CompanyID == 1)
					events.Send(eventData);
			}
			else throw new Exception($"Could not load data for user ({userData.UserName})");
		}

		public async Task ResetPasswordAsync(string userid)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				AppUser usr = await userManager.FindByIdAsync(userid);
				usr.PasswordHash = userManager.PasswordHasher.HashPassword(usr, GetRandomPassword());  // ensures current password is changed to a random value too...
				usr.LockoutEnd = null;
				await userManager.UpdateAsync(usr);

				var token = new ResetToken()
				{
					ID = Guid.NewGuid().ToString("N"),
					UserName = userid,
					ValidUntil = DateTime.Now.AddDays(3)
				};

				ctx.ResetTokens.Add(token);
				await ctx.SaveChangesAsync();

				var baseUrl = config.GetValue<string>("WebLink.BaseUrl");

				var info = new PasswordResetTemplateInfo()
				{
					Title = "Print Smartdots - " + g["Password Reset"],
					Subtitle = g["Password Reset Notification"],
					Line1 = g[$"This is an automated message from {baseUrl}, please do not reply to this email."],
					Line2 = g["Your account has been reset by the system administrator, please follow the link below to create your new password:"],
					Link = $"{baseUrl}account/reset?token={token.ID}",
					Copyright = g["Copyright"]
				};

				var message = mailService.CreateFromTemplate("wwwroot\\PasswordResetTemplate.html", info);
				message.To = usr.Email;
				message.Subject = info.Title;
				message.EmbbedImage("logo", "wwwroot\\images\\SDS_LOGOMail.png");
				await message.SendAsync();
			}
		}

		public List<IAppUser> GetProdManagers(int? byFactoryId, bool IsIDT = true)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return (from u in ctx.Users
						join ur in ctx.UserRoles on u.Id equals ur.UserId
						join r in ctx.Roles on ur.RoleId equals r.Id
						where r.Name == Roles.IDTProdManager
						&& (IsIDT == false || u.CompanyID == 1)
						&& (byFactoryId.HasValue == false || u.LocationID == byFactoryId)
						select u).ToList<IAppUser>();
			}
		}

		public List<IAppUser> GetCustomerServiceUsers()
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return (from u in ctx.Users
						join ur in ctx.UserRoles on u.Id equals ur.UserId
						join r in ctx.Roles on ur.RoleId equals r.Id
						where r.Name == Roles.IDTCostumerService && u.CompanyID == 1
						select u).ToList<IAppUser>();
			}
		}

		public List<IAppUser> GetAdmins()
        {
            using (var ctx = factory.GetInstance<IdentityDB>())
            {
                return (from u in ctx.Users
                        join ur in ctx.UserRoles on u.Id equals ur.UserId
                        join r in ctx.Roles on ur.RoleId equals r.Id
                        where r.Name == Roles.SysAdmin 
                        && u.CompanyID == 1
                        select u).ToList<IAppUser>();
            }
        }

		public List<IAppUser> GetCustomerServiceUsersWithProjects()
		{
			using (var conn = connectionManager.OpenWebLinkDB()) 
			{
				var query = $@"select distinct u.* 
				from {connectionManager.UsersDB}.dbo.AspNetUsers u
				join {connectionManager.UsersDB}.dbo.AspNetUserRoles ur on u.Id = ur.UserId
				join {connectionManager.UsersDB}.dbo.AspNetRoles r on ur.RoleId = r.Id
				join (
					select p.ID, c.CustomerSupport1 cc1, c.CustomerSupport2 cc2, 
						p.CustomerSupport1 cp1, p.CustomerSupport2 cp2
					from {connectionManager.WebLinkDB}.dbo.Projects p
					join {connectionManager.WebLinkDB}.dbo.Brands b on p.BrandID = b.ID
					join {connectionManager.WebLinkDB}.dbo.Companies c on b.CompanyID = c.ID
				) AS up on u.Id in (cc1,cc2,cp1,cp2)
				where u.CompanyID = 1 AND r.Name = 'IDTCommercial'
				order by u.firstname, u.lastname";

				var users = conn.Select<IAppUser>(query);
				return users.ToList();	
			}
		}

		public async Task<IResetToken> GetResetTokenAsync(string id)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				return await ctx.ResetTokens.Where(p => p.ID == id).AsNoTracking().SingleOrDefaultAsync();
			}
		}

		public async Task<string> GetResetURLByUserNameAsync(string userName)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var baseUrl = config.GetValue<string>("WebLink.BaseUrl");
				var token = await ctx.ResetTokens.Where(p => p.UserName == userName).AsNoTracking().OrderByDescending(o=>o.ValidUntil).FirstOrDefaultAsync(); 
				return (token is null) ? string.Empty : $"{baseUrl}account/reset?token={token.ID}";
			}
		}
		public async Task DeleteResetTokenAsync(string token)
		{
			using (var ctx = factory.GetInstance<IdentityDB>())
			{
				var t = await ctx.ResetTokens.Where(p => p.ID == token).SingleOrDefaultAsync();
				if (t != null)
				{
					ctx.ResetTokens.Remove(t);
					await ctx.SaveChangesAsync();
				}
			}
		}

		private string GetErrors(IEnumerable<IdentityError> errors)
		{
			StringBuilder sb = new StringBuilder(1000);
			foreach (var e in errors)
				sb.Append(e.Description).Append(" ");
			return sb.ToString();
		}

        public bool ExistUser(string username, int companyId)
        {
            using(var ctx = factory.GetInstance<IdentityDB>())
            {
                return ctx.Users
                     .Any(u => u.UserName == username &&
                         (u.CompanyID == companyId || companyId == 0));
                    
            }
        }
    }


	public class PasswordResetTemplateInfo
	{
		public string Title = "";
		public string Subtitle = "";
		public string Line1 = "";
		public string Line2 = "";
		public string Link = "";
		public string Copyright = "";
	}
}
