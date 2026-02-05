using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Print.Middleware
{
	public static class ExceptionMiddlewareExtensions
	{
		public static void UseExceptionHandlerMiddleware(this IApplicationBuilder app)
		{
			app.UseExceptionHandler(e =>
			{
				e.Run(async context =>
				{
					try
					{
						context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
						context.Response.ContentType = "application/json";

						var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
						if (contextFeature != null)
						{
							var log = app.ApplicationServices.GetRequiredService<IAppLog>();
							log.LogException($"Internal Server Error", contextFeature.Error);
							await context.Response.WriteAsync(new
							{
								StatusCode = context.Response.StatusCode,
								Message = "Internal Server Error."
							}.ToString());
						}
					}
					catch { } // NOTE: Empty catch is intended. We are already catching an exception, if anything inside this try/catch fails, it is probably not recoverable anyway.
				});
			});
		}
	}
}
