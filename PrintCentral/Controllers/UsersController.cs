using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
		private readonly IUserData userData;
        private readonly IUserRepository repo;
        private readonly ILogService log;
		private readonly ILocalizationService g;
        private readonly ICompanyRepository companyRepository;
        private readonly IOrderEmailService orderEmailService;
        private readonly IOrderNotificationManager notificationManager;
        private readonly IProjectRepository projectRepository;
        private INotificationRepository notificationRepository;
        private IProviderRepository providerRepo;
        private readonly IConnectionManager _connManager;
        public UsersController(IUserData userData, IUserRepository repo, ILogService log, ILocalizationService g, ICompanyRepository companyRepo, IOrderEmailService orderEmailService, IOrderNotificationManager notificationManager, IProjectRepository projectRepository, INotificationRepository notificationRepository, IProviderRepository providerRepo, IConnectionManager connManager)
        {
            this.userData = userData;
            this.repo = repo;
            this.log = log;
            this.g = g;
            this.companyRepository = companyRepo;
            this.orderEmailService = orderEmailService;
            this.notificationManager = notificationManager;
            this.projectRepository = projectRepository;
            this.notificationRepository = notificationRepository;
            this.providerRepo = providerRepo;
            _connManager = connManager;
        }

        [HttpPost, Route("/users/insert")]
        public OperationResult Insert([FromBody]AppUser user)
        {
            try
            {
                if (!userData.Admin_Users_CanAdd)
                    return OperationResult.Forbid;
                if (!userData.Admin_Users_CanCreateWithoutEmail && !MailService.IsValidEmail(user.UserName))
					return new OperationResult(false, g["User name needs to be a valid email address."]);
				return new OperationResult(true, g["User Created!"], repo.Insert(user));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
        }

        [HttpPost, Route("/users/update")]
        public OperationResult Update([FromBody]AppUser data)
        {
            try
            {
                if (!userData.Admin_Users_CanEdit)
                    return OperationResult.Forbid;
     //           if (!userData.Admin_Users_CanCreateWithoutEmail && !MailService.IsValidEmail(data.UserName))
					//return new OperationResult(false, g["User name needs to be a valid email address."]);
                if (GetUserRoles(data.Id).Count == 0)
                    return new OperationResult(false, g["User must have at least one Rol."]);
                return new OperationResult(true, g["User saved!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
        }

		[HttpPost, Route("/users/delete/{id}")]
		public OperationResult Delete(string id)
		{
			var result = new OperationResult();
			try
			{
                if (!userData.Admin_Users_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
				return new OperationResult(true, g["User Deleted!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpGet, Route("/users/getbyid/{userid}")]
		public IAppUser GetByID(string userid)
		{
			try
			{
				return repo.GetByID(userid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/users/getbyname/{username}")]
		public IAppUser GetByName(string username)
		{
			try
			{
				return repo.GetByName(username);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

        [HttpPost, Route("/users/checkusername/{projectid}")]
        public async Task<OperationResult> CheckUserName([FromBody]List<SupplierUserChecks> listOfUsers, int projectid)
        { 
            try
            {
                var project = projectRepository.GetByID(projectid);  

                if(project == null)
                {
                    return new OperationResult(false, g["Operation could not be completed. Project does not exist!"]);
                }

                var customers = projectRepository.GetCustomerEmails(projectid); 

                if(customers == null)
                {
                    return new OperationResult(false, g[$"Operation could not be completed. Customers for project {project.Name} does not exist!"]);
                }
                // Group users by SupplierID
                var usersBySupplier = listOfUsers?.GroupBy(u => u.SupplierID).ToList();

                if (usersBySupplier == null || !usersBySupplier.Any())
                {
                    return new OperationResult(true, g["Operation OK! "]);
                }

                var allUsersNotFound = new List<SupplierUserChecks>();

                // Process each supplier group
                foreach (var supplierGroup in usersBySupplier)
                {
                    int supplierID = supplierGroup.Key;
                    var companyProvider = providerRepo.GetByID(supplierID);
                    if(companyProvider == null)
                    {
                        continue; 
                    }
                    var company = companyRepository.GetByID(companyProvider.ProviderCompanyID);
                    
                    if (company == null)
                    {
                        continue;
                    }

                    var usersNotFoundForSupplier = new List<SupplierUserChecks>();

                    // Check all users for this supplier
                    foreach (var user in supplierGroup)
                    {
                        if (!repo.ExistUser(user.UserEmail, company.ID))
                        {
                            usersNotFoundForSupplier.Add(user);
                        }
                    }

                    if (!usersNotFoundForSupplier.Any())
                    {
                        continue;
                    }

                    allUsersNotFound.AddRange(usersNotFoundForSupplier);

                    // Build user list for this supplier
                    var userNotFoundList = string.Join(Environment.NewLine, usersNotFoundForSupplier.Select(u => u.UserEmail));
                    var companyName = company.Name ?? "Unknown Company";

                    

                    // Send email notification for this supplier
                    var emailSubject = $"New users found for company {companyName}";
                    var emailBody = $"Please, register the following users into company {companyName}:{Environment.NewLine}{userNotFoundList}";
                    var emails = new List<string>();
                    foreach(var customer in customers)
                    {
                        var customerRepository = repo.GetByID(customer);

                        if(customerRepository == null)
                        {
                            log.LogException($"This customer ID:{customer} not exists!");
                            return new OperationResult(false, g["Operation could not be completed."]);
                        }

                        emails.Add(customerRepository?.Email);
                    }
                 //   notificationManager.RegisterErrorNotification(projectid, null, ErrorNotificationType.UserNotFound, emailSubject, emailBody, string.Empty, null);
                    SendUserNotFoundNotification(company.ID, projectid, emailBody, companyName); 
                    await orderEmailService.SendMessage(string.Join(';', emails), emailSubject, emailBody, null);
                    
                }

                if (!allUsersNotFound.Any())
                {
                    return new OperationResult(true, g["Operation OK! "]);
                }

                return new OperationResult(true, g["Operation OK! "]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        
        }

        private void SendUserNotFoundNotification(int companyID, int projectid, string message, string supplierName, string jsonData = null)
        {

            var data = jsonData != null ? jsonData : "{}";

            var sql = $@"INSERT INTO [dbo].[Notifications]
           ([CompanyID]
           ,[Type]
           ,[IntendedRole]
           ,[IntendedUser]
           ,[NKey]
           ,[Source]
           ,[Title]
           ,[Message]
           ,[Data]
           ,[AutoDismiss]
           ,[Count]
           ,[Action]
           ,[LocationID]
           ,[ProjectID]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[UpdatedBy]
           ,[UpdatedDate])
            VALUES
           ({companyID}
           ,1
           ,'{Service.Contracts.Authentication.Roles.IDTCostumerService}'
           ,''
           ,'ArmandThieryFtpPlugin/{message.GetHashCode().ToString()}'
           ,'ArmandThieryFtpPlugin'
           ,@title
           ,@msg
           ,@data
           ,0
           ,1
           ,null
           ,null
           ,{projectid}
           ,'SysAdmin'
           ,GETDATE()
           ,'System'
           ,GETDATE())";

            using(var db = _connManager.OpenDB("MainDB"))
            {
                var title = $"Error Armand Thiery Ftp Workflow with supplier {supplierName}.";
                db.ExecuteNonQuery(sql, title, message, data);
            }
        }

        [HttpGet, Route("/users/getlist")]
		public List<IAppUser> GetList()
		{
			try
			{
				return repo.GetList();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/users/getbycompanyid/{companyid}")]
		public List<IAppUser> GetByCompanyID(int companyid)
		{
			try
			{
				return repo.GetByCompanyID(companyid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

        [HttpGet, Route("/users/getcustomerlist/{companyid}/{isIDT}")]
        public List<IAppUser> GetCustomerList(int companyid, bool isIDT)
        {
            try
            {
                return repo.GetCustomerList(companyid, isIDT);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

		[HttpGet, Route("/users/GetCustomerServiceUsers")]
		public List<IAppUser> GetCustomerServiceUsers()
		{
			try
			{
				// SECURITY: Non-IDT users should not be able to query this data
				if (!userData.IsIDT)
					return null;

				return repo.GetCustomerServiceUsers();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}


		[HttpGet, Route("/users/getroles")]
		public List<IAppRole> GetRoles()
		{
			try
			{
				return repo.GetRoles();
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpGet, Route("/users/getuserroles/{userid}")]
		public List<string> GetUserRoles(string userid)
		{
			try
			{
				return repo.GetUserRoles(userid);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}

		[HttpPost, Route("/users/addrole/{userid}/{role}")]
		public OperationResult AddRole(string userid, string role)
		{
			try
			{
				if (!userData.CanAssignRole(role))
					return OperationResult.Forbid;
				repo.AddRole(userid, role);
				return new OperationResult(true, g["Role assigned!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/users/removerole/{userid}/{role}")]
		public OperationResult RemoveRole(string userid, string role)
		{
			try
			{
				if (!userData.CanAssignRole(role))
					return OperationResult.Forbid;
				repo.RemoveRole(userid, role);
				return new OperationResult(true, g["Role removed!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, g["Operation could not be completed."]);
			}
		}

		[HttpPost, Route("/users/updateprofile")]
		public OperationResult UpdateProfile([FromBody]ProfileDTO data)
		{
			try
			{
				repo.UpdateProfile(data);
				return new OperationResult(true, g["Profile updated!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}

		[HttpPost, Route("/users/resetpwd/{userid}")]
		public async Task<OperationResult> ResetPassword(string userid)
		{
			try
			{
				if (!userData.Admin_Users_CanResetPassword)
					return OperationResult.Forbid;
				await repo.ResetPasswordAsync(userid);
				return new OperationResult(true, g["Password was reset!"]);
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return new OperationResult(false, ex.Message);
			}
		}


		[HttpGet, Route("/users/getresetpwdurl/{userid}")]
		public async Task<string> getresetpwdurl(string userid)
		{
			try
			{
				return await repo.GetResetURLByUserNameAsync(userid);
				
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				return null;
			}
		}
	}
}