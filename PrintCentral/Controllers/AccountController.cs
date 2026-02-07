using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private IUserManager userManager;
        private ISignInManager signInManager;
        private IRoleManager roleManager;
        private IUserDataCacheService userDataCache;
        private IDBConnectionManager connManager;
        private IEventQueue events;
        private ILogService log;
        private ILocalizationService g;
        private IUserRepository userRepo;
        private IProjectRepository projectRepo;
        private IFactory factory;

        public AccountController(
            IUserManager userManager,
            ISignInManager signInManager,
            IRoleManager roleManager,
            IUserDataCacheService userDataCache,
            IDBConnectionManager connManager,
            IEventQueue events,
            ILogService log,
            ILocalizationService g,
            IUserRepository repo,
            IProjectRepository projectRepo,
            IFactory factory
        )
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this.userDataCache = userDataCache;
            this.connManager = connManager;
            this.events = events;
            this.log = log;
            this.g = g;
            this.userRepo = repo;
            this.projectRepo = projectRepo;
            this.factory = factory;
        }

        [AllowAnonymous]
        public ViewResult Login(string returnUrl)
        {
            return View(new LoginRequest { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest loginModel)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.SignInAsync(loginModel.UserName, loginModel.Password);
                if (result.Success)
                {
                    AppUser user = await userManager.FindByNameAsync(loginModel.UserName);
                    if (!String.IsNullOrWhiteSpace(user.Language))
                        Response.Cookies.Append("language", user.Language);

                    var data = userDataCache.GetByUserName(user.UserName);
                    if (data.SelectedCompanyID != data.CompanyID)
                    {
                        data.SelectedCompanyID = user.CompanyID.Value;
                        var lastProject = projectRepo.GetDefaultProject(user.CompanyID.Value);
                        if (lastProject != null)
                        {
                            data.SelectedProjectID = lastProject.ID;
                            data.SelectedBrandID = lastProject.BrandID;
                        }
                        else
                        {
                            data.SelectedProjectID = 0;
                            data.SelectedBrandID = 0;
                        }
                        userDataCache.Update(data);
                    }
                    return Redirect(loginModel.ReturnUrl ?? "/");
                }
                else if (result.UserLockedOut)
                {
                    ModelState.AddModelError("", "Account locked");
                    return View(loginModel);
                }
            }
            ModelState.AddModelError("", "Invalid user name or password");
            return View(loginModel);
        }

        [AllowAnonymous]
        public async Task<ViewResult> Reset()
        {
            var tokenid = Request.Query["token"];
            var token = await userRepo.GetResetTokenAsync(tokenid);
            bool validToken = false;
            if (token != null)
                validToken = token.ValidUntil > DateTime.Now;
            return View(new ResetRequest() { Token = tokenid, IsValid = validToken });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(ResetRequest model)
        {
            model.Success = false;
            if (ModelState.IsValid)
            {
                try
                {
                    model.IsValid = true;
                    if (model.Password == model.Password2)
                    {
                        var user = await userManager.ResetPasswordAsync(model.Token, model.Password);
                        if (user.CompanyID == 1)
                        {
                            var hash = HashFunctions.SHA512($"{user.UserName.ToLower()}:{model.Password}");
                            var userRoles = await userManager.GetRolesAsync(user);
                            events.Send(new SmartdotsUserChangedEvent()
                            {
                                Operation = Operation.ProfileChanged,
                                CompanyID = 1,
                                Id = user.Id,
                                UserName = user.UserName,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                Email = user.Email,
                                Language = user.Language,
                                LocationID = user.LocationID,
                                PhoneNumber = user.PhoneNumber,
                                ShowAsUser = user.ShowAsUser,
                                PwdHash = hash,
                                Roles = userRoles.Merge(",")
                            });
                        }
                        model.Success = true;
                    }
                    else ModelState.AddModelError("", "Password and its confirmation do not match.");
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                    ModelState.AddModelError("", g["Operation could not be completed."]);
                }
            }
            return View(model);
        }

        private string GetErrors(IEnumerable<IdentityError> errors)
        {
            StringBuilder sb = new StringBuilder(1000);
            foreach (var e in errors)
                sb.Append(e.Description).Append(" ");
            return sb.ToString();
        }

        public RedirectResult Logout()
        {
            userDataCache.Remove(User.Identity.Name);
            Response.Cookies.Delete("Session.Data");
            return Redirect("/");
        }

        public async Task<RedirectResult> Lang(string id, string returnUrl = "/")
        {
            Response.Cookies.Append("language", id);
            if (User.Identity.IsAuthenticated)
            {
                AppUser user = await userManager.FindByNameAsync(User.Identity.Name);
                if (user != null)
                {
                    user.Language = id;
                    await userManager.UpdateAsync(user);
                }
            }
            return Redirect(returnUrl);
        }


        [AllowAnonymous, Route("/Account/AccessDenied")]
        public ViewResult AccessDenied()
        {
            return View("");
        }

        [Authorize, Route("/Account/SelectCompany/{companyid}")]
        public RedirectResult SelectCompany(int companyid)
        {
            var userData = userDataCache.GetUserData(User);
            if (userData.IsIDT)
            {
                var companyRepo = factory.GetInstance<ICompanyRepository>();
                var company = companyRepo.GetByID(companyid, true);
                if (company != null)
                {
                    var projectRepo = factory.GetInstance<IProjectRepository>();
                    IProject project = projectRepo.GetDefaultProject(companyid);
                    if (project != null)
                    {
                        userData.SelectedBrandID = project.BrandID;
                        userData.SelectedProjectID = project.ID;
                      
                    }
                    else
                    {
                        userData.SelectedBrandID = 0;
                        userData.SelectedProjectID = 0;
                        //userData.CanUseManualEntry = false;

                    }
                    userData.SelectedCompanyID = companyid;
                    userDataCache.Update(userData);
                }
            }
            return Redirect("/");
        }

        [Authorize, Route("/Account/SelectProject/{projectid}")]
        public RedirectResult SelectProject(int projectid)
        {
            var userData = userDataCache.GetUserData(User);
            var projectRepo = factory.GetInstance<IProjectRepository>();
            var project = projectRepo.GetByID(projectid);
            if (project != null)
            {
                var brandRepo = factory.GetInstance<IBrandRepository>();
                var brand = brandRepo.GetByID(project.BrandID);
                userData.SelectedCompanyID = brand.CompanyID;
                userData.SelectedBrandID = brand.ID;
                userData.SelectedProjectID = project.ID;

                userDataCache.Update(userData);
            }
            return Redirect("/");
        }
    }

    public class LoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }


    public class ResetRequest
    {
        public bool IsValid { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }
        public bool Success { get; set; }
    }
}