using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface IUserRepository
    {
        IAppUser Insert(IAppUser user);
        IAppUser Update(IAppUser data);
        void Delete(string userid);
        IAppUser GetByID(string userid);
        IAppUser GetByName(string name);
        List<IAppUser> GetList();
        List<IAppUser> GetByCompanyID(int companyid);
        List<IAppRole> GetRoles();
        List<string> GetUserRoles(string userid);
        void AddRole(string userid, string role);
        void RemoveRole(string userid, string role);
        void UpdateProfile(ProfileDTO data);
        Task ResetPasswordAsync(string userid);
        Task<IResetToken> GetResetTokenAsync(string id);
        Task DeleteResetTokenAsync(string token);
        List<IAppUser> GetCustomerList(int companyid, bool isIDT);
        List<IAppUser> GetProdManagers(int? byFactoryId, bool IsIDT = true);
        List<IAppUser> GetAdmins();
        Task<string> GetResetURLByUserNameAsync(string id);
        List<IAppUser> GetCustomerServiceUsers();
        List<IAppUser> GetCustomerServiceUsersWithProjects();

        bool ExistUser(string username, int companyId); 
    }
}
