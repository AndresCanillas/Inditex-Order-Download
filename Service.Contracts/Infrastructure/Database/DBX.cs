using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Reflection;
using System.Data.SqlClient;
using Service.Contracts;
using Service.Contracts.Database;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Transactions;
using Newtonsoft.Json.Bson;
using System.Threading.Tasks;
#if NET461
using System.Data.OleDb;
#endif

namespace Service.Contracts.Database
{
	/// <summary>
	/// Class used to perform operations against a database.
	/// </summary>
	class DBX : IDBX
	{
		private IFactory factory;
		private DBConfiguration cfg;
		private DbConnection conn;
		private int cmdTimeout = 120;
		private IDBXTransaction tx;

		/// <summary>
		/// Constructor. Creates and opens an ADO.NET database connection object.
		/// </summary>
		/// <remarks>
		/// Notice the internal access modifier, this forces the programmer to create
		/// connections using a DBConfiguration object and not directly.
		/// </remarks>
		internal DBX(IFactory factory, DBConfiguration cfg, bool connected)
		{
			this.factory = factory;
			this.cfg = cfg;
			if (connected) Open();
		}

		/// <summary>
		/// Gets the data provider used to access the database.
		/// </summary>
		public string Provider { get { return cfg.ProviderName; } }

		/// <summary>
		/// Allows to get the connection string used to connect to this database
		/// </summary>
		public string ConnectionString { get { return cfg.ConnectionString; } }


		public int CommandTimeout
		{
			get { return cmdTimeout; }
			set { cmdTimeout = value; }
		}


		/// <summary>
		/// Opens the database connection.
		/// </summary>
		public void Open()
		{
			if (cfg == null) throw new Exception("This database connection has been disposed and cannot be reused.");
			string connStr = cfg.ConnectionString;
			conn = cfg.DBFactory.CreateConnection();
			DbConnectionStringBuilder sb = cfg.DBFactory.CreateConnectionStringBuilder();
			sb.ConnectionString = connStr;
			if (sb.ContainsKey("Connect Timeout"))
			{
				sb["Connect Timeout"] = "30";
				connStr = sb.ConnectionString;
			}
			conn.ConnectionString = connStr;
			conn.Open();
		}


		public async Task OpenAsync()
		{
			if (cfg == null) throw new Exception("This database connection has been disposed and cannot be reused.");
			string connStr = cfg.ConnectionString;
			conn = cfg.DBFactory.CreateConnection();
			DbConnectionStringBuilder sb = cfg.DBFactory.CreateConnectionStringBuilder();
			sb.ConnectionString = connStr;
			if (sb.ContainsKey("Connect Timeout"))
			{
				sb["Connect Timeout"] = "30";
				connStr = sb.ConnectionString;
			}
			conn.ConnectionString = connStr;
			await conn.OpenAsync();
		}


		/// <summary>
		/// Closes the underlying database connection returning it to the database connection pool. However this object can still be reused, the program just needs to call Open again.
		/// </summary>
		public void Close()
		{
			if (conn != null)
			{
				conn.Close();
				conn.Dispose();
				conn = null;
			}
		}

		/// <summary>
		/// Closes the connection (returns it to the connection pool)
		/// </summary>
		public void Dispose()
		{
			try
			{
				Close();
			}
			catch { }
			finally
			{
				cfg = null;
			}
		}


		public IDBXTransaction BeginTransaction()
		{
			if (tx != null)
				return tx;
			var transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
			tx = new DBXTransaction(transaction, (t)=> { tx = null; });
			return tx;
		}


		/// <summary>
		/// Creates and configures an IDbCommand object in order to execute a query against the database
		/// </summary>
		private DbCommand PrepareCommand(string query, params object[] args)
		{
			//Creates a new command
			DbCommand cmd = cfg.DBFactory.CreateCommand();
			if (cmd.GetType().Name != "SqlCeCommand")
				cmd.CommandTimeout = cmdTimeout;
			cmd.Connection = conn;
			if (tx != null) cmd.Transaction = (tx as DBXTransaction).tx;
			if (args != null && args.Length > 0)
			{
				if (args[0] is DbParameter)
				{
					foreach (object o in args)
						cmd.Parameters.Add(o);
				}
				else if(args[0] is List<DbParameter>)
				{
					foreach (var parameter in args[0] as List<DbParameter>)
						cmd.Parameters.Add(parameter);
				}
				else
				{
					//Gets parameter information from the query string
					string[] pnames = cfg.GetParameterNames(query);
					//Setups the command named parameters
					DbParameter p;
					for (int i = 0; i < args.Length; i++)
					{
						p = cfg.DBFactory.CreateParameter();
						if (i < pnames.Length)
							p.ParameterName = pnames[i];
						if (args[i] != null)
						{
							p.Value = args[i];
						}
						else
						{
							p.Value = DBNull.Value;
						}
#if NET461
						if(p is OleDbParameter)
							if (p.DbType == DbType.DateTime) p.DbType = DbType.Date;
#endif
						cmd.Parameters.Add(p);
					}
				}
			}
			//returns a Command ready to be executed
			return cmd;
		}


		/// <summary>
		/// Executes the specified query and return True if data is returned, false otherwise.
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		public bool Exists(string query, params object[] args)
		{
			using (DbDataReader rd = ExecuteReader(query, args))
			{
				bool hasData = rd.Read();
				if (hasData)
				{
					object row0col0 = rd[0];
					if (row0col0 is int)
						return ((int)row0col0) != 0;
					else
						return true;
				}
				return false;
			}
		}


		public async Task<bool> ExistsAsync(string query, params object[] args)
		{
			using (var rd = await ExecuteReaderAsync(query, args))
			{
				bool hasData = await rd.ReadAsync();
				if (hasData)
				{
					object row0col0 = rd[0];
					if (row0col0 is int)
						return ((int)row0col0) != 0;
					else
						return true;
				}
				return false;
			}
		}


		/// <summary>
		/// Executes a user provided query and return a data reader with the results.
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		/// <returns>Returns a data reader from which we can fetch the results of the query.</returns>
		/// <remarks>
		/// It's very important that sql statements are not build by concatenating strings. For this reason
		/// when executing a query you need to use named parameters. Example:
		/// 
		/// The following example will query the clients table and fetch a row based on it's ID:
		/// 
		///     var rd = dbx.ExecuteReader("select * from clients where id = @ClientID", ClientID);
		///     
		/// IMPORTANT NOTES: 
		///   - Make sure to provide the values of the arguments in the same order they
		///     first appear in the query string. For instance, if your query is:
		///     
		///		"select * from table where id = @id and category = @catid"
		///		
		///		then, you must send the value for the "id" parameter before "catid":
		///		
		///		dbx.ExecuteReader("select * from table where id = @id and category = @catid", 10, 49);
		///     
		/// 
		///   - Ensure that arguments pased to the method and the columns being referenced
		///     by the query are of compatible data types. In a worst case scenario you'll get a
		///     run time exception while trying to execute the query due to a type missmatch.
		///     
		/// 
		///   - Also if a named parameter is used more than once in the query, you only need to send it
		///     the first time. For instance:
		/// 
		///     dbx.ExecuteReader("select * from clients where id > @ClientID and @ClientID + 10 > id", 23);
		///     
		///    The preceding query will yield all records which IDs are greater than 23 and less than 33.
		///    As you can see in the example, you only need to pass the value of the "ClientID" parameter once.
		/// </remarks>
		public DbDataReader ExecuteReader(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			return cmd.ExecuteReader();
		}


		public async Task<DbDataReader> ExecuteReaderAsync(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			return await cmd.ExecuteReaderAsync();
		}


        public async Task<DbDataReader> ExecuteSequentialReaderAsync(string query, params object[] args)
        {
            DbCommand cmd = PrepareCommand(query, args);
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = query;
            return await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        }


        /// <summary>
        /// Executes a user provided query and returns a DataSet with the results
        /// </summary>
        /// <param name="query">Query to be executed.</param>
        /// <param name="args">Values of the named parameters being passed to the database.</param>
        /// <returns>The result of the query as a DataSet.</returns>
        /// <remarks>Same remarks as with ExecuteReader above.</remarks>
        public DataSet ExecuteQuery(string query, params object[] args)
		{
			DataSet ds = new DataSet();
			DbCommand cmd = PrepareCommand(query, args);
			DbDataAdapter adapter = cfg.DBFactory.CreateDataAdapter();
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			adapter.SelectCommand = cmd;
			adapter.Fill(ds);
			return ds;
		}



		/// <summary>
		/// Execute a user provided query that only returns the number of rows affected (insert/update/delete statements)
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		/// <returns>Returns the number of rows affected by the sql statement.</returns>
		/// <remarks>Same remarks as with ExecuteReader above.</remarks>
		public int ExecuteNonQuery(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			return cmd.ExecuteNonQuery(); 
		}


		public async Task<int> ExecuteNonQueryAsync(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			return await cmd.ExecuteNonQueryAsync();
		}

		/// <summary>
		/// Execute a query that only returns a single value (the first column of the first row).
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		/// <returns>Returns the scalar value returned by the statement.</returns>
		/// <remarks>Same remarks as with ExecuteReader above.</remarks>
		public object ExecuteScalar(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			return cmd.ExecuteScalar();
		}

		public async Task<object> ExecuteScalarAsync(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = query;
			return await cmd.ExecuteScalarAsync();
		}


		/// <summary>
		/// Executes a stored procedure
		/// </summary>
		/// <param name="query">Query must contain the name of the stored procedure and the names of the spected parameters. The following sintax must be used: spname(@arg1, @arg2, ... @argn)</param>
		/// <param name="args">Arguments pased to the stored procedure. Its imperative that the order and type of the parameters match the stored procedure definition on the database.</param>
		/// <returns>Returns a data reader from which we can fetch results.</returns>
		/// <remarks>
		/// In order to execute a SP your query string must use the following sintax:
		/// 
		///     "spname(@param1, @param2,... @paramn)"
		///     
		/// NOTES: 
		///   - You must ensure that the names of the arguments listed in the query string
		///     are exactly the same names used by the stored procedure in the database.
		///     
		///   - Make sure to provide the values of the arguments in the same order they
		///     appear in the query string.
		///     
		///   - Ensure that arguments pased to the method and the arguments expected by the
		///     stored procedure are of compatible data types.
		/// </remarks>
		public DbDataReader ExecuteSP(string query, params object[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.StoredProcedure;
			int idx = query.IndexOf('(');
			if (idx >= 0)
				cmd.CommandText = query.Substring(0, idx);
			else
				cmd.CommandText = query;
			return cmd.ExecuteReader();
		}



		/// <summary>
		/// Executes a stored procedure. Allows to send the actual parameters used when executing the stored procedure, this lets the programmer access Output parameters if required.
		/// </summary>
		public DbDataReader ExecuteSP(string query, params DbParameter[] args)
		{
			DbCommand cmd = PrepareCommand(query, args);
			cmd.CommandType = CommandType.StoredProcedure;
			int idx = query.IndexOf('(');
			if (idx >= 0)
				cmd.CommandText = query.Substring(0, idx);
			else
				cmd.CommandText = query;
			return cmd.ExecuteReader();
		}



		/// <summary>
		/// Creates a DataSet with the data retrieved from the DataReader.
		/// </summary>
		public DataSet ReaderToDataSet(DbDataReader rd)
		{
			DataSet ds = new DataSet();
			string[] tableNames = new string[50];
			for (int i = 0; i < tableNames.Length; i++)
				tableNames[i] = "table" + i;
			ds.Load(rd, LoadOption.OverwriteChanges, tableNames);
			return ds;
		}


		/// <summary>
		/// Executes the specified query and returns the data of the rows as json
		/// </summary>
		public JObject SelectOneToJson(string query, params object[] args)
		{
			JObject o = new JObject();
			using (var rd = ExecuteReader(query, args))
			{
				if (rd.Read())
				{
					for (int i = 0; i < rd.FieldCount; i++)
						o.Add(rd.GetName(i), JToken.FromObject(rd.GetValue(i)));
				}
			}
			return o;
		}


		/// <summary>
		/// Executes the specified query and returns the data of the rows as json
		/// </summary>
		public JArray SelectToJson(string query, params object[] args)
		{
			JArray result = new JArray();
			using (var rd = ExecuteReader(query, args))
			{
				while (rd.Read())
				{
					JObject o = new JObject();
					for (int i = 0; i < rd.FieldCount; i++)
						o.Add(rd.GetName(i), JToken.FromObject(rd.GetValue(i)));
					result.Add(o);
				}
			}
			return result;
		}


		/// <summary>
		/// Inserts a new row in the database. The object being passed will be reflected on in order to get all the necessary data to build and execute the statement.
		/// </summary>
		/// <param name="entity">Object containing all the required fields to insert the new row. Type must adhere to specifications given in the Insert/Update/Delete remarks section.</param>
		/// <returns>If the table has an entity column, returns the identity value of the row that was inserted. In all other cases returns null.</returns>
		/// <remarks>
		/// 
		/// Insert/Update/Delete Remarks
		/// 
		/// The entity object sent to this methods must fulfill the following requirements:
		/// 
		///		1. The Type of the object must match the name of the target table or be decorated with
		///		   the TargetTable attribute.
		///		2. Properties and fields of the object must be public and match the names of the columns
		///		   in the table and be of compatible data types.
		///		3. Type is decorated using the necesary attributes. Most important attributes are PK and
		///		   Identity.
		///		
		/// 
		/// When the name of the type does not match the name of the table you can use the TargetTable
		/// attribute. Example:
		/// 
		///			[TargetTable("tbl_Staff")]
		///			public class Employee
		///			{
		///				...
		///			}
		/// 
		/// It is posible for a property or field to have a diferent name than that of the corresponding
		/// column in the table, this can be useful when database columns have cryptic names; if the need
		/// araises you can use the TargetColumn attribute:
		/// 
		///			[TargetColumn("imc")]
		///			public int MaxCapacity;
		///	
		/// Also the Type must be decorated using the PK and Identity attributes to provide information
		/// about which columns are part of the primary key and which column is treated as identity.
		///        
		///		    Example:
		///        
		///			[TargetTable("tbl_Staff")]
		///			public class Employee
		///			{
		///				[Identity, PK]
		///				public int EmpID;
		///				
		///				...
		///			}
		///	
		/// 
		/// Other attributes that are useful are:  Nullable, ReadOnly and Hidden
		/// 
		/// Nullable - Means that a column allows for null values, so the library can take the necesary
		///			   steps to check for that condition and react accordingly.
		///			   
		/// ReadOnly - Means that a column should never be included in an insert/update statement.
		/// 
		/// Hidden   - Means that the field or property does not correlate to any column, and should
		///			   be ignored by the generic methods (Insert/Update/Delete/SelectOne/FillObject/MakeList)
		/// 
		/// 
		/// A type used as an entity usually reflects exactly the fields that exists in the target
		/// table, hence, no aditional public fields or properties should be declared in an entity class
		/// unless they are decorated with the Hidden attribute.
		/// </remarks>
		public object Insert<T>(T entity) where T : class
        {
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			//Executes the statement and checks if there is anything to return.
			DbDataReader rd = ExecuteReader(info.InsertStatement(), info.GetInsertDBParameters(entity, cfg.DBFactory));
			try
			{
				if (rd.Read() && rd.FieldCount > 0)
				{
					object ID = rd[0];
					if (entity is IEntity)
					{
						((IEntity)entity).ID = Convert.ToInt32(ID);
					}
					else if (info.IdentityColumn != null)
					{
						var identity = info.Type.GetMember(info.IdentityColumn)[0];
						var identityType = identity.MemberType == MemberTypes.Field ? (identity as FieldInfo).FieldType : (identity as PropertyInfo).PropertyType;
						Reflex.SetMember(entity, info.IdentityColumn, Convert.ChangeType(ID, identityType));
					}
					return ID;
				}
				else return null;
			}
			finally
			{
				rd.Close();
			}
		}


		public async Task<object> InsertAsync<T>(T entity) where T : class
		{
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			//Executes the statement and checks if there is anything to return.
			DbDataReader rd = await ExecuteReaderAsync(info.InsertStatement(), info.GetInsertDBParameters(entity, cfg.DBFactory));
			try
			{
				if (rd.Read() && rd.FieldCount > 0)
				{
					object ID = rd[0];
					if (entity is IEntity)
					{
						((IEntity)entity).ID = Convert.ToInt32(ID);
					}
					else if (info.IdentityColumn != null)
					{
						var identity = info.Type.GetMember(info.IdentityColumn)[0];
						var identityType = identity.MemberType == MemberTypes.Field ? (identity as FieldInfo).FieldType : (identity as PropertyInfo).PropertyType;
						Reflex.SetMember(entity, info.IdentityColumn, Convert.ChangeType(ID, identityType));
					}
					return ID;
				}
				else return null;
			}
			finally
			{
				rd.Close();
			}
		}


		/// <summary>
		/// Inserts data into the specified table using Json as the source
		/// </summary>
		/// <param name="tableName">Name of the table</param>
		/// <param name="data">Data to be inserted in the table</param>
		public void InsertJson(string tableName, JObject data)
		{
			List<object> args = new List<object>();
			var ins = new StringBuilder(1000);
			var vals = new StringBuilder(1000);
			ins.Append($"insert into [{tableName}](");
			vals.Append(" values(");
			foreach(var prop in data.Properties())
			{
				if (String.Compare(prop.Name, "ID", true) != 0)
				{
					ins.Append($"[{prop.Name}],");
					vals.Append($"@{prop.Name.ToLower()},");
					var ptype = GetPropertyType(prop.Value.Type);
					if (ptype == null)
						args.Add(null);
					else
						args.Add(prop.Value.ToObject(ptype));
				}
			}
			ins.Remove(ins.Length - 1, 1);
			vals.Remove(vals.Length - 1, 1);
			ins.Append(")");
			vals.Append(")");
			ExecuteNonQuery($"{ins.ToString()}{vals.ToString()}", args.ToArray());
		}


		private Type GetPropertyType(JTokenType type)
		{
			switch (type)
			{
				case JTokenType.Boolean: return typeof(bool);
				case JTokenType.Date: return typeof(DateTime);
				case JTokenType.Float: return typeof(float);
				case JTokenType.Integer: return typeof(int);
				case JTokenType.Null: return null;
				default: return typeof(string);
			}
		}



		public void BulkInsert<T>(List<T> data)
		{
			string fileName = null;
			EntityMetadata info = cfg.GetMetadata(typeof(T));
			try
			{
				fileName = Path.GetTempFileName();
				ExportToCsv(fileName, data, false);
				ExecuteNonQuery($"bulk insert {info.TableName} from '{fileName}' with (DATAFILETYPE = 'widechar', FIRSTROW = 1, FORMAT='CSV', MAXERRORS=0)");
			}
			finally
			{
				if (fileName != null)
					File.Delete(fileName);
			}
		}

		public void BulkInsert(string table, string dataFile)
		{
			ExecuteNonQuery($"bulk insert {table} from '{dataFile}' with (DATAFILETYPE = 'widechar', FIRSTROW = 1, FORMAT='CSV', MAXERRORS=0)");
		}

		public void ExportToCsv<T>(string fileName, List<T> data, bool includeHeader, string fields = null)
		{
			EntityMetadata info = cfg.GetMetadata(typeof(T));
			CreateCSVFormat(info, fields, out var header, out var format);
			using (var file = File.OpenWrite(fileName))
			{
				using (var sw = new StreamWriter(file, Encoding.Unicode))
				{
					if (includeHeader) sw.WriteLine(header);
					int i = 0;
					foreach (var r in data)
					{
						i++;
						var args = info.GetInsertValues(r);
						if (info.IdentityColumn != null) args.Insert(0, i.ToString());
						replaceSpecialTypes(args);
						sw.WriteLine(format, args.ToArray());
					}
				}
			}
		}

		private void replaceSpecialTypes(List<object> args)
		{
			for(int i = 0; i < args.Count; i++)
			{
				if (args[i] is bool)
					args[i] = ((bool)args[i]) ? "1" : "0";
				else if (args[i] != null && args[i].GetType().IsEnum)
					args[i] = (int)args[i];
			}
		}

		private void CreateCSVFormat(EntityMetadata meta, string fields, out string header, out string format)
		{
			string[] columnNames = null;
			StringBuilder sbFormat = new StringBuilder(500);
			StringBuilder sbHeader = new StringBuilder(500);
			if (!String.IsNullOrWhiteSpace(fields)) columnNames = fields.Split(',');
			int i = 0;
			foreach (var c in meta.Columns)
			{
				if (c.IsIgnore) continue;
				if (columnNames != null)
				{
					if(columnNames.FirstOrDefault(p => String.Compare(p, c.CName, true) == 0) != null)
					{
						sbHeader.Append($"{c.CName},");
						sbFormat.Append($"{{{i++}}},");
					}
				}
				else
				{
					sbHeader.Append($"{c.CName},");
					sbFormat.Append($"{{{i++}}},");
				}
			}
			if (sbHeader.Length > 0)
				sbHeader.Remove(sbFormat.Length - 1, 1);
			if (sbFormat.Length > 0)
				sbFormat.Remove(sbFormat.Length - 1, 1);
			header = sbHeader.ToString();
			format = sbFormat.ToString();
		}


		/// <summary>
		/// Updates a row in the database. The object being passed will be reflected on in order to get all the necesary data to execute the statement.
		/// </summary>
		/// <param name="entity">Object containing all the required fields to update the row. Type must adhere to specifications given in the Insert/Update/Delete remarks section.</param>
		/// <returns>The number of rows affected by the update statement.</returns>
		/// <remarks>
		/// (See Insert remarks)
		/// 
		/// Also consider that the values stored in the diferent properties of the entity will be copied to the
		/// updated row, for this reason if you need partial updates (updates that only change a few fields) you
		/// must:
		/// 
		///		- Pre-populate the entity with the SelectOne method, or any other initialization logic.
		///		- Use diferent data contracts for different update stages; or
		///		- Resort to an alternative strategy like a stored procedure or a query to perform the task.
		/// </remarks>
		public int Update<T>(T entity) where T : class
        {
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
			//Executes the statement and returns the number of rows that where affected.
			return ExecuteNonQuery(info.UpdateStatement(), info.GetUpdateDBParameters(entity, cfg.DBFactory));
		}


		public async Task<int> UpdateAsync<T>(T entity) where T : class
        {
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
			//Executes the statement and returns the number of rows that where affected.
			return await ExecuteNonQueryAsync(info.UpdateStatement(), info.GetUpdateDBParameters(entity, cfg.DBFactory));
		}


		public Tuple<string, List<DbParameter>> GetUpdateStatement<T>(T entity) where T : class
		{
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
			//Returns the update statement without executing it
			return new Tuple<string, List<DbParameter>>(info.UpdateStatement(), info.GetUpdateDBParameters(entity, cfg.DBFactory));
		}


		public async Task<object> UpdateAsync<T>(object entity) where T : class
        {
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
			//Executes the statement and returns the number of rows that where affected.
			return await ExecuteNonQueryAsync(info.UpdateStatement(), info.GetUpdateDBParameters(entity, cfg.DBFactory));
		}


		public int Update(CatalogDefinition cat, JObject data)
		{
			List<object> args = new List<object>();
			var sets = new List<DBSetInfo>();
			var identity = cat.Fields.FirstOrDefault(p => p.IsIdentity);
			if (identity == null)
				throw new Exception("Cannot use this Update if table has no identity column.");
			else if (data[identity.Name] == null)
				throw new Exception("Value for the identity column was not supplied.");
			var statement = new StringBuilder(1000);
			statement.Append($"update {cat.Name}_{cat.ID} set ");
			foreach (var f in cat.Fields)
			{
				if (identity != null && identity.Name == f.Name) continue;
				if (f.Type == ColumnType.Set)
				{
					sets.Add(new DBSetInfo(f, data[f.Name]));
				}
				else
				{
					var p = data.Property(f.Name);
					if (p != null)
					{
						statement.Append(f.Name).Append("=@").Append(f.Name.ToLower()).Append(',');
						var ptype = GetPropertyType(p.Value.Type);
						if (ptype == null)
							args.Add(null);
						else
							args.Add(p.Value.ToObject(ptype));
					}
				}
			}
			statement.Remove(statement.Length - 1, 1);
			statement.Append(" where ");
			statement.Append($"{identity.Name} = @_identity_");
			args.Add(data[identity.Name].ToObject(typeof(int)));
			var recordCount = ExecuteNonQuery(statement.ToString(), args.ToArray());
			foreach(var s in sets)
				InsertIntoRel(cat, data.GetValue<int>("ID"), s.Field, s.SetData);
			return recordCount;
		}


		private void InsertIntoRel(CatalogDefinition cat, int recid, FieldDefinition field, JToken setData)
		{
			if (setData == null) return;
			var ids = setData.Value<string>();
			if (String.IsNullOrWhiteSpace(ids)) return;
			var right = SelectOne<CatalogDefinition>("select * from _Catalog_ where ID = @catid", field.CatalogID);
			var relName = $"REL_{cat.ID}_{right.ID}_{field.FieldID}";
			ExecuteNonQuery($"delete from [{relName}] where SourceID = @recid", recid);
			var insertStatement = $"insert into [{relName}] values(@source, @target)";
			var idArray = ids.Split(',');
			foreach (var id in idArray)
			{
				var targetid = Convert.ToInt32(id);
				if (targetid != 0)
					ExecuteNonQuery(insertStatement, recid, targetid);
			}
		}



		/// <summary>
		/// Updates data into the specified table using Json as the source
		/// </summary>
		/// <param name="tableName">Name of the table</param>
		/// <param name="data">Data to be updated in the table</param>
		public int UpdateJson(string tableName, JObject data)
		{
			List<object> args = new List<object>();
			var qry = new StringBuilder(1000);
			qry.Append($"update [{tableName}] set ");
			foreach (var prop in data.Properties())
			{
				if (String.Compare(prop.Name, "ID", true) != 0)
				{
					qry.Append($"[{prop.Name}] = @{prop.Name.ToLower()},");
					var ptype = GetPropertyType(prop.Value.Type);
					if (ptype == null)
						args.Add(null);
					else
						args.Add(prop.Value.ToObject(ptype));
				}
			}
			qry.Remove(qry.Length - 1, 1);
			qry.Append(" where ID = @_id_");
			args.Add(data.GetValue<int>("ID"));
			return ExecuteNonQuery(qry.ToString(), args.ToArray());
		}


		/// <summary>
		/// Deletes a row in the database. The object being passed will be reflected on in order to get all the necesary data to execute the statement.
		/// </summary>
		/// <param name="entity">Object containing all the required fields to delete the row. Type must adhere to specifications given in the remarks section.</param>
		/// <returns>The number of rows affected by the update statement.</returns>
		/// <remarks>
		/// (See Insert remarks)
		/// 
		/// Also consider that the operation will fail whenever a constraint prevents the deletion of rows. Code must take all the necesary steps to grant
		/// that the record is not being referenced any more before trying to delete it.
		/// </remarks>
		public int Delete<T>(T entity) where T : class
        {
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
			//Executes the statement and return the number of affected rows.
			return ExecuteNonQuery(info.DeleteStatement(), info.GetDeleteValues(entity));
		}


		public async Task<int> DeleteAsync<T>(T entity) where T : class
        {
			EntityMetadata info;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
			//Executes the statement and return the number of affected rows.
			return await ExecuteNonQueryAsync(info.DeleteStatement(), info.GetDeleteValues(entity));
		}


		/// <summary>
		/// Deletes a row in the database. The object being passed will be used as primary key.
		/// </summary>
		/// <param name="id">The id of the row to delete, the target table must have a single column primary key whose type matches the provided object.</param>
		/// <returns>The number of rows affected by the update statement.</returns>
		/// <remarks>
		/// (See Insert remarks)
		/// 
		/// Also consider that the operation will fail whenever a constraint prevents the deletion of rows. Code must take all the necesary steps to grant
		/// that the record is not being referenced any more before trying to delete it.
		/// </remarks>
		public int Delete<T>(object id) where T : class
		{
			EntityMetadata info;
			T entity = factory.GetInstance(typeof(T)) as T;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count != 1) throw new Exception("Entity must have a primary key composed of a single column in order to use this method.");
			//Executes the statement and return the number of affected rows.
			return ExecuteNonQuery(info.DeleteStatement(), id);
		}


        public async Task<int> DeleteAsync<T>(object id) where T : class
        {
            EntityMetadata info;
            T entity = factory.GetInstance(typeof(T)) as T;
            info = cfg.GetMetadata(entity);
            if (info.PK.Count != 1) throw new Exception("Entity must have a primary key composed of a single column in order to use this method.");
            //Executes the statement and return the number of affected rows.
            return await ExecuteNonQueryAsync(info.DeleteStatement(), id);
        }



        /// <summary>
        /// Creates a new entity object and loads it with data from the database. The object passed as parameter is used to get the information required for the select statement.
        /// </summary>
        /// <typeparam name="T">The Type T of the entity that needs to be loaded. From the type of the entity the method will get the following information: the name of the target table, colums to be fetched, and which columns are part of the Primary Key (all that comes from the custom attributes used to decorate the entity class).</typeparam>
        /// <param name="entity">An instance of T, the program MUST initialize all fields and properties that are used as the Primary Key of the table (otherwise the method will not know which row to select).</param>
        /// <returns>A new entity filled with the data of the record, or null if no record is found.</returns>
        /// <remarks>
        /// Before calling this method, the fields that form part of the primary key of the entity object passed as parameter must be initialized. Example:
        /// 
        /// The following example loads all the data of the employee with id 10:
        /// 
        ///		Employee emp = new Employee();
        ///		emp.EmpID = 10;
        ///		emp = conn.SelectOne(emp);
        ///		
        /// </remarks>
        public T SelectOne<T>(T entity) where T : class
        {
            EntityMetadata info;
            info = cfg.GetMetadata(entity);
            if (info.PK.Count == 0) throw new Exception("Entity must have primary key in order to be used with the Update/Delete/SelectOne methods.");
            //Executes the statement and checks if there is anything to return.
            using (var rd = ExecuteReader(info.SelectStatement(), info.GetSelectValues(entity)))
            {
                T row = FillObject<T>(rd);
                return row;
            }
        }


		public T SelectOne<T>(params object[] pkValues) where T : class
		{
			EntityMetadata info;
			T entity = factory.GetInstance(typeof(T)) as T;
			info = cfg.GetMetadata(entity);
			if (info.PK.Count != 1) throw new Exception("Entity must have a primary key composed of a single column in order to use this method.");
			//Executes the statement and checks if there is anything to return.
            using (var rd = ExecuteReader(info.SelectStatement(), pkValues))
            {
                T row = FillObject<T>(rd);
                return row;
            }
		}


		/// <summary>
		/// Returns the fist row retrieved by the select statement as an instance of T
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="query">Query to be run.</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		public T SelectOne<T>(string query, params object[] args) where T : class
		{
            using (var rd = ExecuteReader(query, args))
            {
                T result = FillObject<T>(rd);
                return result;
            }
		}


        public async Task<T> SelectOneAsync<T>(params object[] pkvalues) where T : class
        {
            EntityMetadata info;
            T entity = factory.GetInstance(typeof(T)) as T;
            info = cfg.GetMetadata(entity);
            if (info.PK.Count != 1) throw new Exception("Entity must have a primary key composed of a single column in order to use this method.");
            //Executes the statement and checks if there is anything to return.
            using (var rd = await ExecuteReaderAsync(info.SelectStatement(), pkvalues))
            {
                T row = FillObject<T>(rd);
                return row;
            }
        }


        public async Task<T> SelectOneAsync<T>(string query, params object[] args) where T : class
		{
            using (var rd = await ExecuteReaderAsync(query, args))
            {
                T result = FillObject<T>(rd);
                return result;
            }
		}


		/// <summary>
		/// Returns the list of entities using the provided SQL statement
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="query">Query to be run.</param>
		/// <param name="args">Values of the named parameters being used in the query.</param>
		public List<T> Select<T>(string query, params object[] args) where T : class
		{
			var rd = ExecuteReader(query, args);
			return MakeList<T>(rd);
		}


		public async Task<List<T>> SelectAsync<T>(string query, params object[] args) where T : class
		{
			var rd = await ExecuteReaderAsync(query, args);
			return MakeList<T>(rd);
		}


		/// <summary>
		/// Creates a list from the first column returned by the given query.
		/// </summary>
		/// <typeparam name="T">The type of the column that will be returned</typeparam>
		/// <param name="query">Query to be run.</param>
		/// <param name="args">Values of the named parameters being used in the query.</param>
		public List<T> SelectColumn<T>(string query, params object[] args)
        {
			List<T> list = new List<T>();
			using (var rd = ExecuteReader(query, args))
			{
				var nullableType = typeof(T).IsNullable();
				if (nullableType)
				{
					while (rd.Read())
						list.Add((T)GetNullable(rd[0], null));
				}
				else
				{
					while (rd.Read())
						list.Add((T)rd[0]);
				}
			}
			return list;
		}


		/// <summary>
		/// Creates a list from the first column returned by the given query.
		/// </summary>
		/// <typeparam name="T">The type of the column that will be returned</typeparam>
		/// <param name="query">Query to be run.</param>
		/// <param name="args">Values of the named parameters being used in the query.</param>
		public async Task<List<T>> SelectColumnAsync<T>(string query, params object[] args)
		{
			List<T> list = new List<T>();
			using (var rd = await ExecuteReaderAsync(query, args))
			{
				var nullableType = typeof(T).IsNullable() || typeof(T).IsClass;
				if (nullableType)
				{
					while (await rd.ReadAsync())
						list.Add((T)GetNullable(rd[0], null));
				}
				else
				{
					while (await rd.ReadAsync())
						list.Add((T)rd[0]);
				}
			}
			return list;
		}


		/// <summary>
		/// Retrieves a page of data from the database.
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="page">Which page will be loaded.</param>
		/// <param name="pagesize">The size of the page.</param>
		/// <param name="orderBy">The name of the column by which you want to sort the results.</param>
		/// <returns>Returns a list of entities.</returns>
		public List<T> GetPage<T>(int page, int pageSize, string orderBy) where T : class
		{
			EntityMetadata info;
			T entity = factory.GetInstance(typeof(T)) as T;
			info = cfg.GetMetadata(entity);
			DbDataReader rd = null;
			try
			{
				rd = ExecuteReader(info.SelectPageStatement(info.TableName, page, pageSize, orderBy, ""));
				return MakeList<T>(rd);
			}
			finally
			{
				if (rd != null) rd.Close();
			}
		}


		/// <summary>
		/// Retrieves a page of data from the database.
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="page">Which page will be loaded.</param>
		/// <param name="pageSize">The size of the page.</param>
		/// <param name="orderBy">The name of the column by which you want to sort the results.</param>
		/// <param name="whereStatement">The conditions that returned rows must comply in order to be included in the results.</param>
		/// <param name="args">Any arguments required by the where statement</param>
		/// <returns>Returns a list of entities.</returns>
		public List<T> GetPage<T>(string table, int page, int pageSize, string orderBy, string whereStatement, params object[] args) where T : class
		{
			EntityMetadata info;
			T entity = factory.GetInstance(typeof(T)) as T;
			info = cfg.GetMetadata(entity);
			DbDataReader rd = null;
			try
			{
				rd = ExecuteReader(info.SelectPageStatement(table, page, pageSize, orderBy, whereStatement), args);
				return MakeList<T>(rd);
			}
			finally
			{
				if (rd != null) rd.Close();
			}
		}


		/// <summary>
		/// Given an DbDataReader with at least one row in it, FillObject will create a new object of type T
		/// and initialize its properties with data from the current row of the data reader.
		/// </summary>
		/// <param name="rd">DataReader with the result of a query</param>
		/// <returns>Returns an object of type T with data from the current row in the data reader. If there are no rows in the DataReader, returns null.</returns>
		public T FillObject<T>(DbDataReader rd) where T : class
		{
			EntityMetadata info;
			if (rd.Read())
			{
				T entity = factory.GetInstance(typeof(T)) as T;
				info = cfg.GetMetadata(entity);
				if (info.CopyEntityMethod == null) info.InitDynamicCode(rd, entity, typeof(List<T>));
				try
				{
					info.CopyEntityMethod.Invoke(null, BindingFlags.ExactBinding, null, new object[] { entity, rd }, null);
				}
				catch (Exception ex)
				{
					while (ex.InnerException != null) ex = ex.InnerException;
					throw ex;
				}
				return entity;
			}
			else return default(T);
		}


		/// <summary>
		/// Creates a list of entities from a data reader.
		/// </summary>
		/// <param name="rd">DataReader with the result of a query</param>
		/// <returns>List of entities</returns>
		public List<T> MakeList<T>(DbDataReader rd) where T : class
		{
			EntityMetadata info;
			object result;
			T entity = factory.GetInstance(typeof(T)) as T;
			info = cfg.GetMetadata(entity);
			if (info.MakeListMethod == null) info.InitDynamicCode(rd, entity, typeof(List<T>));
			try
			{
				result = info.MakeListMethod.Invoke(null, BindingFlags.ExactBinding, null, new object[] { rd }, null);
				return result as List<T>;
			}
			catch (Exception ex)
			{
				while (ex.InnerException != null) ex = ex.InnerException;
				throw ex;
			}
		}


		/// <summary>
		/// Allows to excecute a database script without checking for SQL injection. IMPORTANT: Use only if the script parameter is a constant string (never include user input as part of the script).
		/// </summary>
		/// <param name="script"></param>
		public void RunDBScript(string script)
		{
			DbCommand cmd = cfg.DBFactory.CreateCommand();
			if (cmd.GetType().Name != "SqlCeCommand")
				cmd.CommandTimeout = 120;
			cmd.Connection = conn;
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = script;
			cmd.ExecuteNonQuery();
		}


		/// <summary>
		/// Allows to excecute a database script without checking for SQL injection and returns a data reader with the results. IMPORTANT: Use only if the script parameter is a constant string (never include user input as part of the script).
		/// </summary>
		public DbDataReader ExecuteSQL(string script)
		{
			DbCommand cmd = cfg.DBFactory.CreateCommand();
			if (cmd.GetType().Name != "SqlCeCommand")
				cmd.CommandTimeout = 120;
			cmd.Connection = conn;
			cmd.CommandType = CommandType.Text;
			cmd.CommandText = script;
			return cmd.ExecuteReader();
		}


		//****************************************************
		// Helper methods
		//****************************************************

		public object GetNullable(object obj, object defaultValue)
		{
			if (obj is DBNull) return defaultValue;
			else return obj;
		}


		/// <summary>
		/// Verifies that the given SQL query is valid and is secure to be executed against the database.
		/// </summary>
		/// <remarks>
		/// It should not be necesary to check SQL statements with this method if DBX is used as intended, i.e.,
		/// by using named parameters instead of creating a SQL query by concatenating string.
		/// 
		/// This method is provided however as a fail-safe in case it is unavoidable to pass a query
		/// directly to the server. In such case, this method must be invoked before executing
		/// the statement.
		/// 
		/// CheckSqlInjection will impose the following limitations on the query beign executed:
		/// 
		/// > Only one sql statement is allowed by call. The only exception would be in the case
		///   of an insert, in which case it is allowed to perform a select statement to fetch
		///   the inserted record identity value (if required). With this limitation, the user 
		///   is forced to write stored procedures to perform complex operations.
		///   
		/// > Comentaries are not allowed in the estatement being executed.
		/// 
		/// > Only a subset of the available SQL statements are permited. The allowed statements are
		///   select, insert, update, delete, and calls to stored procedures.
		///   
		/// > Updates and deletes without a where clause will be considered invalid.
		///   
		/// If the query is found invalid, an exception will be thrown by the CheckSqlInjection method.
		/// </remarks>
		public void CheckSqlInjection(string query)
		{
			string lcase;
			string[] sqlStatements = { "exec ", "execute ", "select ", "insert ", "update ", "delete ", "create ", "drop ", "alter ", "kill ", "restore ", "--", "/*", "*/", "//", "#" };
			string disallowed = ",create,drop,alter,kill,restore,";

			//1: Verify query is not empty
			if (query == null) throw new Exception("SqlInjectionCheckup: Database lookup is empty.");
			lcase = query.ToLower().Trim();
			if (lcase.Length == 0) throw new Exception("SqlInjectionCheckup: Database lookup is empty.");

			//2: Check for unterminated strings.
			int idx, idx2;
			do
			{
				idx = lcase.IndexOf(cfg.StringDelimiter);
				if (idx >= 0)
				{
					idx2 = lcase.IndexOf(cfg.StringDelimiter, idx + 1);
					if (idx2 > 0)
					{
						lcase = lcase.Substring(0, idx) + lcase.Substring(idx2 + 1);
					}
					else
					{
						throw new Exception("SqlInjectionCheckup: Found unterminated string while executing a database lookup.");
					}
				}
			} while (idx >= 0);

			//3: Check that the statement being executed is allowed
			string[] tokens = lcase.Split(new char[] { ' ', ',', ';', '.' }, StringSplitOptions.RemoveEmptyEntries);
			if (disallowed.IndexOf("," + tokens[0] + ",") >= 0)
				throw new Exception("SqlInjectionCheckup: Found a statement that is not allowed while executing a database lookup (" + tokens[0] + ").");

			//4: Count the number of statement being executed
			int count = 0;
			for (int i = 0; i < sqlStatements.Length; i++)
			{
				if (lcase.IndexOf(sqlStatements[i]) >= 0) count++;
			}
			if (count > 1)
			{
				// Check the special case insert -> select
				idx = lcase.IndexOf("insert");
				if (count == 2 && idx >= 0 && lcase.IndexOf("select") > idx) return; // all good...
				else
				{
					throw new Exception("SqlInjectionCheckup. It is not allowed to: a) execute more than one statement or b) execute statements that include comments.");
				}
			}
		}


		public long GetNextValue(string sequenceName)
		{
			var result = ExecuteScalar($@"
				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{sequenceName}]') AND type in (N'SO'))
				BEGIN
				CREATE SEQUENCE [dbo].[{sequenceName}] 
				 AS [bigint]
				 START WITH 1
				 INCREMENT BY 1
				 MINVALUE 1
				 MAXVALUE 9999999
				 CYCLE 
				 CACHE 
				end
				select NEXT VALUE FOR [dbo].[{sequenceName}] as sq");
			return Convert.ToInt64(result);
		}


		public async Task<long> GetNextValueAsync(string sequenceName)
		{
			var result = await ExecuteScalarAsync($@"
				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{sequenceName}]') AND type in (N'SO'))
				BEGIN
				CREATE SEQUENCE [dbo].[{sequenceName}] 
				 AS [bigint]
				 START WITH 1
				 INCREMENT BY 1
				 MINVALUE 1
				 MAXVALUE 9999999
				 CYCLE 
				 CACHE 
				end
				select NEXT VALUE FOR [dbo].[{sequenceName}] as sq");
			return Convert.ToInt64(result);
		}


        public async Task<DbDataReader> ReadBlobAsync<T>(string fieldName, params object[] pkValues) where T : class
        {
            EntityMetadata info;
            T entity = factory.GetInstance(typeof(T)) as T;
            info = cfg.GetMetadata(entity);

            if (info.PK.Count == 0)
				throw new Exception("Entity must have a primary key in order to use this method.");

            if (!IsValidColumnName(fieldName))
                throw new InvalidOperationException($"\"{fieldName}\" is not a valid column name");

			var where = info.PK.Merge(" and ", (c) => $"[{c.CName}] = @{c.CName.ToLower()}");

            //Executes the statement and checks if there is anything to return.
            DbDataReader rd = null;
            try
            {
                rd = await ExecuteSequentialReaderAsync($"select datalength([{fieldName}]), [{fieldName}] from [{info.TableName}] where {where}", pkValues);
				if (rd.Read())
					return rd;
				else
					return null;
            }
            catch
            {
                if (rd != null)
                    rd.Dispose();
                throw;
            }
        }


        private bool IsValidColumnName(string fieldName)
        {
            foreach(var c in fieldName)
            {
                if (!Char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }
    }


	class DBXTransaction: IDBXTransaction
	{
		internal DbTransaction tx;
		private Action<DbTransaction> completed;
		private bool commited = false;

		internal DBXTransaction(DbTransaction tx, Action<DbTransaction> completed)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			if (completed == null)
				throw new ArgumentNullException(nameof(completed));
			this.tx = tx;
			this.completed = completed;
		}

		public void Commit()
		{
			commited = true;
			tx.Commit();
		}

		public void Dispose()
		{
			if(!commited)
				tx.Rollback();
			tx.Dispose();
			completed(tx);
		}
	}
}