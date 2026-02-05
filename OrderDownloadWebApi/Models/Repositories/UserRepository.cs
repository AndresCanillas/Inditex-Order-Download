using Microsoft.EntityFrameworkCore;
using OrderDownloadWebApi.Services.Authentication;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace OrderDownloadWebApi.Models.Repositories
{
    public interface IUserRepository
    {
        /// <summary>
        /// Authenticates the given user and password. If the authentication process is successful it returns a token
        /// that can be used to setup a cookie, such that subsecuent requests comming from the same client do not need
        /// to be authenticated any more.
        /// </summary>
        /// <param name="userName">The username</param>
        /// <param name="password">The password</param>
        /// <returns>
        /// Returns an object representing the result of the authentication attempt.
        /// </returns>
        Task<AuthResult> AuthenticateAsync(string userName, string password);
        /// <summary>
        /// Gets a list of all registered users
        /// </summary>
        List<User> GetList();
        /// <summary>
        /// Gets a list of all registered users that match the specified filter(s)
        /// </summary>
        List<User> GetList(UserFilter filter);
        /// <summary>
        /// Gets the user record that corresponds to the given id. If no match is found, then null is returned.
        /// </summary>
        /// <param name="id">The id of the user</param>
        User GetByID(int id);
        /// <summary>
        /// Gets the user record that corresponds to the given user name. If no match is found, then null is returned.
        /// </summary>
        /// <param name="userName">The name of the user</param>
        User GetByName(string userName);
        /// <summary>
        /// Creates a new instance of a user
        /// </summary>
        User Create();
        /// <summary>
        /// Inserts or updates the given user
        /// </summary>
        void Insert(User entity);
        /// <summary>
        /// Inserts or updates the given user
        /// </summary>
        void Update(User entity);
        /// <summary>
        /// Deletes the given user
        /// </summary>
        void Delete(User entity);
        void Save(User entity);
    }

    public class UserRepository : IUserRepository
    {
        private IFactory factory;
        private IAuthClient client;
        private IAppLog log;

        public UserRepository(IFactory factory, IAuthClient client, IAppLog log)
        {
            this.factory = factory;
            this.client = client;
            this.log = log.GetSection("Authentication");
        }

        public async Task<AuthResult> AuthenticateAsync(string userName, string password)
        {
            AuthResult result = null;
            try
            {
                using(var ctx = factory.GetInstance<LocalDB>())
                {
                    var user = await ctx.Users.Where(usr => usr.Name == userName).FirstOrDefaultAsync();
                    if(user == null)
                        result = await AuthenticateNewUserAsync(ctx, userName, password);
                    else
                        result = await AuthenticateExistingUserAsync(ctx, user, userName, password);
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
            }
            finally
            {
                if(result == null)
                    result = new AuthResult() { Success = false };
                if(result.Success)
                    log.LogMessage($"Authentication attempt for user {userName} succeded.");
                else
                    log.LogMessage($"Authentication attempt for user {userName} failed.");
            }
            return result;
        }

        private async Task<AuthResult> AuthenticateNewUserAsync(LocalDB ctx, string userName, string password)
        {
            var user = new User();
            user.CreatedDate = DateTime.Now;
            ctx.Users.Add(user);
            var authResult = await AuthenticateWithPrintCentralAsync(ctx, user, userName, password);
            return authResult;
        }

        private async Task<AuthResult> AuthenticateExistingUserAsync(LocalDB ctx, User user, string userName, string password)
        {
            if(user.UpdatedDate.AddDays(1) < DateTime.Now)
                return await AuthenticateWithPrintCentralAsync(ctx, user, userName, password);
            else
                return AuthenticateWithLocalCache(user, userName, password);
        }

        private async Task<AuthResult> AuthenticateWithPrintCentralAsync(LocalDB ctx, User user, string userName, string password)
        {
            try
            {
                await client.LoginAsync("/", userName, password);
                var userData = await client.GetUserDataAsync(userName);
                if(userData.CompanyID == 1)
                {
                    user.UserId = userData.Id;
                    user.Name = userData.UserName;
                    user.FirstName = userData.FirstName;
                    user.LastName = userData.LastName;
                    user.Email = userData.Email;
                    user.PhoneNumber = userData.PhoneNumber;
                    user.Language = userData.Language;
                    user.Roles = userData.Roles.Merge(",");
                    user.UpdatedDate = DateTime.Now;
                    user.PwdHash = HashFunctions.SHA512($"{userName.ToLower()}:{password}");
                    await ctx.SaveChangesAsync();
                    var principal = new UserPrincipal(new UserIdentity(user.ID, userData));
                    return new AuthResult()
                    {
                        Success = true,
                        Principal = principal
                    };
                }
                else
                {
                    // Deny access to external users (users that are not from Smartdots)
                    return new AuthResult() { Success = false };
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return AuthenticateWithLocalCache(user, userName, password);
            }
        }

        private AuthResult AuthenticateWithLocalCache(User user, string userName, string password)
        {
            log.LogMessage("Attempting login with cached data...");
            if(user == null)
                throw new InvalidOperationException("Cannot procceed, user is null");
            if(userName == null)
                throw new InvalidOperationException("Cannot procceed, userName is null");
            if(password == null)
                throw new InvalidOperationException("Cannot procceed, password is null");
            if(user.Roles == null)
                user.Roles = "";
            var hash = HashFunctions.SHA512($"{userName.ToLower()}:{password}");
            if(user.PwdHash == hash)
            {
                var principal = new UserPrincipal(
                    new UserIdentity(
                        user.ID,
                        new AppUser()
                        {
                            Id = user.UserId,
                            UserName = userName,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email,
                            PhoneNumber = user.PhoneNumber,
                            Language = user.Language,
                            Roles = new List<string>(user.Roles.Split(','))
                        })
                );
                return new AuthResult()
                {
                    Success = true,
                    Principal = principal
                };
            }
            else return new AuthResult() { Success = false };
        }

        public List<User> GetList()
        {
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                return (from usr in ctx.Users select usr).AsNoTracking().ToList();
            }
        }

        public List<User> GetList(UserFilter filter)
        {
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                return
                (from usr in ctx.Users
                 where
                    (String.IsNullOrWhiteSpace(filter.UserName) || usr.Name.Contains(filter.UserName))
                 select usr
                ).AsNoTracking().ToList();
            }
        }

        public User GetByID(int id)
        {
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                return ctx.Users.Where(usr => usr.ID == id).FirstOrDefault();
            }
        }

        public User GetByName(string userName)
        {
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                return ctx.Users.Where(usr => usr.Name == userName).FirstOrDefault();
            }
        }

        public User Create()
        {
            return new User()
            {
                IsNew = true,
                CreatedDate = DateTime.Now
            };

        }

        public void Insert(User entity)
        {
            if(entity == null)
                throw new ArgumentNullException(nameof(entity));
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                entity.UpdatedDate = DateTime.Now;
                ctx.Users.Add(entity);
                ctx.SaveChanges();
                entity.IsNew = false;
            }
        }

        public void Update(User entity)
        {
            if(entity == null)
                throw new ArgumentNullException(nameof(entity));
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                entity.UpdatedDate = DateTime.Now;
                ctx.Users.Update(entity);
                ctx.SaveChanges();
            }
        }

        public void Delete(User entity)
        {
            if(entity == null)
                throw new ArgumentNullException(nameof(entity));
            using(var ctx = factory.GetInstance<LocalDB>())
            {
                ctx.Users.Remove(entity);
            }
        }

        public void Save(User entity)
        {
            if(entity.IsNew)
                Insert(entity);
            else
                Update(entity);
        }
    }

    public class UserIdentity : IIdentity
    {
        public UserIdentity(int id, IAppUser user)
        {
            ID = id;
            UserData = user;
            Name = user.UserName;
            IsAuthenticated = true;
            Roles = user.Roles;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public string AuthenticationType { get => "Basic"; }
        public bool IsAuthenticated { get; set; }
        public List<string> Roles { get; set; }
        public IAppUser UserData { get; set; }
    }

    public class UserPrincipal : ClaimsPrincipal
    {
        private readonly UserIdentity user;
        private ClaimsIdentity identity;
        public UserPrincipal(UserIdentity user)
        {
            identity = new ClaimsIdentity(user);
            this.user = user;
            this.AddIdentity(identity);
            identity.AddClaim(new Claim("Language", user.UserData.Language ?? "en-US"));
        }

        public UserIdentity User { get => user; }

        public override IIdentity Identity { get => identity; }

        public override bool IsInRole(string role)
        {
            return user.Roles.Contains(role);
        }
    }

    public class UserFilter
    {
        public string UserName { get; set; }
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public bool MustChangePassword { get; set; }
        public ClaimsPrincipal Principal { get; set; }
    }
}
