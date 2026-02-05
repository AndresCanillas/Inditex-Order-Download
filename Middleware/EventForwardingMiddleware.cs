using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Middleware
{
	public static class EventForwardingMiddleware
	{
		static EventForwardingOptions options;
		static IEventQueue events;
		static IAppLog log;

		/// <summary>
		/// Forwards selected events that occur in the system to connected clients (this applies to events originating from IEventQueue).
		/// </summary>
		/// <param name="app"></param>
		/// <remarks>
		/// In order for event to reach the connected clients you have to allow them to pass thru, this can be done by configuring the
		/// EventForwardingOptions inside your Startup ConfigureServices method, example:
		/// 
		/// services.Configure<EventForwardingOptions>((options) =>
		///	{
		///		options.Allow<MyEvent1>()
		///		.Allow<MyEvent2>();
		///		//... etc
		/// });
		/// </remarks>
		public static void UseEventForwardingMiddleware(this IApplicationBuilder app)
		{
			options = app.ApplicationServices.GetRequiredService<EventForwardingOptions>();
			events = app.ApplicationServices.GetRequiredService<IEventQueue>();
			log = app.ApplicationServices.GetRequiredService<IAppLog>();

			// Middleware sets current ui culture based of the language cookie if available, otherwise language will be the default (en-US)
			app.Use(async (ctx, next) =>
			{
				if (ctx.Request.Path.Value == "/events/listen")
				{
					if (!ctx.User.Identity.IsAuthenticated)
					{
						ctx.Response.StatusCode = 403;
					}
					else if (ctx.WebSockets.IsWebSocketRequest)
					{
						WebSocket socket = await ctx.WebSockets.AcceptWebSocketAsync();
						await ProcessEventNotifications(socket, ctx.User);
					}
					else
					{
						ctx.Response.StatusCode = 400;
					}
				}
				else await next.Invoke();
			});
		}

		private static async Task ProcessEventNotifications(WebSocket socket, ClaimsPrincipal user)
		{
			WebSocketReceiveResult result;
			Action<EQEventInfo> eventHandler = (e) => {
				if (options.IsAllowed(e))
				{
					socket.SendAsync(
						new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e))),
						WebSocketMessageType.Text, true, CancellationToken.None);
				}
			};
			events.OnEventRegistered += eventHandler;
			try
			{
				// NOTE: This channel is not used to receive data from the client, so we simply ignore whatever they decide to send us...
				var buffer = new byte[1024];
				do
				{
					result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
					await Task.Delay(250); // punish misbehavior by delaying next receive (which might cause buffer to become full and TCP forced to enter in congestion control)...
				} while (socket.State == WebSocketState.Open && result.CloseStatus == null);
			}
			catch (Exception ex)
			{
				log.LogException("Error while sending Event Notification to the client.", ex);
			}
			finally
			{
				events.OnEventRegistered -= eventHandler;
			}
		}
	}
}
