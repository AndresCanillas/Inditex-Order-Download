using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Middleware;
using Newtonsoft.Json.Serialization;
using Print.Middleware;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.LabelService;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintLocal;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;
using WebLink.Contracts;
using WebLink.Contracts.Middleware;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;
using WebLink.Contracts.Services.Ship24;
using WebLink.Services;
using WebLink.Services.Automated;
using WebLink.Services.Ship24;
using WebLink.Services.Zebra;
using WebLink.Services.Zebra.Commands;

namespace WebLink
{
    public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			var factory = Program.Factory;
			RegisterTransientServices(factory);
			RegisterSingletonServices(factory);
			services.AddServices(factory);
			services.AddHttpContextAccessor();
			services.AddIdentity<AppUser, AppRole>()
				.AddEntityFrameworkStores<IdentityDB>();

			services.AddMvc()
			.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
			.AddDataAnnotationsLocalization()
			.AddJsonOptions(options =>
			{
				options.SerializerSettings.ContractResolver = new DefaultContractResolver();
				options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
			})
			.AddRazorPagesOptions(options =>
			{
				options.Conventions.AllowAnonymousToFolder("/Email");
				options.Conventions.AuthorizeFolder("/");
			});
		}


		private void RegisterSingletonServices(IFactory factory)
		{
			factory.Setup<ServicesSetup>(); // registers all repos and other services defined in WebLink.Contracts

			factory.RegisterSingleton<IWSConnectionManager, WSConnectionManager>();
			factory.RegisterSingleton<IZPrinterManager, ZPrinterManager>();
			factory.RegisterTransient<IPrintJob, PrintJob>();
			factory.RegisterTransient<IZPrinter, ZPrinter>();
			factory.RegisterTransient<IWSConnection, WSConnection>();
			factory.RegisterTransient<IZebraRFIDEncoder, ZebraRFIDEncoder>();
			factory.RegisterTransient<IPrintLabelCommand, PrintLabelCommand>();
			factory.RegisterTransient<IEmail, EmailObject>();
			factory.RegisterTransient<EmailObject>();
            factory.RegisterTransient<IShip24ClientService, Ship24ClientService>();

            // Load site languages from languages.json
            RegisterLanguagueOptions(factory);

			// Event forwarding options (for EventForwardingMiddleware)
			factory.RegisterSingleton<EventForwardingOptions>(new EventForwardingOptions()
				.Allow<EntityEvent>()
				.Allow<SageSyncArticleProcessEvent>()
				.Allow<SageSyncItemImportsEndEvent>()
				.Allow<PrinterJobEvent>()
				.Allow<OrderEntityEvent>()
				.Allow<DashboardRefreshEvent>()
			);
		}


		private void RegisterLanguagueOptions(IFactory factory)
		{
			var config = factory.GetInstance<IAppConfig>();
			var languageConfig = config.Bind<LanguageConfig>("Languages");
			var defaultCulture = new CultureInfo(languageConfig.Default.Culture);
			CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
			CultureInfo.DefaultThreadCurrentCulture = defaultCulture;

			var supportedUICultures = new List<CultureInfo>();
			supportedUICultures.Add(defaultCulture);

			foreach (var l in languageConfig.Languages)
			{
				supportedUICultures.Add(new CultureInfo(l.Culture));
			}

			factory.RegisterSingleton<RequestLocalizationOptions>(new RequestLocalizationOptions()
			{
				DefaultRequestCulture = new RequestCulture(CultureInfo.DefaultThreadCurrentUICulture),
				SupportedCultures = new[] { CultureInfo.DefaultThreadCurrentCulture },
				SupportedUICultures = supportedUICultures
			});
		}


		private void RegisterTransientServices(IFactory factory)
		{
			// Register transient objects and services
			factory.RegisterTransient<IPrincipal>(scope =>
			{
				try
				{
					var accessor = scope.GetInstance<IHttpContextAccessor>();
					if (accessor.HttpContext != null)
					{
						if (accessor.HttpContext.User.Identity.IsAuthenticated)
							return accessor.HttpContext.User;
						else if (accessor.HttpContext.Request.Path.Value == "/WebLink")
							return new SystemIdentity();
						else if (accessor.HttpContext.User.Identity.Name == null)
							return new SystemIdentity();
						else
							return null;
					}
					else return new SystemIdentity();
				}
				catch (Exception ex)
				{
					scope.GetInstance<ILogService>().LogException(ex);
					return null;
				}
			});

			factory.RegisterTransient<IUserData>(scope =>
			{
				try
				{
					var accessor = scope.GetInstance<IHttpContextAccessor>();
					var cache = scope.GetInstance<IUserDataCacheService>();
					if (accessor.HttpContext == null || accessor.HttpContext.User.Identity.Name == null)
						return cache.GetDefaultUserData();
					else
						return cache.GetUserData(accessor.HttpContext.User);
				}
				catch (Exception ex)
				{
					scope.GetInstance<ILogService>().LogException(ex);
					return null;
				}
			});

		}


		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			var sp = app.ApplicationServices;
			var factory = sp.GetRequiredService<IFactory>();
			var config = sp.GetRequiredService<IAppConfig>();

			factory.RegisterSingleton<IServiceProvider>(sp);

			// Configure service provider (used by IFactory to create some services)
			var appLifeTime = sp.GetRequiredService<IApplicationLifetime>();
			factory.RegisterSingleton<IApplicationLifetime>(appLifeTime);

			var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
			factory.RegisterSingleton<IHttpContextAccessor>(httpContextAccessor);

			var tempDataProvider = sp.GetRequiredService<ITempDataProvider>();
			factory.RegisterSingleton<ITempDataProvider>(tempDataProvider);

			var dataProtectionProvider = sp.GetRequiredService<IDataProtectionProvider>();
			factory.RegisterSingleton<IDataProtectionProvider>(dataProtectionProvider);

			var userManager = sp.GetRequiredService<UserManager<AppUser>>();
			factory.RegisterSingleton<IPasswordHasher<AppUser>>(userManager.PasswordHasher);


            using(PrintDB PrinterCtx = factory.GetInstance<PrintDB>())
            {
                using(IdentityDB IdentityCtx = factory.GetInstance<IdentityDB>())
                {
                    DBInitialization dbi = new DBInitialization(factory);
                    if(Program.InitDB)
                    {
                        _ = dbi.EnsureDBInitialized(PrinterCtx, IdentityCtx).Result;
                        app.UseDeveloperExceptionPage();
                    }
                    _ = dbi.SeedDB(PrinterCtx).Result;
                }
            }


			app.UseWebSockets(new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
				ReceiveBufferSize = 4 * 1024
			});

			app.UseEventSyncAsServer("PrintLocal", (events) =>
			{
				// Subscribe to any events that are of interest. IMPORTANT: This subscribes to events generated by the remote system (PrintLocal in this case).
				events.Subscribe<PLOrderStatusChangeEvent, ProcessOrderStatusChange>();
				events.Subscribe<PLArticleStatusChangeEvent, ProcessArticleStatusChange>();
				events.Subscribe<PLUnitProgressChangeEvent, ProcessUnitProgressChange>();
				events.Subscribe<SageFileDropAckEvent, PerformOrderBilling>();
				//events.Subscribe<NotifyOrdersSyncEvent, VerifyOrdersNotSync>(); Not implemented yet
				events.Subscribe<OrderFileReceivedEvent, HandleOrderReceivedEvent>();
				events.Subscribe<DuplicatedEPCEvent, SendDuplicatedEPCEmail>();
				events.Subscribe<SageFileDropEvent, SendSageFileDropEvent>();
				events.Subscribe<EntityEvent, HandleEntityEvent>();
				events.Subscribe<PrintPackageReadyEvent, HandlePrintPackageReadyEvent>();
                events.Subscribe<OrderStoppedEvent, ForwardingStoppedEvent>();
			});

			app.UseExceptionHandlerMiddleware();
			app.UseStatusCodePages();
			app.UseMiddleware<SubdomainRedirectMiddleware>();
			app.UsePortRedirectionMiddleware();
			app.UseStaticFiles();
			app.UseMiddleware<BearerTokenAuthentication>();
			app.UseMiddleware<AuthenticationMiddleware>();
			//app.UseAuthentication();

			var options = sp.GetRequiredService<RequestLocalizationOptions>();
			app.UseRequestLocalization(options);

			app.UseLanguageCookieMiddleware();
			app.UseMiddleware<ConfigMetadataMiddleware>();

			app.UseMvc(routes =>
			{
				routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");
			});

			var blsc = sp.GetRequiredService<IBLabelServiceClient>();
			blsc.MaxRetries = 10;
            if (config.GetValue<int>("LabelService.Version", 1) == 2)
            {
                blsc.SetEndPoints(config.Bind<List<LSEP>>("LabelService.EndPoints"));
            }else
            {// old version
                blsc.SetEndPoints(config.Bind<List<string>>("LabelService.EndPoints"));
            }

            var context = factory.GetInstance<IConfigurationContext>();
            context.RegisterSystem<RFIDConfigurationSystem>();

            // Ensure automated process manager is initialized as soon as possible
            var apm = sp.GetRequiredService<IAutomatedProcessManager>();
			apm.Setup<SetupPrintCentralProcesses>();
			apm.Start();

			// Ensure catalog plugin manager is initialized as soon as possible
			var pluginManager = sp.GetRequiredService<ICatalogPluginManager>();
			pluginManager.Start();

			// Ensure printer connection manager is initialized as soon as possible
			var connectionManager = sp.GetRequiredService<IWSConnectionManager>();

			appLifeTime.ApplicationStopping.Register(() =>
			{
				apm.Stop();
			});
		}
	}
}
