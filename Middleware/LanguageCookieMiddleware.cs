using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Print.Middleware
{
	public static class LanguageCookieMiddleware
	{
		static RequestLocalizationOptions options;

		public static void UseLanguageCookieMiddleware(this IApplicationBuilder app)
		{
			options = app.ApplicationServices.GetRequiredService<RequestLocalizationOptions>();
			// Middleware sets current ui culture based of the language cookie if available, otherwise language will be the default (en-US)
			app.Use(async (ctx, next) =>
			{
				string lang;
				if (ctx.Request.Path.StartsWithSegments("/lang"))
				{
					lang = ctx.Request.Query["id"];
					var culture = options.SupportedUICultures.Where(c => c.Name == lang).FirstOrDefault();
					if (culture != null)
						ctx.Response.Cookies.Append("language", lang, new CookieOptions() { MaxAge = TimeSpan.FromDays(8) });
					ctx.Response.Redirect("/");
				}
				else if (ctx.Request.Cookies.TryGetValue("language", out lang))
				{
					CultureInfo.CurrentUICulture = new CultureInfo(lang);
					await next.Invoke();
				}
				else await next.Invoke();
			});
		}
	}
}
