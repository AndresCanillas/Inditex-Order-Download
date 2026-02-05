using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Service.Contracts;

namespace Print.Middleware
{
	public class SubdomainRedirectMiddleware
	{
		private readonly object syncObj = new object();
		private readonly RequestDelegate next;
		private volatile bool realodConfig;
		private RedirectConfig redirectConfig;
		private IAppConfig config;
		private IAppLog log;


		public SubdomainRedirectMiddleware(RequestDelegate next, IAppConfig config, IAppLog log)
		{
			this.next = next;
			this.config = config;
			this.log = log.GetSection("Redirects");
			redirectConfig = config.Bind<RedirectConfig>("Environments", updateConfig);
		}


		private void updateConfig()
		{
			realodConfig = true;
		}


		public async Task Invoke(HttpContext context)
		{
			if (realodConfig) ReloadCfg();
			if (redirectConfig.Enabled)
			{
				var redirects = redirectConfig.Redirects;
				var logMsg = redirectConfig.LogRequests;
				if (logMsg) log.LogMessage($"Received request to: {context.Request.Host.Host}");
				string[] tokens = context.Request.Host.Host.Split('.');
				if (tokens.Length >= 3)
				{
					var redirect = SearchRedirect(redirects, tokens);
					if (redirect != null)
					{
						if (logMsg) log.LogMessage($"Redirecting from {context.Request.Host.Host} to {redirect.TargetUrl}");
						context.Response.Redirect(redirect.TargetUrl, false);
						return;
					}
				}
				if (logMsg) log.LogMessage($"Letting request proceed normally...");
			}
			await next(context);
		}


		private RedirectInfo SearchRedirect(List<RedirectInfo> redirects, string[] urlComponents)
		{
			StringBuilder sb = new StringBuilder(50);
			for (int i = 0; i < urlComponents.Length - 2; i++)
				sb.Append(urlComponents[i]).Append('.');
			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
				var subdomain = sb.ToString();
				var info = redirects.FirstOrDefault(p => String.Compare(p.Subdomain, subdomain, true) == 0);
				return info;
			}
			return null;
		}


		private void ReloadCfg()
		{
			lock (syncObj)
			{
				if (realodConfig) // NOTE: Intended double lock check
				{
					redirectConfig = config.Bind<RedirectConfig>("Environments");
					realodConfig = false;
				}
			}
		}
	}


	public class RedirectConfig
	{
		public bool Enabled = false;
		public bool LogRequests = false;
		public List<RedirectInfo> Redirects = new List<RedirectInfo>();
	}


	public class RedirectInfo
	{
		public string Subdomain;
		public string TargetUrl;
	}
}