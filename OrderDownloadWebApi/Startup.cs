using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Middleware;
using Newtonsoft.Json.Serialization;
using OrderDonwLoadService;
using OrderDonwLoadService.Services;
using OrderDownloadWebApi.Models;
using OrderDownloadWebApi.Models.Repositories;
using OrderDownloadWebApi.Processing;
using OrderDownloadWebApi.Services;
using OrderDownloadWebApi.Services.Authentication;
using Print.Middleware;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;

namespace OrderDownloadWebApi
{
    public class Startup
    {
        public Startup()
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-ES");

            var factory = Program.Factory;
            RegisterLanguageOptions(factory);
            RegisterLocalDB(factory);
            RegisterSingletonServices(factory);
            RegisterTransientServices(factory);

            services.AddServices(factory);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMvc()

            .AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
            });
        }
        private void RegisterLocalDB(IFactory factory)
        {
            var config = factory.GetInstance<IAppConfig>();

            var localDBConnStr = config.GetValue<string>("Databases.LocalDB.ConnStr");
            var localDBOptionsBuilder = new DbContextOptionsBuilder<LocalDB>();

#if DEBUG
            localDBOptionsBuilder.UseLoggerFactory(DbCommandConsoleLoggerFactory);
#endif

            localDBOptionsBuilder.UseSqlServer(localDBConnStr,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure();
                    sqlOptions.CommandTimeout(120);
                });
            factory.RegisterSingleton<DbContextOptions<LocalDB>>(localDBOptionsBuilder.Options);

            // Configure database context
            factory.RegisterTransient<LocalDB>(scope =>
            {
                var options = scope.GetInstance<DbContextOptions<LocalDB>>();
                return new LocalDB(options);
            });
        }
        private void RegisterLanguageOptions(IFactory factory)
        {
            var config = factory.GetInstance<IAppConfig>();

            // Load site languages from AppConfig
            var languageConfig = config.Bind<LanguageConfig>("Languages");
            var supportedUICultures = new List<CultureInfo>();
            var defaultCulture = new CultureInfo(languageConfig.Default.Culture);
            CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
            supportedUICultures.Add(defaultCulture);
            foreach(var l in languageConfig.Languages)
                supportedUICultures.Add(new CultureInfo(l.Culture));

            factory.RegisterSingleton<RequestLocalizationOptions>(new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new RequestCulture(CultureInfo.DefaultThreadCurrentUICulture),
                SupportedCultures = new[] { CultureInfo.DefaultThreadCurrentCulture },
                SupportedUICultures = supportedUICultures
            });
        }


        private void RegisterSingletonServices(IFactory factory)
        {
            factory.RegisterSingleton<Services.ITokenService, Services.TokenService>();

            factory.RegisterSingleton<EventForwardingOptions>(new EventForwardingOptions()
            //.Allow<AccessGeneratedEvent>()
            );
        }


        private void RegisterTransientServices(IFactory factory)
        {
            factory.RegisterTransient<IPrincipal>(scope =>
            {
                try
                {
                    var accessor = scope.GetInstance<IHttpContextAccessor>();
                    if(accessor.HttpContext != null)
                    {
                        if(accessor.HttpContext.User.Identity.IsAuthenticated)
                            return accessor.HttpContext.User;
                        else if(accessor.HttpContext.User.Identity.Name == null)
                            return new SystemIdentity();
                        else
                            return null;
                    }
                    else return new SystemIdentity();
                }
                catch(Exception ex)
                {
                    scope.GetInstance<IAppLog>().LogException(ex);
                    return null;
                }
            });

            factory.RegisterSingleton<IUserRepository, UserRepository>();
            factory.RegisterTransient<IAuthClient, AuthClient>();
            factory.RegisterTransient<Services.IPrintCentralClient, CentralClient>();
        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var sp = app.ApplicationServices;
            var factory = Program.Factory;
            var config = factory.GetInstance<IAppConfig>();

            factory.RegisterSingleton<IServiceProvider>(sp);

            if(Program.InitDB)
            {
                using(LocalDB ctx = factory.GetInstance<LocalDB>())
                {
                    LocalDBInitialization dbi = new LocalDBInitialization(factory);
                    dbi.EnsureDBInitialized(ctx);
                    app.UseDeveloperExceptionPage();
                }
            }

            var appLifeTime = sp.GetRequiredService<IApplicationLifetime>();
            factory.RegisterSingleton<IApplicationLifetime>(appLifeTime);

            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            factory.RegisterSingleton<IHttpContextAccessor>(httpContextAccessor);

            var tempDataProvider = sp.GetRequiredService<ITempDataProvider>();
            factory.RegisterSingleton<ITempDataProvider>(tempDataProvider);

            app.UseExceptionHandlerMiddleware();
            app.UseStatusCodePages();
            app.UsePortRedirectionMiddleware();
            app.UseStaticFiles();
            app.UsePrintAuthentication();
            app.UseWebSockets(new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            });

            // Forwards selected events that occur in the system to connected clients (browsers)...
            app.UseEventForwardingMiddleware();

            // Subscribe to any events of interest that are genereated by the remote system
            app.UseEventSyncAsClient("PrintCentral", (events) =>
            {
                events.Subscribe<SmartdotsUserChangedEvent, ProcessUserChange>();
            });

            var options = factory.GetInstance<RequestLocalizationOptions>();
            app.UseRequestLocalization(options);
            app.UseLanguageCookieMiddleware();
            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");
            });


            var log = factory.GetInstance<IAppLog>();

            // Ensure automated process manager is initialized
            var apm = factory.GetInstance<IAutomatedProcessManager>();
            apm.Setup<ApmSetup>();
            apm.Start();

            var OrderDownload = factory.GetInstance<IOrderQueueDownloadService>();
            if(OrderDownload == null)
            {
                log.LogMessage("Order download service is not registered in the DI container.");
                return;
            }
            log.LogMessage("Starting Order download service");
            OrderDownload.Start();

            appLifeTime.ApplicationStopping.Register(() =>
            {
                apm.Stop();
                log.LogMessage("Stopping Order download service");
                OrderDownload.Stop();
            });

        }


        private static readonly LoggerFactory DbCommandConsoleLoggerFactory = new LoggerFactory(new ILoggerProvider[] {
            new ConsoleLoggerProvider ((category, level) => category == DbLoggerCategory.Database.Command.Name && level == Microsoft.Extensions.Logging.LogLevel.Information, true)
        });
    }
}

