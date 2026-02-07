using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Print.Middleware;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace WebLink
{
	public class Program
	{
		public static IFactory Factory;
		public static bool InitDB;
		public static string ConfigFile;

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
				ConfigFile = CmdLineHelper.ExtractCmdArgument("config", false, "appsettings.json");
				var appInfo = Factory.GetInstance<IAppInfo>();
                var config = Factory.GetInstance<IAppConfig>();
				var log = Factory.GetInstance<ILogService>();
				log.LogMessage("Service Started");
				try
				{
					AssemblyResolver.AddSearchLocation(Path.Combine(appInfo.AssemblyDir, "plugins"));
					AssemblyResolver.Initialize();
					if (InitDB)
					{
						CreateWebHostBuilder().Build().Run();
						log.LogMessage("Host.Run finished.");
					}
					else
					{
						CreateWebHostBuilder().Build().RunAsService();
					}
				}
				catch (Exception ex)
				{
					log.LogException(ex);
				}
                finally
                {
                    log.Terminate();
                }
            }
			catch(Exception ex)
			{
				File.WriteAllText("C:\\Temp\\Print.txt", $"{ex.Message}\r\n{ex.StackTrace}");
			}
		}

		public static IWebHostBuilder CreateWebHostBuilder()
		{
			string contentPath;
			var log = Program.Factory.GetInstance<ILogService>();
			contentPath = Environment.CurrentDirectory;
#if !DEBUG
			var pathToExe = Assembly.GetExecutingAssembly().Location;
			log.LogMessage("Executing Assembly Path: " + pathToExe);
			contentPath = Path.GetDirectoryName(pathToExe);
#endif
			log.LogMessage("Initializing");
			log.LogMessage("Current Directory: " + Environment.CurrentDirectory);
			log.LogMessage("Content Path: " + contentPath);

			var host = new WebHostBuilder()
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
				if (endpoint.UseSSL)
				{
					X509Certificate2 cert;
					if (endpoint.SSLSource == "Store")
						cert = GetSSLCert(endpoint.CertName);
					else
					{
						if (!String.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(endpoint.CertName)))
							cert = GetEmbeddedCertByName(endpoint.CertName);
						else
							cert = GetEmbeddedCertByIP();
					}
					options.Listen(IPAddress.Any, endpoint.Port, listenOptions => listenOptions.UseHttps(cert));
				}
				else
				{
					options.Listen(IPAddress.Any, endpoint.Port);
				}
			}
			options.Limits.MaxRequestBodySize = int.MaxValue;
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
				foreach (X509Certificate cert in store.Certificates)
				{
					if (cert.Subject.Contains(certName))  //"*.smartdots.es"
						return cert as X509Certificate2;
				}
			}
			catch { }
			return GetEmbeddedCertByIP();
		}


		private static X509Certificate2 GetEmbeddedCertByIP()
		{
			var asm = Assembly.GetExecutingAssembly();
			var resources = asm.GetManifestResourceNames().Where(p => p.EndsWith(".pfx")).ToList();
			Regex re = new Regex(@"^.+\.(?'ip'\d+\.\d+\.\d+\.\d+)\.pfx$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
			foreach (string cert in resources)
			{
				var match = re.Match(cert);
				if (match.Groups.Count > 1) {
					string ip = match.Groups[1].Captures[0].Value;
					if (TcpHelper.IsLocalAddress(ip))
					{
						return GetEmbeddedCertByName(cert);
					}
				}
			}
			// Last resort: return a default certificate.
			return GetEmbeddedCertByName("PrintCentral.81.25.127.175.pfx");
		}


		private static X509Certificate2 GetEmbeddedCertByName(string name)
		{
			var asm = Assembly.GetExecutingAssembly();
			using (MemoryStream ms = new MemoryStream())
			{
				using (var file = asm.GetManifestResourceStream(name))
				{
					file.CopyTo(ms, 4096);
					var x509Cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(ms.ToArray(), "publico");
					return x509Cert;
				}
			}
		}
	}
}
