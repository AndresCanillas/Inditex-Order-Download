using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;

namespace Service.Contracts.Database
{
	public interface IConnectionManager
	{
        IDBConfiguration GetDBConfiguration(string name);
        IDBX OpenDB();
		IDBX OpenDB(string name);
		Task<IDBX> OpenDBAsync(string name);
		string GetInitialCatalog(string name);
		void Enqueue(Action<IDBX> operation);
        IDBX OpenSqlite(string connectionString);
        IDBX OpenSqlServer(string connectionString);
    }

	public class ConnectionManager : IConnectionManager
	{
		private IAppConfig config;
		private IDBConfiguration db;
		private WakingDBTasks tasks;

		public ConnectionManager(IAppConfig config, IDBConfiguration db, ILogService log)
		{
			this.config = config;
			this.db = db;
			tasks = new WakingDBTasks(()=>OpenDB(), log);
		}

        public IDBConfiguration GetDBConfiguration(string name)
        {
            db.ProviderName = config[$"Databases.{name}.Provider"];
            db.ConnectionString = config[$"Databases.{name}.ConnStr"];
            if(db.ProviderName == null || db.ConnectionString == null)
                return null;
            else 
                return db;
        }

        public IDBX OpenDB()
		{
			db.ProviderName = CommonDataProviders.SqlServer;
			db.ConnectionString = config.GetValue<string>("Databases.MainDB.ConnStr");
			return db.CreateConnection();
		}

		public IDBX OpenDB(string name)
		{
			db.ProviderName = config[$"Databases.{name}.Provider"];
			db.ConnectionString = config[$"Databases.{name}.ConnStr"];
			return db.CreateConnection();
		}

		public async Task<IDBX> OpenDBAsync(string name)
		{
			db.ProviderName = config[$"Databases.{name}.Provider"];
			db.ConnectionString = config[$"Databases.{name}.ConnStr"];
			return await db.CreateConnectionAsync();
		}

        public IDBX OpenSqlite(string connectionString)
        {
            db.ProviderName = CommonDataProviders.Sqlite;
            db.ConnectionString = connectionString;
            return db.CreateConnection();
        }

        public IDBX OpenSqlServer(string connectionString)
        {
            db.ProviderName = CommonDataProviders.SqlServer;
            db.ConnectionString = connectionString;
            return db.CreateConnection();
        }


        public string GetInitialCatalog(string name)
		{
			db.ProviderName = config[$"Databases.{name}.Provider"];
			db.ConnectionString = config[$"Databases.{name}.ConnStr"];
			return db.GetInitialCatalog();
		}

		public void Enqueue(Action<IDBX> operation)
		{
			tasks.Enqueue(operation);
		}
	}
}
