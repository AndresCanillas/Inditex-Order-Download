using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Miracle.FileZilla.Api;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WebLink.Contracts.Models
{
	public class FtpAccountRepository: IFtpAccountRepository
	{
		private IFactory factory;
		private IAppConfig config;
		private IEncryptionService dpp;

		public FtpAccountRepository(
			IFactory factory,
            IAppConfig config,
			IEncryptionService dpp
			)
		{
			this.factory = factory;
			this.config = config;
			this.dpp = dpp;
		}


		public FtpAccountInfo GetCompanyFtpAccount(int companyid, bool unprotect = false)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetCompanyFtpAccount(ctx, companyid, unprotect);
			}
		}


		public FtpAccountInfo GetCompanyFtpAccount(PrintDB ctx, int companyid, bool unprotect = false)
		{
			Company company = ctx.Companies.Where(c => c.ID == companyid).Single();
			var account = new FtpAccountInfo()
			{
				CompanyID = companyid,
				FtpServer = config["WebLink.FileZilla.FTPServer"],
				FtpPort = config.GetValue<int>("WebLink.FileZilla.Port"),
				FtpsPort = config.GetValue<int>("WebLink.FileZilla.FTPSPort")
			};

			if (!String.IsNullOrWhiteSpace(company.FtpUser))
			{
				account.FtpUser = company.FtpUser;
				if (unprotect)
					account.FtpPassword = dpp.DecryptString(company.FtpPassword);
			}

			return account;
		}


		public void SaveCompanyFtpAccount(FtpAccountInfo accInfo)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				SaveCompanyFtpAccount(ctx, accInfo);
			}
		}


		public void SaveCompanyFtpAccount(PrintDB ctx, FtpAccountInfo accInfo)
		{
			Company company = ctx.Companies.Where(c => c.ID == accInfo.CompanyID).Single();
			if (String.IsNullOrWhiteSpace(accInfo.FtpUser) || String.IsNullOrWhiteSpace(accInfo.FtpPassword))
				throw new InvalidOperationException("FTP Account information was not provided.");
			if (!IsValidFtpUser(accInfo.FtpUser))
				throw new InvalidOperationException("FTP User Name contains some invalid characters.");
			if (!IsValidFtpPassword(accInfo.FtpPassword))
				throw new FtpPasswordTooWeakException();
			var homePath = GetCompanyHomeDirectory(accInfo.CompanyID);
			if (!Directory.Exists(homePath))
				Directory.CreateDirectory(homePath);
			using (var api = OpenFtp())
			{
				var accountSettings = api.GetAccountSettings();
				User account = null;
				SharedFolder homeDir = null;

				if (String.IsNullOrWhiteSpace(company.FtpUser) && accountSettings.Users.Any(p => String.Compare(p.UserName, accInfo.FtpUser, true) == 0))
					throw new FtpAccountTakenException();
				if (!String.IsNullOrWhiteSpace(company.FtpUser) && company.FtpUser != accInfo.FtpUser && accountSettings.Users.Any(p => String.Compare(p.UserName, accInfo.FtpUser, true) == 0))
					throw new FtpAccountTakenException();

				if (!String.IsNullOrWhiteSpace(company.FtpUser))
					account = accountSettings.Users.FirstOrDefault(p => String.Compare(p.UserName, company.FtpUser, true) == 0);
				else
					account = accountSettings.Users.FirstOrDefault(p => String.Compare(p.UserName, accInfo.FtpUser, true) == 0);
				if (account == null)
				{
					account = new User() { UserName = accInfo.FtpUser };
					account.UserLimit = 3;
					accountSettings.Users.Add(account);
					homeDir = new SharedFolder()
					{
						Directory = homePath,
						AccessRights = AccessRights.DirList | AccessRights.FileRead | AccessRights.FileWrite | AccessRights.IsHome
					};
					account.SharedFolders.Add(homeDir);
				}
				account.UserName = accInfo.FtpUser;
				account.AssignPassword(accInfo.FtpPassword, api.ProtocolVersion);
				api.SetAccountSettings(accountSettings);
			}
			company.FtpUser = accInfo.FtpUser;
			company.FtpPassword = dpp.EncryptString(accInfo.FtpPassword);
			ctx.SaveChanges();
		}


		public void AddCompanyFtpDirectory(int companyid, string directoryName)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				AddCompanyFtpDirectory(ctx, companyid, directoryName);
			}
		}


		public void AddCompanyFtpDirectory(PrintDB ctx, int companyid, string directoryName)
		{
			if (!IsValidFtpDirectory(directoryName))
				throw new InvalidOperationException($"Specified directory name: \"{directoryName}\" is not valid.");

			Company company = ctx.Companies.Where(c => c.ID == companyid).Single();

			if (String.IsNullOrWhiteSpace(company.FtpUser) || String.IsNullOrWhiteSpace(company.FtpPassword))
				throw new InvalidOperationException("Company does not have a valid FTP account setup.");

			if (!Directory.Exists(directoryName))
				Directory.CreateDirectory(directoryName);

			using (var api = OpenFtp())
			{
				var accountSettings = api.GetAccountSettings();
				var account = accountSettings.Users.FirstOrDefault(p => String.Compare(p.UserName, company.FtpUser, true) == 0);
				if (account == null)
					throw new InvalidOperationException("Company does not have a valid FTP account setup.");

				var existingFolder = account.SharedFolders.Where(f => String.Compare(f.Directory, directoryName, 0) == 0).FirstOrDefault();
				if (existingFolder == null)
				{
					var ftpDir = new SharedFolder()
					{
						Directory = directoryName,
						AccessRights = AccessRights.DirList | AccessRights.FileRead | AccessRights.FileWrite
					};
					account.SharedFolders.Add(ftpDir);
					api.SetAccountSettings(accountSettings);
				}
			}
		}


		public void DeleteCompanyFtpDirectory(int companyid, string directoryName)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				DeleteCompanyFtpDirectory(ctx, companyid, directoryName);
			}
		}


		public void DeleteCompanyFtpDirectory(PrintDB ctx, int companyid, string directoryName)
		{
			if (!IsValidFtpDirectory(directoryName))
				throw new InvalidOperationException($"Specified directory name: \"{directoryName}\" is not valid.");

			Company company = ctx.Companies.Where(c => c.ID == companyid).Single();

			if (String.IsNullOrWhiteSpace(company.FtpUser) || String.IsNullOrWhiteSpace(company.FtpPassword))
				throw new InvalidOperationException("Company does not have a valid FTP account setup.");

			using (var api = OpenFtp())
			{
				var accountSettings = api.GetAccountSettings();
				var account = accountSettings.Users.FirstOrDefault(p => String.Compare(p.UserName, company.FtpUser, true) == 0);
				if (account == null)
					throw new InvalidOperationException("Company does not have a valid FTP account setup.");

				var existingFolder = account.SharedFolders.Where(f => String.Compare(f.Directory, directoryName, 0) == 0).FirstOrDefault();
				if (existingFolder != null)
				{
					account.SharedFolders.Remove(existingFolder);
					api.SetAccountSettings(accountSettings);
				}
			}
		}


		public void DeleteCompanyFtpAccount(string accountName)
		{
			using (var api = OpenFtp())
			{
				var accountSettings = api.GetAccountSettings();
				var account = accountSettings.Users.FirstOrDefault(p => String.Compare(p.UserName, accountName, true) == 0);
				if (account != null)
				{
					accountSettings.Users.Remove(account);
					api.SetAccountSettings(accountSettings);
				}
			}
		}


		public string GetCompanyHomeDirectory(int companyid)
		{
			var rootDir = config["WebLink.FileZilla.RootDirectory"];
			var homePath = Path.Combine(rootDir, "COMP_" + companyid.ToString("D3"));
			return homePath;
		}


		public bool IsValidFtpPassword(string ftpPassword)
		{
			if (String.IsNullOrWhiteSpace(ftpPassword))
				return false;
			bool hasUpperCase = ftpPassword.Any(c => Char.IsUpper(c));
			bool hasLowerCase = ftpPassword.Any(c => Char.IsLower(c));
			bool hasDigits = ftpPassword.Any(c => Char.IsDigit(c));
			return hasUpperCase && hasLowerCase && hasDigits && ftpPassword.Length >= 8;
		}


		public bool IsValidFtpUser(string ftpUser)
		{
			if (String.IsNullOrWhiteSpace(ftpUser))
				return false;
			var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			foreach (var c in ftpUser)
			{
				if (validChars.IndexOf(c) < 0)
					return false;
			}
			return true;
		}


		public bool IsValidFtpDirectory(string directory)
		{
			if (String.IsNullOrWhiteSpace(directory))
				return false;
			var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789\\_-:";
			foreach (var c in directory)
			{
				if (validChars.IndexOf(c) < 0)
					return false;
			}
			return true;
		}


		private FileZillaApi OpenFtp()
		{
			var adminPort = config.GetValue<int>("WebLink.FileZilla.AdminPort");
			var adminPwd = config["WebLink.FileZilla.AdminPwd"];
			var result = new FileZillaApi(IPAddress.Loopback, adminPort);
			result.Connect(adminPwd);
			return result;
		}
	}
}
