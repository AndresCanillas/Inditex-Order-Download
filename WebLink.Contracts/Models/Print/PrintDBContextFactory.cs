using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
	public class PrinterDBContextFactory : IDesignTimeDbContextFactory<PrintDB>
	{
		private IAppConfig configuration;

		public PrinterDBContextFactory()
		{
			var factory = new ServiceFactory();
			configuration = factory.GetInstance<IAppConfig>();
			if (!File.Exists(configuration.FileName))
			{
				var configPath = configuration.FileName.Substring(0, configuration.FileName.IndexOf("\\WebLink.Contracts"));
				var configFile = Path.Combine(configPath, "WebLink", "appsettings.json");
				if (File.Exists(configFile))
					configuration.Load(configFile);
			}
		}

		public PrintDB CreateDbContext(string[] args)
		{
			var builder = new DbContextOptionsBuilder<PrintDB>();
			var connStr = configuration.GetValue<string>("Databases.MainDB.ConnStr");
			builder.UseSqlServer(connStr, optionsBuilder => optionsBuilder.MigrationsAssembly(typeof(PrintDB).GetTypeInfo().Assembly.GetName().Name));
			return new PrintDB(builder.Options, null);
		}
	}
}
