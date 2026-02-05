using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderDonwLoadService;
using Print.Middleware;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace OrderDownloadWebApi
{
    public class Program
    {
        public static IFactory Factory;
        public static bool InitDB;
        public const string VERSION = "v1";

        public static void Main(string[] args)
        {
            try
            {
                Factory = new ServiceFactory();
#if DEBUG
                InitDB = true;
#else
				InitDB = CmdLineHelper.ExtractCmdArgument("initdb", false, "0") == "1";
#endif
                var appInfo = Factory.GetInstance<IAppInfo>();
                var config = LoadConfiguration(appInfo);
                var log = Factory.GetInstance<IAppLog>();
                log.InitializeLogFile(
                    Path.Combine(appInfo.SystemLogDir, "InditexOrderDownload.log"),
                    config.GetValue("WebLink:MaxLogSize", 4194304));


                Factory.Setup<DIContainerSetup>();
                try
                {

                    log.LogMessage("WebApi Service Started");
                    if(InitDB)
                    {
                        CreateWebHostBuilder().Build().Run();
                        log.LogMessage("WebApi Host.Run finished.");
                        log.Terminate();
                    }
                    else
                    {
                        CreateWebHostBuilder().Build().RunAsService();
                        log.LogMessage("WebApi Service has been stopped");
                    }

                }
                catch(Exception ex)
                {
                    log.LogException(ex);
                    log.Terminate();


                }
            }
            catch(Exception ex)
            {
                File.WriteAllText("C:\\Temp\\InditexOrderDownload.txt", $"{ex.Message}\r\n{ex.StackTrace}");
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder()
        {
            string contentPath;
            var log = Program.Factory.GetInstance<IAppLog>();
            contentPath = Environment.CurrentDirectory;
#if !DEBUG
			var pathToExe = Assembly.GetExecutingAssembly().Location;
			log.LogMessage("Executing Assembly Path: " + pathToExe);
			contentPath = Path.GetDirectoryName(pathToExe);
#endif
            log.LogMessage("Initializing");
            log.LogMessage("Current Directory: " + Environment.CurrentDirectory);
            log.LogMessage("Content Path: " + contentPath);
            log.LogLevel = Service.Contracts.LogLevel.Default;

            var host = new WebHostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .UseKestrel(ConfigureServerEndPoints)
            .UseContentRoot(contentPath)
            .UseDefaultServiceProvider((context, options) => { options.ValidateScopes = false; })
            .UseStartup<Startup>();
            return host;
        }

        private static void ConfigureServerEndPoints(KestrelServerOptions options)
        {
            var cfg = Factory.GetInstance<IAppConfig>();
            var endpoints = cfg.Bind<List<ServerEndPoint>>("EndPoints");
            foreach(var endpoint in endpoints)
            {
                if(endpoint.UseSSL)
                {
                    X509Certificate2 cert;
                    if(endpoint.SSLSource == "Store")
                        cert = GetSSLCert(endpoint.CertName);
                    else
                        cert = GetEmbeddedCert(endpoint.CertName);
                    options.Listen(IPAddress.Any, endpoint.Port, listenOptions => listenOptions.UseHttps(cert));
                }
                else
                {
                    options.Listen(IPAddress.Any, endpoint.Port);
                }
            }
        }

        private static IConfigurationRoot LoadConfiguration(IAppInfo appInfo)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(appInfo.AssemblyDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            return config;
        }

        private static X509Certificate2 GetSSLCert(string certName)
        {
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                foreach(X509Certificate cert in store.Certificates)
                {
                    if(cert.Subject.Contains(certName))  //"*.smartdots.es"
                        return cert as X509Certificate2;
                }
            }
            catch { }
            return GetEmbeddedCert(".pfx");
        }

        private static X509Certificate2 GetEmbeddedCert(string certName)
        {
            var asm = Assembly.GetExecutingAssembly();
            var resources = asm.GetManifestResourceNames().Where(p => p.EndsWith(certName)).ToList();
            foreach(string cert in resources)
            {
                string fileName = cert.Substring(8);
                string ip = Path.GetFileNameWithoutExtension(fileName);
                if(TcpHelper.IsLocalAddress(ip))
                {
                    return GetEmbeddedCertByName(cert);
                }
            }
            // Last resort: return a default certificate.
            return GetEmbeddedCertByName("PrintLocal.81.25.127.175.pfx");
        }

        private static X509Certificate2 GetEmbeddedCertByName(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using(MemoryStream ms = new MemoryStream())
            {
                using(var file = asm.GetManifestResourceStream(name))
                {
                    file.CopyTo(ms);
                    var x509Cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(ms.ToArray(), "publico");
                    return x509Cert;
                }
            }
        }
    }
}
