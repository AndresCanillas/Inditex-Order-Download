using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Database
{
	/// <summary>
	/// Allows to create database connection objects from a given configuration.
	/// </summary>
	public interface IDBConfiguration
	{
		/// <summary>
		/// Gets the database configuration name
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Allows the user to get the name of the data provider used to connect to the target database.
		/// </summary>
		string ProviderName { get; set; }

		/// <summary>
		/// Allows to get the connection string used to connect to the target database.
		/// </summary>
		string ConnectionString { get; set; }

		/// <summary>
		/// Retrieves configuration from the specified database name and setups the provider and connection string values.
		/// </summary>
		/// <param name="databaseName">The name of the database connection to search for in the appsettings.json file</param>
		void Configure(string databaseName);

		/// <summary>
		/// Retrieves the name of the database we are connecting to
		/// </summary>
		/// <returns></returns>
		string GetInitialCatalog();

		/// <summary>
		/// Determines if the database exists in the configured data soruce or not
		/// </summary>
		bool DatabaseExists { get; }

		/// <summary>
		/// Checks to see if the database exists, and if it does not exist creates and empty database.
		/// </summary>
		void EnsureCreated();

        /// <summary>
        /// Checks to see if the database exists, and if it does not exist creates and empty database.
        /// </summary>
        Task EnsureCreatedAsync();

        /// <summary>
        /// Checks to see if the database exists, and if it does not exist creates and empty database and then invokes the specified callback, passing an open connection to the database and a flag indicating if the database was created new or already existed.
        /// </summary>
        Task EnsureCreatedAsync(Action<IDBX, bool> initializationCallback);

        /// <summary>
        /// Creates a new IDBX object using this database configuration
        /// </summary>
        IDBX CreateConnection();

        /// <summary>
        /// Creates a new IDBX object using this database configuration
        /// </summary>
        Task<IDBX> CreateConnectionAsync();
	}
}
