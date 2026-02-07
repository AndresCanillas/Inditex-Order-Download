using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using WebLink.Contracts;

namespace WebLink.Services
{
	public class DBConnectionManager: IDBConnectionManager
	{
		private IFactory factory;
		private IAppConfig configuration;

		public DBConnectionManager(IFactory factory, IAppConfig configuration)
		{
			this.factory = factory;
			this.configuration = configuration;
		}

		public string WebLinkDB
		{
			get
			{
				SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(configuration.GetValue<string>("Databases.MainDB.ConnStr"));
				return csb.InitialCatalog;
			}
		}

		public string UsersDB
		{
			get
			{
				SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(configuration.GetValue<string>("Databases.IdentityDB.ConnStr"));
				return csb.InitialCatalog;
			}
		}

		public string CatalogDB
		{
			get
			{
				SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(configuration["Databases.CatalogDB.ConnStr"]);
				return csb.InitialCatalog;
			}
		}


		public IDBX OpenDatabase(string database)
		{
			var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = CommonDataProviders.SqlServer;
			cfg.ConnectionString = configuration[$"Databases.{database}.ConnStr"];
			return cfg.CreateConnection();
		}

		public IDBX OpenWebLinkDB()
		{
			var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = CommonDataProviders.SqlServer;
			cfg.ConnectionString = configuration.GetValue<string>("Databases.MainDB.ConnStr");
			return cfg.CreateConnection();
		}

		public IDBX OpenUsersDB()
		{
			var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = CommonDataProviders.SqlServer;
			cfg.ConnectionString = configuration.GetValue<string>("Databases.IdentityDB.ConnStr");
			return cfg.CreateConnection();
		}

		public IDBX OpenCatalogDB()
		{
			var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = CommonDataProviders.SqlServer;
			cfg.ConnectionString = configuration["Databases.CatalogDB.ConnStr"];
			return cfg.CreateConnection();
		}

		public IDBX OpenIDTECIBrands()
		{
			var cfg = factory.GetInstance<IDBConfiguration>();
			cfg.ProviderName = configuration["Databases.idtecibrands.Provider"];
			cfg.ConnectionString = configuration["Databases.idtecibrands.ConnStr"];
			return cfg.CreateConnection();
		}


		public DynamicDB CreateDynamicDB()
		{
			var db = factory.GetInstance<DynamicDB>();
			db.Open(configuration["Databases.CatalogDB.ConnStr"]);
			return db;
		}

        public IDBX OpenHerculesDB()
        {
            var cfg = factory.GetInstance<IDBConfiguration>();
            cfg.ProviderName = CommonDataProviders.SqlServer;
            cfg.ConnectionString = configuration["Databases.HerculesDB.ConnStr"];
            return cfg.CreateConnection();
        }
    }
}
