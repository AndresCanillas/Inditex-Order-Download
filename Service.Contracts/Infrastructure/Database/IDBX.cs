using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
	/// <summary>
	/// Contract used to interact with a database. This interface provides the basic set of functions provided by the database library as well as functions related to entities.
	/// </summary>
	public interface IDBX : IDisposable
	{
		/// <summary>
		/// Gets the data provider used to access the database.
		/// </summary>
		string Provider { get; }

		/// <summary>
		/// Allows to get the connection string used to connect to this database
		/// </summary>
		string ConnectionString { get; }

		/// <summary>
		/// Allows to get or set the timeout used when executing any database query.
		/// </summary>
		int CommandTimeout { get; set; }

		/// <summary>
		/// Executes the specified query and returns true if data is returned, false otherwise.
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		bool Exists(string query, params object[] args);

		Task<bool> ExistsAsync(string query, params object[] args);

		/// <summary>
		/// Starts a new transaction
		/// </summary>
		IDBXTransaction BeginTransaction();

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
		DbDataReader ExecuteReader(string query, params object[] args);

		/// <summary>
		/// Executes a user provided query and returns a DataSet with the results
		/// </summary>
		/// <param name="query">Query to be executed.</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		/// <returns>The result of the query as a DataSet.</returns>
		/// <remarks>Same remarks as with ExecuteReader above.</remarks>
		DataSet ExecuteQuery(string query, params object[] args);

		/// <summary>
		/// Execute a user provided query that only returns the number of rows affected (insert/update/delete statements)
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		/// <returns>Returns the number of rows affected by the sql statement.</returns>
		/// <remarks>Same remarks as with ExecuteReader above.</remarks>
		int ExecuteNonQuery(string query, params object[] args);

		/// <summary>
		/// Execute a query that only returns a single value (the first column of the first row).
		/// </summary>
		/// <param name="query">Query to be run. This query must make use of named parameters if parameters are necesary (see remarks).</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		/// <returns>Returns the scalar value returned by the statement.</returns>
		/// <remarks>Same remarks as with ExecuteReader above.</remarks>
		object ExecuteScalar(string query, params object[] args);

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
		DbDataReader ExecuteSP(string query, params object[] args);


		/// <summary>
		/// Executes a stored procedure. Allows to send the actual parameters used when executing the stored procedure, this lets the programmer access Output parameters if required.
		/// </summary>
		DbDataReader ExecuteSP(string query, params DbParameter[] args);

        /// <summary>
        /// Inserts a new row in the database. The object being passed will be reflected on in order to get all the necessary data to build and execute the sql statement.
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
        ///			[TargetTable("tbl_Personel")]
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
        ///			[TargetTable("tbl_Personel")]
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
        object Insert<T>(T entity) where T : class;

        /// <summary>
        /// Inserts data into the specified table using Json as the source
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="data">Data to be inserted in the table</param>
        void InsertJson(string tableName, JObject data);

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
        int Update<T>(T entity) where T : class;

        /// <summary>
        /// Updates data into the specified table using Json as the source
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="data">Data to be updated in the table</param>
        int UpdateJson(string tableName, JObject data);

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
        int Delete<T>(T entity) where T : class;

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
        int Delete<T>(object id) where T : class;

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
		/// </remarks>
		T SelectOne<T>(T entity) where T : class;

		/// <summary>
		/// Creates a new entity object and loads it with data from the database. The integer passed as parameter is used to get the information required for the select statement.
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded. From the type of the entity the method will get the following information: the name of the target table, colums to be fetched, and which columns are part of the Primary Key (all that comes from the custom attributes used to decorate the entity class).</typeparam>
		/// <param name="id">The id of the row to retrieve, required the entity to have a primary key that is also int.</param>
		/// <returns>A new entity filled with the data of the record, or null if no record is found.</returns>
		/// <remarks>
		/// Before calling this method, the fields that form part of the primary key of the entity object passed as parameter must be initialized. Example:
		/// 
		/// The following example loads all the data of the employee with id 10:
		/// 
		///		emp = conn.SelectOne(10);
		/// </remarks>
		T SelectOne<T>(params object[] pkValues) where T : class;

		/// <summary>
		/// Returns the fist row retrieved by the select statement as an instance of T
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="query">Query to be run.</param>
		/// <param name="args">Values of the named parameters being passed to the database.</param>
		T SelectOne<T>(string query, params object[] args) where T : class;

		/// <summary>
		/// Returns the list of entities using the provided SQL statement
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="query">Query to be run.</param>
		/// <param name="args">Values of the named parameters being used in the query.</param>
		List<T> Select<T>(string query, params object[] args) where T : class;

        /// <summary>
        /// Creates a list from the first column returned by the given query.
        /// </summary>
        /// <typeparam name="T">The type of the column that will be returned</typeparam>
        /// <param name="query">Query to be run.</param>
        /// <param name="args">Values of the named parameters being used in the query.</param>
        List<T> SelectColumn<T>(string query, params object[] args);

		Task<List<T>> SelectColumnAsync<T>(string query, params object[] args);

		/// <summary>
		/// Retrieves a page of data from the database.
		/// </summary>
		/// <typeparam name="T">The Type T of the entity that needs to be loaded.</typeparam>
		/// <param name="page">Which page will be loaded.</param>
		/// <param name="pageSize">The size of the page.</param>
		/// <returns>Returns a list of entities.</returns>
		List<T> GetPage<T>(int page, int pageSize, string orderBy) where T : class;

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
		List<T> GetPage<T>(string table, int page, int pageSize, string orderBy, string whereStatement, params object[] args) where T : class;

		/// <summary>
		/// Given an DataReader with at least one row in it, FillObject will create a new object of type T
		/// and initialize its properties with data from the current row of the data reader.
		/// </summary>
		/// <param name="rd">DataReader with the result of a query</param>
		/// <returns>Returns an object of type T with data from the current row in the data reader. If there are no rows in the DataReader, returns null.</returns>
		T FillObject<T>(DbDataReader rd) where T : class;

		/// <summary>
		/// Creates a list of entities from a data reader.
		/// </summary>
		/// <param name="rd">DataReader with the result of a query</param>
		/// <returns>List of entities</returns>
		List<T> MakeList<T>(DbDataReader rd) where T : class;

		/// <summary>
		/// Helper method used to retrieve the value of a field that can be null.
		/// </summary>
		/// <param name="obj">The field from the database.</param>
		/// <param name="defaultValue">A default value in case the field is null.</param>
		/// <returns>Returns the value of the field, or the default value if the field is null.</returns>
		object GetNullable(object obj, object defaultValue);

		/// <summary>
		/// Allows to excecute a database script without checking for SQL injection. IMPORTANT: Use only if the script parameter is a constant string (never include user input as part of the script).
		/// </summary>
		void RunDBScript(string script);

		/// <summary>
		/// Allows to excecute a database script without checking for SQL injection and returns a data reader with the results. IMPORTANT: Use only if the script parameter is a constant string (never include user input as part of the script).
		/// </summary>
		DbDataReader ExecuteSQL(string script);

		/// <summary>
		/// Reads all the data in the data reader and fills a dataset with it.
		/// </summary>
		DataSet ReaderToDataSet(DbDataReader rd);

		void BulkInsert<T>(List<T> data);
		void BulkInsert(string table, string dataFile);
		void ExportToCsv<T>(string fileName, List<T> data, bool includeHeader, string fields = null);

		/// <summary>
		/// Executes the specified query and returns the data of the rows as json
		/// </summary>
		JObject SelectOneToJson(string query, params object[] args);
		/// <summary>
		/// Executes the specified query and returns the data of the rows as json
		/// </summary>
		JArray SelectToJson(string query, params object[] args);

		long GetNextValue(string sequenceName);


		Task OpenAsync();
		Task<DbDataReader> ExecuteReaderAsync(string query, params object[] args);
		Task<int> ExecuteNonQueryAsync(string query, params object[] args);
		Task<object> ExecuteScalarAsync(string query, params object[] args);
		Task<object> InsertAsync<T>(T entity) where T : class;
		Task<int> UpdateAsync<T>(T entity) where T : class;
		Tuple<string, List<DbParameter>> GetUpdateStatement<T>(T entity) where T : class;
		Task<int> DeleteAsync<T>(T entity) where T : class;
        Task<int> DeleteAsync<T>(object id) where T : class;
        Task<T> SelectOneAsync<T>(params object[] pkValues) where T : class;
        Task<T> SelectOneAsync<T>(string query, params object[] args) where T : class;
		Task<List<T>> SelectAsync<T>(string query, params object[] args) where T : class;
		Task<long> GetNextValueAsync(string sequenceName);
		Task<DbDataReader> ReadBlobAsync<T>(string fieldName, params object[] pkValues) where T : class;
    }


	public interface IDBXTransaction : IDisposable
	{
		void Commit();
	}
}
