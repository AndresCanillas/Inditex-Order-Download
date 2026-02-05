using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderDownloadWebApi.Models.Repositories;
using OrderDownloadWebApi.Services;
using Service.Contracts;
using System;
using System.Linq;

namespace OrderDownloadWebApi.Controllers
{
    public class AccountController : Controller
    {
        private IUserRepository repo;
        private RequestLocalizationOptions options;
        private ITokenService tokenService;
        private IAppConfig config;

        public AccountController(
            IUserRepository repo,
            RequestLocalizationOptions options,
            ITokenService tokenService,
            IAppConfig config
            )
        {
            this.repo = repo;
            this.options = options;
            this.tokenService = tokenService;
            this.config = config;
        }

        public RedirectResult Logout(string returnUrl = "/")
        {
            if(Request.Cookies.TryGetValue(config.GetValue<string>("DownloadServicesWeb.bearerTokenName", "bearerToken"), out var token))
                tokenService.RemoveToken(token);
            return Redirect(returnUrl);
        }

        [Route("/Account/Language/{lang}")]
        public RedirectResult Language(string lang)
        {
            var culture = options.SupportedUICultures.Where(c => c.Name == lang).FirstOrDefault();
            if(culture != null)
            {
                Response.Cookies.Append("language", lang, new CookieOptions() { MaxAge = TimeSpan.FromDays(8) });
                var user = repo.GetByName(User.Identity.Name);
                user.Language = lang;
                repo.Update(user);
            }
            return Redirect("/");
        }
    }
}