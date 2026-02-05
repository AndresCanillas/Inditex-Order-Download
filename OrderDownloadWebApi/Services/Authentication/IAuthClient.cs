using Service.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderDownloadWebApi.Services.Authentication
{
    public interface IAuthClient
    {
        string Url { get; set; }
        string Token { get; }
        void Login(string loginUrl, string userName, string password);
        Task LoginAsync(string loginUrl, string userName, string password);
        IAppUser GetUserData(string userName);
        Task<IAppUser> GetUserDataAsync(string userName);
    }

    public interface IAppUser
    {
        string Id { get; }
        string UserName { get; }
        int? CompanyID { get; }
        int? LocationID { get; }
        string FirstName { get; }
        string LastName { get; }
        string Email { get; }
        string PhoneNumber { get; }
        string Language { get; }
        bool ShowAsUser { get; }
        List<string> Roles { get; }
    }

    public class AppUser : IAppUser
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public int? CompanyID { get; set; }
        public int? LocationID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Language { get; set; }
        public bool ShowAsUser { get; set; }
        public List<string> Roles { get; set; }
    }

    public class AuthClient : BaseServiceClient, IAuthClient
    {
        public AuthClient(IAppConfig cfg)
        {
            Url = cfg["DownloadServicesWeb.PrintCentralUrl"];
        }

        public IAppUser GetUserData(string userName)
        {
            var user = Get<AppUser>($"users/getbyname/{userName}");
            user.Roles = Get<List<string>>($"users/getuserroles/{user.Id}");
            return user;
        }

        public async Task<IAppUser> GetUserDataAsync(string userName)
        {
            var user = await GetAsync<AppUser>($"users/getbyname/{userName}");
            user.Roles = await GetAsync<List<string>>($"users/getuserroles/{user.Id}");
            return user;
        }
    }
}
