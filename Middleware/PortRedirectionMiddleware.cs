using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Print.Middleware
{
    public static class PortRedirectionMiddleware
	{
		public static void UsePortRedirectionMiddleware(this IApplicationBuilder app)
		{
			var config = app.ApplicationServices.GetRequiredService<IAppConfig>();
			var endpoints = config.Bind<List<ServerEndPoint>>("EndPoints");
			// Redirects non-HTTPS requests
			app.Use(async (ctx, next) =>
			{
				var rqPort = ctx.Request.Host.Port ?? (ctx.Request.IsHttps ? 443 : 80);
				var ep = endpoints.First(p => p.Port == rqPort);
				if (ep.Redirect)
				{
					if (String.IsNullOrWhiteSpace(ep.RedirectPort))
						ctx.Response.Redirect($"{ep.RedirectProtocol}{ctx.Request.Host.Host}{ctx.Request.Path}", false);
					else
						ctx.Response.Redirect($"{ep.RedirectProtocol}{ctx.Request.Host.Host}:{ep.RedirectPort}{ctx.Request.Path}", false);
				}
				else await next.Invoke();
			});
		}
	}
}
