using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class UserDataCacheService : IUserDataCacheService
    {
        private IFactory factory;
        private IUserData systemUser;
        private ConcurrentDictionary<string, UserData> index = new ConcurrentDictionary<string, UserData>();


        public UserDataCacheService(IFactory factory)
        {
            this.factory = factory;
            systemUser = new UserData(new SystemIdentity(), new UserData() { CompanyID = 1, SelectedCompanyID = 1, SelectedBrandID = 1, SelectedProjectID = 1, LocationID = 1 });
        }


        public IUserData GetUserData(ClaimsPrincipal principal)
        {
            var data = GetByUserName(principal.Identity.Name);
            UserData cpy = new UserData(principal, data);
            return cpy;
        }


        public IUserData GetDefaultUserData()
        {
            return systemUser;
        }


        public IUserData GetByUserName(string userName)
        {
            if (userName == null)
                return systemUser;
            if (index.TryGetValue(userName, out var userData))
                return userData;

            using (var db = factory.GetInstance<IdentityDB>())
            {
                var user = db.Users.Where(u => u.UserName == userName).SingleOrDefault();
                if (user == null)
                    throw new Exception($"User with name {userName} could not be found.");

                var data = new UserData() { Id = user.Id, UserName = user.UserName, CompanyID = user.CompanyID.Value, LocationID = user.LocationID ?? 0 };

                if (!user.SelectedCompanyID.HasValue)
                    data.SelectedCompanyID = data.CompanyID;
                else
                    data.SelectedCompanyID = user.SelectedCompanyID.Value;

                if (!user.SelectedBrandID.HasValue)
                {
                    using (var ctx = factory.GetInstance<PrintDB>())
                    {
                        var lastProject = (from p in ctx.Projects
                                           join b in ctx.Brands on p.BrandID equals b.ID
                                           where b.CompanyID == user.CompanyID
                                           select p).OrderByDescending(p => p.CreatedDate).Take(1).AsNoTracking().FirstOrDefault();

                        if (lastProject != null)
                        {
                            user.SelectedProjectID = lastProject.ID;
                            user.SelectedBrandID = lastProject.BrandID;
                            
                            
                            SetManualEntry(data, ctx);
                            SetOrderPoolManager(data, ctx); 
                            db.SaveChanges();
                        }
                    }
                }

                data.SelectedBrandID = user.SelectedBrandID ?? 0;
                data.SelectedProjectID = user.SelectedProjectID ?? 0;

                if (data.SelectedProjectID > 0)
                {
                    using (var ctx = factory.GetInstance<PrintDB>())
                    {
                        var project = ctx.Projects.FirstOrDefault(p => p.ID == data.SelectedProjectID);


                    }
                }


                if (!index.TryAdd(user.UserName, data))
                    data = index[user.UserName];

                return data;
            }
        }

        private static void SetOrderPoolManager(UserData data, PrintDB ctx)
        {
            data.ManualEntryUrl = ctx
                                    .ManualEntryForms
                                    .Any(m => m.CompanyID == data.CompanyID && m.ProjectID == data.SelectedProjectID && m.FormType == Contracts.Models.Repositories.ManualEntry.Entities.OrderPoolFormType.PoolManagerForm) ?
                                     ctx.ManualEntryForms.FirstOrDefault(m => m.CompanyID == data.CompanyID && m.ProjectID == data.SelectedProjectID && m.FormType == Contracts.Models.Repositories.ManualEntry.Entities.OrderPoolFormType.PoolManagerForm).Url :
                                     string.Empty;
        }


        private static void SetManualEntry(UserData data, PrintDB ctx)
        {
            data.ManualEntryUrl = ctx
                                    .ManualEntryForms
                                    .Any(m => m.ProjectID == data.SelectedProjectID && m.FormType == Contracts.Models.Repositories.ManualEntry.Entities.OrderPoolFormType.ManualEntryForm) ?
                                     ctx.ManualEntryForms.FirstOrDefault(m => m.ProjectID == data.SelectedProjectID && m.FormType == Contracts.Models.Repositories.ManualEntry.Entities.OrderPoolFormType.ManualEntryForm).Url :
                                     string.Empty;
        }

        public void Update(IUserData data)
        {
            bool success = false;
            using (var db = factory.GetInstance<IdentityDB>())
            {
                do
                {
                    var user = db.Users.Where(u => u.UserName == data.UserName).SingleOrDefault();
                    if (user == null) throw new Exception($"User {data.UserName} could not be found.");
                    user.SelectedCompanyID = data.SelectedCompanyID;
                    user.SelectedBrandID = data.SelectedBrandID;
                    user.SelectedProjectID = data.SelectedProjectID;
                    try
                    {
                        db.SaveChanges();
                        success = true;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        var entry = ex.Entries.Single();
                        entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                    }
                } while (!success);
                if (index.TryGetValue(data.UserName, out var cachedData))
                {

                    if (cachedData.SelectedBrandID != data.SelectedCompanyID
                        || cachedData.SelectedCompanyID != data.SelectedCompanyID
                        || cachedData.SelectedProjectID != data.SelectedProjectID)
                    {

                        cachedData.SelectedCompanyID = data.SelectedCompanyID;
                        cachedData.SelectedBrandID = data.SelectedBrandID;
                        cachedData.SelectedProjectID = data.SelectedProjectID;
                        index.TryAdd(data.UserName, cachedData);
                    }
                    else
                    {
                        cachedData.SelectedCompanyID = data.SelectedCompanyID;
                        cachedData.SelectedBrandID = data.SelectedBrandID;
                        cachedData.SelectedProjectID = data.SelectedProjectID;
                    }


                }
            }
        }


        public void Remove(string userName)
        {
            index.TryRemove(userName, out _);
        }
    }



    class UserData : IUserData, IUserFeatures
    {
        private volatile int companyid;
        private volatile int locationid;
        private volatile int selectedcompanyid;
        private volatile int selectedbrandid;
        private volatile int selectedprojectid;

        public UserData() { }

        public UserData(ClaimsPrincipal principal, IUserData data)
        {
            this.Principal = principal;
            this.Id = data.Id;
            this.UserName = principal.Identity.Name;
            this.companyid = data.CompanyID;
            this.locationid = data.LocationID;
            this.selectedcompanyid = data.SelectedCompanyID;
            this.selectedbrandid = data.SelectedBrandID;
            this.selectedprojectid = data.SelectedProjectID;
            this.IsLockoutEnabled = data.IsLockoutEnabled;
            this.LockoutEnd = data.LockoutEnd;
        }

        public string Id { get; set; }
        public string UserName { get; set; }
        public ClaimsPrincipal Principal { get; set; }
        public int CompanyID { get => companyid; set => companyid = value; }
        public int LocationID { get => locationid; set => locationid = value; }
        public int SelectedCompanyID { get => selectedcompanyid; set => selectedcompanyid = value; }
        public int SelectedBrandID { get => selectedbrandid; set => selectedbrandid = value; }
        public int SelectedProjectID { get => selectedprojectid; set => selectedprojectid = value; }
        public bool IsLockoutEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }




        public bool IsCompositionChecker
        {
            get
            {
                return Principal.IsInRole(Roles.CompositionChecker);
            }
        }

        //Validate if the user is any of the public Roles
        public bool IsPublicRoles
        {
            get
            {
                return Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager, Roles.DataUpload, Roles.PrinterOperator);
            }
        }

        public IEnumerable<string> UserRoles
        {
            get
            {
                return ((ClaimsIdentity)Principal.Identity).Claims
                .Where(c => c.Type == ClaimTypes.Role).Select(r => r.Value);
            }
        }

        public bool IsIDT { get => CompanyID == 1; }

        public bool IsIDTAdminRoles
        {
            get
            {
                return CompanyID == 1 && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager, Roles.IDTLabelDesign);
            }
        }

        public bool IsSysAdmin
        {
            get
            {
                return CompanyID == 1 && Principal.IsInRole(Roles.SysAdmin);
            }
        }

        public bool IsDesigner
        {
            get
            {
                return CompanyID == 1 && Principal.IsInRole(Roles.IDTLabelDesign);
            }
        }

        public bool IsIDTExternal
        {
            get
            {
                return CompanyID == 1 && Principal.IsInRole(Roles.IDTExtProdManager);
            }
        }

        public bool CanSelectCompany
        {
            get
            {
                return IsIDT;
            }
        }

        public bool CanSelectBrandProject
        {
            get
            {
                var roles = ((ClaimsIdentity)Principal.Identity).Claims
                .Where(c => c.Type == ClaimTypes.Role).Count();
                return roles > 0;
            }
        }

        public bool CanSeeCompanyFilter
        {
            get
            {
                return IsIDT && SelectedCompanyID == 1;
            }
        }

        public bool CanSeeVMenu
        {
            get
            {
                return IsIDT || IsPublicRoles;
            }
        }

        public bool CanSeeVMenu_UploadMenu
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.DataUpload, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_UploadOrder
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.DataUpload, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_UploadData
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.DataUpload, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_UploadPoolFile
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_UploadOrdersReport
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.DataUpload, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_PrintMenu
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.PrinterOperator, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_PrintLabels
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.PrinterOperator, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_PrintJobsReport
        {
            get
            {
                return IsSysAdmin || Principal.IsAnyRole(Roles.IDTProdManager, Roles.CompanyAdmin, Roles.ProdManager, Roles.PrinterOperator);
            }
        }

        public bool CanSeeVMenu_Printers
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.PrinterOperator, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_Admin
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_AdminBrands
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_AdminArticles
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_AdminLocations
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool CanSeeVMenu_AdminUsers
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool CanSeeMainAdminMenu
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Companies_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanAdd
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanEdit
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Companies_CanRename
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanDelete
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanEditLogo
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Companies_CanEditMDSettings
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanEditProductionSettings
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanEditProviders
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Companies_CanEditFTPSettings
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Companies_CanEditRFIDSettings
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Locations_CanSee
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Locations_CanAdd
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Locations_CanEdit
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Locations_CanRename
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Locations_CanDelete
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Locations_CanEditMDSettings
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Printers_CanSee
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Printers_CanAdd
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Printers_CanEdit
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Printers_CanRename
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Printers_CanDelete
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Printers_CanChangeCompany
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Printers_CanSendCommand
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Brands_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Brands_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Brands_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Brands_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Brands_CanDelete
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Brands_CanEditLogo
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Brands_CanEditFTPSettings
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Brands_CanEditRFIDSettings
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Projects_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Projects_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Projects_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Projects_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Projects_CanDelete
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Projects_CanEditLogo
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Projects_CanEditFTPSettings
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Projects_CanEditRFIDSettings
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Projects_CanAddImages
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Projects_CanSeeImages
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign, Roles.CompanyAdmin);
            }
        }
        public bool Admin_Projects_CanEditImages
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }
        public bool Admin_Projects_CanDeleteImages
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Fonts_CanAdd { get => IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign); }
        public bool Admin_Fonts_CanSee { get => IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign); }
        public bool Admin_Fonts_CanEdit { get => IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign); }
        public bool Admin_Fonts_CanDelete { get => IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign); }


        public bool Admin_Packs_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Packs_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Packs_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Packs_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Packs_CanDelete
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Packs_CanEditMDSettings
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Articles_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Articles_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Articles_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Articles_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Articles_CanDelete
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Articles_CanEditMDSettings
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Labels_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Labels_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Labels_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Labels_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Labels_CanDelete
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Catalogs_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Catalogs_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Catalogs_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Catalogs_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Catalogs_CanDelete
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Mappings_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Mappings_CanAdd
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Mappings_CanEdit
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Mappings_CanRename
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Mappings_CanDelete
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTLabelDesign);
            }
        }

        public bool Admin_Users_CanSee
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Users_CanAdd
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Users_CanEdit
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Users_CanRename
        {
            get
            {
                return IsIDTAdminRoles && Principal.IsInRole(Roles.SysAdmin);
            }
        }

        public bool Admin_Users_CanDelete
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Users_CanHiddeUser
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Users_CanCreateWithoutEmail
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool Admin_Users_CanResetPassword
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsAnyRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Users_CanAssignPublicRoles
        {
            get
            {
                return IsIDTAdminRoles || Principal.IsInRole(Roles.CompanyAdmin);
            }
        }

        public bool Admin_Users_CanAssignIDTRoles
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Users_CanAssignSysAdminRole
        {
            get
            {
                return IsSysAdmin;
            }
        }

        public bool CanAssignRole(string roleName)
        {
            var role = Roles.GetRoles().FirstOrDefault(r => r.Name == roleName);
            if(role == null) return false;
            if(role.IsSysAdminRole) return Admin_Users_CanAssignSysAdminRole;
            if(role.IsIDTRole && role.IsAdmin) return Admin_Users_CanAssignIDTRoles;
            return Admin_Users_CanAssignPublicRoles;
        }

        public bool Admin_Materials_CanSee
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Materials_CanAdd
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Materials_CanEdit
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Materials_CanRename
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }

        public bool Admin_Materials_CanDelete
        {
            get
            {
                return IsIDTAdminRoles;
            }
        }


        public bool PrintJobs_CanAssignFactory
        {
            get
            {
                return (IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTProdManager)) || Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager);
            }
        }

        public bool PrintJobs_CanAssignDueDate
        {
            get
            {
                return (IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTProdManager)) || Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager);
            }
        }

        public bool PrintJobs_CanAssignPrinter
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager);
            }
        }

        public bool PrintJobs_CanCancel
        {
            get
            {
                return (IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTProdManager)) || Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager);
            }
        }

        public bool PrintJobs_CanDownloadMDB => IsIDT;



        public bool Admin_Artifact_CanAdd { get => IsSysAdmin || IsDesigner; }
        public bool Admin_Artifact_CanEdit { get => IsSysAdmin || IsDesigner; }
        public bool Admin_Artifact_CanDelete { get => IsSysAdmin || IsDesigner; }

        public bool Admin_Categories_CanSee { get => IsIDT; }
        public bool Admin_Categories_CanAdd { get => IsIDT; }
        public bool Admin_Categories_CanEdit { get => IsIDT; }
        public bool Admin_Categories_CanRename { get => IsIDT; }
        public bool Admin_Categories_CanDelete { get => IsIDT; }

        public bool Can_Change_Provider { get => Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.CompanyAdmin); } // BROKERS Action

        public bool Can_Change_ERPConfiguration
        {
            get
            {
                return IsIDT && Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTProdManager, Roles.IDTCostumerService);
            }
        }


        public bool Can_Orders_Delete
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.ProdManager, Roles.CompanyAdmin);
            }
        }

        public string ManualEntryUrl { get; set; }

        public bool Can_UploadDelivery
        {
            get
            {
                return IsIDT || Principal.IsAnyRole(Roles.DataUpload, Roles.ProdManager, Roles.CompanyAdmin);
            }
        }
    }
}
