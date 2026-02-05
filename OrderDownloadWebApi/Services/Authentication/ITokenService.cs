using OrderDownloadWebApi.Models;
using OrderDownloadWebApi.Models.Repositories;
using OrderDownloadWebApi.Services.Authentication;
using Service.Contracts.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;

namespace OrderDownloadWebApi.Services
{
    public interface ITokenService
    {
        string RegisterPrincipal(ClaimsPrincipal principal);
        bool ValidateToken(string token, out ClaimsPrincipal principal);
        void RemoveToken(string token);
    }

    class TokenService : ITokenService
    {
        private ConcurrentDictionary<string, TokenInfo> tokens;
        private RNGCryptoServiceProvider rnd;
        private Timer timer;
        private IDBConfiguration db;

        public TokenService(IDBConfiguration db)
        {
            this.db = db;
            tokens = new ConcurrentDictionary<string, TokenInfo>();
            rnd = new RNGCryptoServiceProvider();
            timer = new Timer(expireTokens, null, (int)TimeSpan.FromMinutes(1).TotalMilliseconds, Timeout.Infinite);
            db.Configure("LocalDB");
            db.EnsureCreated();
            using(var conn = db.CreateConnection())
            {
                UpdateDBObjects(conn);
            };

            using(var conn = db.CreateConnection())
            {
                var list = conn.Select<LoginToken>("select * from LoginTokens");
                foreach(var token in list)
                {
                    var user = conn.SelectOne<User>("select * from Users where ID = @id", token.UserID);
                    if(user != null)
                        HydrateToken(user, token);
                }
            }
        }

        private void HydrateToken(User user, LoginToken token)
        {
            ClaimsPrincipal principal = new UserPrincipal(new UserIdentity(token.UserID, new AppUser()
            {
                Id = user.UserId,
                UserName = user.Name,
                CompanyID = 1,
                LocationID = 1,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Language = user.Language,
                Roles = new List<string>(user.Roles.Split(',')),
                ShowAsUser = true
            }));
            tokens.TryAdd(token.Token, new TokenInfo(principal));
        }

        private void expireTokens(object state)
        {
            try
            {
                TokenInfo principal;
                List<string> expiredTokens = new List<string>();
                foreach(string key in tokens.Keys)
                {
                    if(tokens.TryGetValue(key, out principal))
                    {
                        if(principal.Date.Add(TimeSpan.FromHours(12)) <= DateTime.Now)
                            expiredTokens.Add(key);
                    }
                }

                foreach(string key in expiredTokens)
                    tokens.TryRemove(key, out principal);

                using(var conn = db.CreateConnection())
                {
                    conn.ExecuteNonQuery("delete from LoginTokens where Expires < @date ", DateTime.Now);
                }
            }
            catch { }
            finally
            {
                timer.Change((int)TimeSpan.FromMinutes(1).TotalMilliseconds, Timeout.Infinite);
            }
        }

        public string RegisterPrincipal(ClaimsPrincipal principal)
        {
            var tokenInfo = new TokenInfo(principal);
            string token;
            byte[] tokenBytes = new byte[64];
            do
            {
                rnd.GetBytes(tokenBytes);
                token = Convert.ToBase64String(tokenBytes);
            } while(!tokens.TryAdd(token, tokenInfo));

            // Register token in DB
            int userid = (principal as UserPrincipal).User.ID;
            using(var conn = db.CreateConnection())
            {
                conn.ExecuteNonQuery("insert into LoginTokens values(@token, @userid, @expires)",
                    token, userid, DateTime.Now.AddHours(8));
            }

            return token;
        }

        public bool ValidateToken(string token, out ClaimsPrincipal principal)
        {
            TokenInfo info;
            principal = null;
            if(String.IsNullOrWhiteSpace(token))
            {
                return false;
            }
            if(tokens.TryGetValue(token, out info))
            {
                if(info.Date.Add(TimeSpan.FromHours(3)) > DateTime.Now)
                {
                    info.Renew(); // Renew session token to give it another 15 minutes of life time
                    principal = info.Principal;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void RemoveToken(string token)
        {
            tokens.TryRemove(token, out _);
        }

        private void UpdateDBObjects(IDBX conn)
        {
            conn.ExecuteNonQuery(@"
				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LoginTokens]') AND type in (N'U'))
				BEGIN
					CREATE TABLE [dbo].[LoginTokens](
						[Token] [nvarchar](100) NOT NULL,
						[UserID] [int] NOT NULL,
						[Expires] [datetime] NOT NULL
					)
				END
				");
        }
    }

    public class TokenInfo
    {
        private DateTime date = DateTime.Now;
        private ClaimsPrincipal principal;
        public DateTime Date { get { return date; } }
        public ClaimsPrincipal Principal { get { return principal; } }

        public TokenInfo(ClaimsPrincipal principal)
        {
            this.principal = principal;
        }

        public void Renew()
        {
            date = DateTime.Now;
        }
    }

    public class LoginToken
    {
        public string Token;
        public int UserID;
        public DateTime Expires;
    }
}
