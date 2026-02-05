using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Print.Middleware
{
	public static class EventSyncMiddlewareExtensions
	{
		public static void UseEventSyncAsServer(this IApplicationBuilder app, string routeName, Action<IEventSyncClient> setup)
		{
			var factory = app.ApplicationServices.GetRequiredService<IFactory>();

			// Load EventSyncService configuration. IMPORTANT: this configuration must be present in the appsettings.json or else an exception is thrown.
			var config = factory.GetInstance<IAppConfig>();
			var appInfo = factory.GetInstance<IAppInfo>();
			var cfg = config.Bind<EventSyncConfig>("EventSyncService");

			// Configure IEventSyncStore service, this service provides persistance for events that might be of interest to remote clients.
			var eventStore = factory.GetInstance<IEventSyncStore>();
			eventStore.Configure(appInfo.AppName + "_EventSyncService", cfg.EventStore.Provider, cfg.EventStore.ConnStr);

			// Locate specified route within the EventSyncService configuration
			var route = cfg.Routes.FirstOrDefault(r => r.Name == routeName);
			if (route == null)
				throw new InvalidOperationException($"Route {routeName} is not defined in the EventSyncService configuration. Review your appsettings.json");

			// Setup middleware to process websocket requests directed to the specified route.
			// WebSockets connecting to the specified route are assumed to be using the protocol defined by 
			// IEventSyncClient, i.e., on both sides of the connection there must be an instance of IEventSyncClient.
			app.Use(async (context, next) =>
			{
				if (context.WebSockets.IsWebSocketRequest && context.Request.Path.Value == route.Route)
				{
					using (var socket = await context.WebSockets.AcceptWebSocketAsync())
					{
						using (var client = factory.GetInstance<IEventSyncClient>())
						{
							client.Configure(eventStore, cfg.Secret);
							setup(client);
							var ip = context.Connection.RemoteIpAddress.ToString();
							await client.Accept(socket, ip);
						}
					}
				}
				else await next.Invoke();
			});
		}

		public static void UseEventSyncAsClient(this IApplicationBuilder app, string routeName, Action<IEventSyncClient> setup)
		{
			var factory = app.ApplicationServices.GetRequiredService<IFactory>();

			// Load EventSyncService configuration. IMPORTANT: this configuration must be present in the appsettings.json or else an exception is thrown.
			var config = factory.GetInstance<IAppConfig>();
			var appInfo = factory.GetInstance<IAppInfo>();
			var cfg = config.Bind<EventSyncConfig>("EventSyncService");

			// Configure IEventSyncStore service, this service provides persistance for events that might be of interest to remote clients.
			var eventStore = factory.GetInstance<IEventSyncStore>();
			eventStore.Configure(appInfo.AppName + "_EventSyncService", cfg.EventStore.Provider, cfg.EventStore.ConnStr);

			// Locate specified route within the EventSyncService configuration
			var route = cfg.Routes.FirstOrDefault(r => r.Name == routeName);
			if (route == null)
				throw new InvalidOperationException($"Route {routeName} is not defined in the EventSyncService configuration. Review your appsettings.json");

			// Setup the EventSyncClient
			var client = factory.GetInstance<IEventSyncClient>();
			client.Configure(eventStore, cfg.Secret);
			setup(client);
			client.Connect(route.Url);

			// Do some cleanup when web application is stopped
			var appLifeTime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();
			appLifeTime.ApplicationStopping.Register(() =>
			{
				client.Dispose();
			});
		}
	}


	public class EventSyncConfig
	{
		public string Secret;						// Shared secret between the two parties communicating, this is validated when a new session is being stablished.
		public EventSyncStoreConfig EventStore;		// Configuration used for the EventSyncStore service (which provides persistance for event data)
		public List<EventSyncRoute> Routes;
	}

	public class EventSyncStoreConfig
	{
		public string Provider;		// Database connection information required by the EvenSyncStore service
		public string ConnStr;
	}

	public class EventSyncRoute
	{
		public string Name;         // Name of the route. This is specified when calling UseEventSyncAsServer or UseEventSyncAsClient
		public string Route;        // The route (or request path), used to determine if the middleware should handle the request or not. This is Requered only when the system behaves as server (calling UseEventSyncAsServer)
		public string Url;          // Url to the remote server, this is required only when the system will behave as client (calling UseEventSyncAsClient)
	}
}
