using System;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Service.Contracts;
using Service.Contracts.Database;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Service.Contracts.Database
{
	/// <summary>
	/// This class is used to hold all the information required to create database connections.
	/// </summary>
	public class DBConfiguration : IDBConfiguration
	{
		class ProviderInfo
		{
			public string ProviderName;
			public string FactoryAssemblyName;
			public string FactoryType;
			public string BuilderAssemblyName;
			public string BuilderType;
		}

		
		// =====================================================
		// Static members
		// =====================================================
		#region Static
		private static Dictionary<string, ProviderInfo> providerInfo;

		static DBConfiguration()
		{
			providerInfo = new Dictionary<string, ProviderInfo>();
#if NET461
			providerInfo.Add("System.Data.SqlClient", new ProviderInfo()
			{
				ProviderName = "System.Data.SqlClient",
				FactoryAssemblyName = "System.Data.Common",
				FactoryType = "System.Data.Common.DbProviderFactories",
				BuilderAssemblyName = "Service.Contracts",
                BuilderType = "Service.Contracts.Database.SqlServerStatementBuilder"
            });
			providerInfo.Add("System.Data.Sqlite", new ProviderInfo()
			{
				ProviderName = "System.Data.Sqlite",
				FactoryAssemblyName = "System.Data.Common",
				FactoryType = "System.Data.Common.DbProviderFactories",
				BuilderAssemblyName = "Service.Contracts",
                BuilderType = "Service.Contracts.Database.SqliteStatementBuilder"
			});
			providerInfo.Add("MySql.Data.MySqlClient", new ProviderInfo()
			{
				ProviderName = "MySql.Data.MySqlClient",
				FactoryAssemblyName = "Microsoft.Data.Common",
				FactoryType = "System.Data.Common.DbProviderFactories",
				BuilderAssemblyName = "Service.Contracts",
				BuilderType = "Service.Contracts.Database.MySqlStatementBuilder"
			});
			providerInfo.Add("System.Data.OleDb", new ProviderInfo()
			{
				ProviderName = "System.Data.OleDb",
				FactoryAssemblyName = "System.Data.Common",
				FactoryType = "System.Data.Common.DbProviderFactories",
				BuilderAssemblyName = "Service.Contracts",
				BuilderType = "Service.Contracts.Database.SqlServerStatementBuilder"
			});
#else 
    #if NET8_0
            providerInfo.Add("System.Data.SqlClient", new ProviderInfo()
			{
				ProviderName = "System.Data.SqlClient",
				FactoryAssemblyName = "System.Data.SqlClient",
				FactoryType = "System.Data.SqlClient.SqlClientFactory",
				BuilderAssemblyName = "Service.Contracts-Net8",
				BuilderType = "Service.Contracts.Database.SqlServerStatementBuilder"
			});
			providerInfo.Add("Microsoft.Data.Sqlite", new ProviderInfo()
			{
				ProviderName = "Microsoft.Data.Sqlite",
				FactoryAssemblyName = "Microsoft.Data.Sqlite",
				FactoryType = "Microsoft.Data.Sqlite.SqliteFactory",
				BuilderAssemblyName = "Service.Contracts-Net8",
				BuilderType = "Service.Contracts.Database.SqliteStatementBuilder"
			});
			providerInfo.Add("MySql.Data.MySqlClient", new ProviderInfo()
			{
				ProviderName = "MySql.Data.MySqlClient",
				FactoryAssemblyName = "MySql.Data",
				FactoryType = "MySql.Data.MySqlClient.MySqlClientFactory",
				BuilderAssemblyName = "Service.Contracts-Net8",
				BuilderType = "Service.Contracts.Database.MySqlStatementBuilder"
			});
    #else
            providerInfo.Add("System.Data.SqlClient", new ProviderInfo()
            {
                ProviderName = "System.Data.SqlClient",
                FactoryAssemblyName = "System.Data.SqlClient",
                FactoryType = "System.Data.SqlClient.SqlClientFactory",
                BuilderAssemblyName = "Service.Contracts",
                BuilderType = "Service.Contracts.Database.SqlServerStatementBuilder"
            });
            providerInfo.Add("Microsoft.Data.Sqlite", new ProviderInfo()
            {
                ProviderName = "Microsoft.Data.Sqlite",
                FactoryAssemblyName = "Microsoft.Data.Sqlite",
                FactoryType = "Microsoft.Data.Sqlite.SqliteFactory",
                BuilderAssemblyName = "Service.Contracts",
                BuilderType = "Service.Contracts.Database.SqlServerStatementBuilder"
            });
            providerInfo.Add("System.Data.Sqlite", new ProviderInfo()
            {
                ProviderName = "Microsoft.Data.Sqlite",
                FactoryAssemblyName = "Microsoft.Data.Sqlite",
                FactoryType = "Microsoft.Data.Sqlite.SqliteFactory",
                BuilderAssemblyName = "Service.Contracts",
                BuilderType = "Service.Contracts.Database.SqlServerStatementBuilder"
            });
            providerInfo.Add("MySql.Data.MySqlClient", new ProviderInfo()
            {
                ProviderName = "MySql.Data.MySqlClient",
                FactoryAssemblyName = "MySql.Data",
                FactoryType = "MySql.Data.MySqlClient.MySqlClientFactory",
                BuilderAssemblyName = "Service.Contracts",
                BuilderType = "Service.Contracts.Database.MySqlStatementBuilder"
            });

    #endif
#endif
        }

		/// <summary>
		/// Returns the "Invariable Name" of all data providers that are installed in this system.
		/// </summary>
		public static string[] GetProviders() { return (from p in providerInfo.Values select p.ProviderName).ToArray(); }


		public static bool[] ReaderContainsFields(IDataReader rd, string[] fieldNames)
		{
			int rdMax = rd.FieldCount;
			int arMax = fieldNames.Length;
			bool[] result = new bool[arMax];
			for (int i = 0; i < rdMax; i++)
			{
				string rdField = rd.GetName(i);
				for (int j = 0; j < arMax; j++)
				{
					string arField = fieldNames[j];
					if (String.Compare(rdField, arField, true) == 0)
					{
						result[j] = true;
						break;
					}
				}
			}
			return result;
		}


		private static DbProviderFactory GetDBFactoryInstance(string providername)
		{
#if NET461
            if(providername.IndexOf("sqlite", StringComparison.OrdinalIgnoreCase) >= 0)
                return System.Data.SQLite.SQLiteFactory.Instance;
            return DbProviderFactories.GetFactory(providername);
#else
			var p = providerInfo[providername];
			Type t = Type.GetType($"{p.FactoryType}, {p.FactoryAssemblyName}");
			PropertyInfo pinfo = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
			if (pinfo != null)
				return pinfo.GetValue(null) as DbProviderFactory;
			FieldInfo finfo = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
			return finfo.GetValue(null) as DbProviderFactory;
#endif
		}


		private static ISqlStatementBuilder GetBuilder(string providername)
		{
			var p = providerInfo[providername];
			Type t = Type.GetType($"{p.BuilderType}, {p.BuilderAssemblyName}");
			return Activator.CreateInstance(t) as ISqlStatementBuilder;
		}

#endregion



		private object factoryLock = new object();
		private object parameterCacheLock = new object();
		private object metadataCacheLock = new object();

		private IFactory factory;
		private string providername;
		private DbProviderFactory dbFactory;
		private ISqlStatementBuilder SqlBuilder;
		private Dictionary<string, string[]> parameterNameCache;
		private Dictionary<string, EntityMetadata> metadataCache;
		private string connectionString;


		public DBConfiguration(IFactory factory)
		{
			this.factory = factory;
			parameterNameCache = new Dictionary<string, string[]>();
			metadataCache = new Dictionary<string, EntityMetadata>();
		}


		/// <summary>
		/// Gets the database name
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// Gets the database name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Allows to get/set the connection string used when creating an instance of the DBX class from this configuration object.
		/// </summary>
		public string ConnectionString
		{
			get { return connectionString; }
			set { connectionString = value; }
		}

		/// <summary>
		/// Allows the user to get/set the DataProvider used when creating an instance of the DBX class from this configuration object.
		/// </summary>
		public string ProviderName
		{
			get { return providername; }
			set
			{
				lock (factoryLock)
				{
					if (providername != value)
					{
						providername = value;
						dbFactory = GetDBFactoryInstance(providername);
						SqlBuilder = GetBuilder(providername);
					}
				}
			}
		}


		/// <summary>
		/// Returns a reference to the Data Provider's Factory.
		/// </summary>
		public DbProviderFactory DBFactory
		{
			get { lock (factoryLock) return dbFactory; }
		}


		/// <summary>
		/// Allows to get the character used as string delimited.
		/// </summary>
		public string StringDelimiter { get { return SqlBuilder.StringDelimiter; } }


		public bool DatabaseExists
		{
			get
			{
				var dbName = GetInitialCatalog();
				using(var conn = CreateConnectionToDataSource().Result)
				{
					var count = Convert.ToInt32(conn.ExecuteScalar("select count(name) from [sysdatabases] where [name] = @dbname", dbName));
					return count > 0;
				}
			}
		}


		public void EnsureCreated()
		{
			EnsureCreatedAsync(null).Wait();
		}

        public async Task EnsureCreatedAsync()
        {
            await EnsureCreatedAsync(null);
        }



        public async Task EnsureCreatedAsync(Action<IDBX, bool> initializationCallback)
		{
			string dbname = GetInitialCatalog();
			using (var conn = await CreateConnectionToDataSource())
			{
				if (!DatabaseExists)
				{
					switch (providername)
					{
						case CommonDataProviders.SqlServer:
							await conn.ExecuteNonQueryAsync($"create database [{dbname}]");
							await conn.ExecuteNonQueryAsync($"use [{dbname}]");
							initializationCallback?.Invoke(conn, false);
							break;
						default:
							throw new NotImplementedException("No time to implement this for other providers... Implement as needed...");
					}
				}
				else
				{
					await conn.ExecuteNonQueryAsync($"use [{dbname}]");
					initializationCallback?.Invoke(conn, true);
				}
			}
		}

		private async Task<IDBX> CreateConnectionToDataSource()
		{
			DbConnectionStringBuilder sb;
			lock (factoryLock)
			{
				if (dbFactory == null || String.IsNullOrWhiteSpace(connectionString))
					throw new InvalidOperationException("You need to configure the data provider and the connection string before attempting this operation.");
				sb = dbFactory.CreateConnectionStringBuilder();
			}
			sb.ConnectionString = connectionString;
			switch (providername)
			{
				case CommonDataProviders.SqlServer:
					var ds = ((string)sb["Data Source"]).ToLower();
					if (!ds.Contains("localdb"))
						sb["Initial Catalog"] = "master";
					else
						sb.ConnectionString = $"Data Source={ds};";
					break;
				default:
					throw new NotImplementedException("No time to implement this for other providers... Implement as needed...");
			}
			var dbcfg = new DBConfiguration(factory);
			dbcfg.ProviderName = providername;
			dbcfg.ConnectionString = sb.ConnectionString;
			return await dbcfg.CreateConnectionAsync();
		}

		/// <summary>
		/// Creates and opens a new database connection.
		/// </summary>
		public IDBX CreateConnection()
		{
			if (dbFactory == null || String.IsNullOrWhiteSpace(connectionString))
				throw new InvalidOperationException("You need to configure the data provider and the connection string before attempting this operation.");
			DBX db = new DBX(factory, this, true);
			return db;
		}


		/// <summary>
		/// Creates a new DBX object using "this" DBConfiguration, optionally the connection is open
		/// </summary>
		public IDBX CreateConnection(bool connected)
		{
			DBX db = new DBX(factory, this, connected);
			return db;
		}


		/// <summary>
		/// Creates and opens a new database connection.
		/// </summary>
		public async Task<IDBX> CreateConnectionAsync()
		{
			if (dbFactory == null || String.IsNullOrWhiteSpace(connectionString))
				throw new InvalidOperationException("You need to configure the data provider and the connection string before attempting this operation.");
			DBX db = new DBX(factory, this, false);
			await db.OpenAsync();
			return db;
		}

		public void Configure(string databaseName)
		{
			var config = factory.GetInstance<IAppConfig>();
			ProviderName = config.GetValue<string>($"Databases.{databaseName}.Provider");
			ConnectionString = config.GetValue<string>($"Databases.{databaseName}.ConnStr");
		}


		public string GetInitialCatalog()
		{
			DbConnectionStringBuilder sb;
			lock (factoryLock)
			{
				if (dbFactory == null || String.IsNullOrWhiteSpace(connectionString))
					throw new InvalidOperationException("You need to configure the data provider and the connection string before attempting this operation.");
				sb = dbFactory.CreateConnectionStringBuilder();
			}
			sb.ConnectionString = connectionString;
			switch (providername)
			{
				case CommonDataProviders.SqlServer:
					return sb["Initial Catalog"].ToString();
				default:
					throw new NotImplementedException("No time to implement this for other providers... Implement as needed...");
			}
		}


		/// <summary>
		/// Gets entity metadata from metadata cache, if the entity is not in the cache it gets added so that
		/// future calls run faster.
		/// </summary>
		internal EntityMetadata GetMetadata(object entity)
		{
			string key;
			EntityMetadata info = null;
			if (entity is Type)
				key = (entity as Type).FullName;
			else
				key = entity.GetType().FullName;
			lock (metadataCacheLock)
			{
				if (metadataCache.TryGetValue(key, out info))
				{
					return info;
				}
				else
				{
					info = EntityMetadata.GetMetadata(entity, SqlBuilder);
					metadataCache.Add(key, info);
					return info;
				}
			}
		}


		/// <summary>
		/// Returns all parameters found in the query string from left to right (repeated entryes are filtered).
		/// </summary>
		/// <param name="query">The query string that might contain some parameters.</param>
		/// <returns>Returns an array of string objects. Each element represents an argument that is going to be pased to the database.</returns>
		internal string[] GetParameterNames(string query)
		{
			string[] result;
			// Since this might require a bit of procesing eachtime we execute a query we will cache the results
			// of this method so that future calls run faster.
			lock (parameterCacheLock)
			{
				if (parameterNameCache.TryGetValue(query, out result))
				{
					return result;
				}
				else
				{
					result = GetParamNames(query);
					parameterNameCache.Add(query, result);
					return result;
				}
			}
		}



		private string[] GetParamNames(string query)
		{
			// Nothing in the cache... so extract parameter info and add to cache
			int idx1, idx2;
			var lst = new List<string>();
			char[] symbols = " ,\t\r\n=+-*/!&|()[].<>;\'$".ToCharArray();
			string parameterName;
            query = RemoveDeclares(query);

            idx1 = query.IndexOf('@');
			while (idx1 >= 0)
			{
				idx2 = query.IndexOfAny(symbols, idx1 + 1);
				if (idx2 > idx1)
				{
					parameterName = query.Substring(idx1, idx2 - idx1).Trim();
					if (!lst.Contains(parameterName))
						lst.Add(parameterName);
					idx1 = query.IndexOf('@', idx2 + 1);
				}
				else
				{
					parameterName = query.Substring(idx1).Trim();
					if (!lst.Contains(parameterName))
						lst.Add(parameterName);
					break; //End since it was the last token in the string
				}
			}
			return lst.ToArray();
		}

        private string RemoveDeclares(string query)
        {
            Regex re = new Regex(@"declare\s+(?<id>@[\w\d_]+).*", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            MatchCollection m = re.Matches(query);
            if (m.Count > 0)
            {
                foreach (Match match in m)
                {
                    foreach (Group group in match.Groups)
                    {
                        foreach (Capture capture in group.Captures)
                        {
                            query = query.Replace(capture.Value, "");
                        }
                    }
                }
            }
            return query.Replace("@@", "");
        }
    }

	/// <summary>
	/// Lists the names of supported Data Providers
	/// </summary>
	public class CommonDataProviders
	{
		public const string SqlServer = "System.Data.SqlClient";
		public const string Sqlite = "System.Data.Sqlite";
		public const string MySql = "MySql.Data.MySqlClient";
		public const string Oracle = "System.Data.OracleClient";
	}
}
