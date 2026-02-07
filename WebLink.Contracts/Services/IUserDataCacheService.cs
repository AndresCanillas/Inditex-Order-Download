using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebLink.Contracts
{
    public interface IUserDataCacheService
    {
        IUserData GetByUserName(string userName);
        IUserData GetUserData(ClaimsPrincipal principal);
        IUserData GetDefaultUserData();
        void Update(IUserData data);
        void Remove(string userName);
    }

    public interface IUserData : IUserFeatures
    {
        ClaimsPrincipal Principal { get; set; }
        string Id { get; set; }
        string UserName { get; set; }
        int CompanyID { get; set; }
        int LocationID { get; set; }
        int SelectedCompanyID { get; set; }
        int SelectedBrandID { get; set; }
        int SelectedProjectID { get; set; }
        IEnumerable<string> UserRoles { get; }
        bool IsPublicRoles { get; }
        bool IsIDT { get; }
        bool IsIDTAdminRoles { get; }
        bool IsSysAdmin { get; }
        bool IsIDTExternal { get; }
        bool IsLockoutEnabled { get; }
        DateTime? LockoutEnd { get; }

        bool IsCompositionChecker { get; } 

    }
}
