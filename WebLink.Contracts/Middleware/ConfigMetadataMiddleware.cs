using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Service.Contracts;


namespace WebLink.Contracts.Middleware
{
	public class ConfigMetadataMiddleware
	{
		private readonly IMetadataStore meta;
		private readonly RequestDelegate next;

		public ConfigMetadataMiddleware(RequestDelegate next, IMetadataStore meta)
		{
			this.next = next;
			this.meta = meta;
		}

		public async Task Invoke(HttpContext context)
		{
			if (context.Request.Path.ToString().StartsWith("/meta"))
			{
				if (context.Request.Method == "GET")
				{
					string result;
					if (meta.TryGetValue(context.Request.Path, out result))
					{
						context.Response.StatusCode = 200;
						context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
						await context.Response.WriteAsync(result);
					}
					else context.Response.StatusCode = 404;
				}
				else context.Response.StatusCode = 404;
			}
			else await next(context);
		}
	}
}
