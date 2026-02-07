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
	public class IdentityDBContextFactory : IDesignTimeDbContextFactory<IdentityDB>
	{
		private IAppConfig configuration;

		public IdentityDBContextFactory()
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

		public IdentityDB CreateDbContext(string[] args)
		{
			var builder = new DbContextOptionsBuilder<IdentityDB>();
			var connStr = configuration.GetValue<string>("Databases.IdentityDB.ConnStr");
			builder.UseSqlServer(connStr, optionsBuilder => optionsBuilder.MigrationsAssembly(typeof(IdentityDB).GetTypeInfo().Assembly.GetName().Name));
			return new IdentityDB(builder.Options);
		}
	}
}
