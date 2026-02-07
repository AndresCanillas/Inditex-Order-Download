using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebLink.Contracts
{
	public interface IDynamicCatalogCollection: IEnumerable<IDynamicCatalog>
	{
		int Count { get; }                                  // Retrieves the number of catalogs in this project
		IDynamicCatalog this[int index] { get; }            // Gets a catalog based of its index
		IDynamicCatalog this[string catalogName] { get; }   // Gets a catalog based of its name
		IDynamicCatalog GetByID(int catalogid);
	}

	public interface IDynamicCatalog
	{
		int ID { get; }
		string Name { get; }
		IDynamicCatalogColumnCollection Columns { get; }
		int RowCount { get; }
		IEnumerable<IDynamicCatalogRow> Select();
		IEnumerable<IDynamicCatalogRow> Select(string query, params object[] args);
		IDynamicCatalogRow GetByID(int id);
		IDynamicCatalogRow Create();
		void Insert(IDynamicCatalogRow row);
		IDynamicCatalogRow Insert(string jsonData);
		void Update(IDynamicCatalogRow row);
		void Update(string jsonData);
		void Delete(int id);
	}

	public interface IDynamicCatalogColumnCollection: IEnumerable<IDynamicCatalogColumn>
	{
		int Count { get; }
		IDynamicCatalogColumn this[int index] { get; }
		IDynamicCatalogColumn this[string columnName] { get; }
	}

	public interface IDynamicCatalogColumn
	{
		string Name { get; }
		ColumnType Type { get; }
		bool CanBeEmpty { get; }
	}

	public interface IDynamicCatalogRowCollection : IEnumerable<IDynamicCatalogRow>
	{
		int Count { get; }
		IDynamicCatalogRow this[int index] { get; }
	}

	public interface IDynamicCatalogRow
	{
		IDynamicCatalog Catalog { get; }                // Reference to the catalog from which this row was retrieved
		string Data { get; set; }						// Gets or sets the data of this row as Json
		string this[string columnName] { get; }         // Gets the value of the specified column converted to a string. Use GetValue<T>/SetValue<T> if you know the data type of the column.
		T GetValue<T>(string columnName);               // Retrieves the value of a column, you need to know the data type of the column.
		void SetValue<T>(string columnName, T value);   // Sets the value of a column, you need to know the data type of the column.
	}

	public interface IDynamicCatalogRef
	{
		IDynamicCatalog Catalog { get; }                // Gets the information of the catalog referenced by this field (right side of the relation)
		IDynamicCatalogRow Data { get; set; }           // The data of the referenced record (or null if the reference is not set). NOTE: If set to a new record (i.e. the ID is 0), then the record will first be inserted in the respective catalogm then the reference to the newly inserted record will be set. Can be set to null.
	}

	public interface IDynamicCatalogSet
	{
		IDynamicCatalog Catalog { get; }                // Gets the information of the catalog used to compose this set (right side of the relation)
		int Count { get; }                              // Retrieves the number of records in the set (executes a select count(*) from the relation table)
		IEnumerable<IDynamicCatalogRow> GetList();      // Gets all the records that are part of this set
		IDynamicCatalogRow Add(IDynamicCatalogRow row); // Adds the specified row as part of this set. IMPORTANT: If the row passed as argument is a new record (i.e. its ID is 0), then the row will be inserted in the respective catalog then the reference will be added to this set.
		IDynamicCatalogRow Add(int id);                 // Gets the record with the given ID form the catalog (right side of the relation), and then adds that record as part of this set.
		void Remove(IDynamicCatalogRow row);            // Removes the specified record from the set
		void Remove(int id);                            // Removes the specified record from the set using the ID of the record.
	}

	public interface IDynamicCatalogFile
	{
		int ID { get; }
		int Length { get; }
		string FileName { get; set; }
		void SetContent(Stream stream);
		void SetContent(byte[] bytes);
		Stream GetContentAsStream();
		byte[] GetContentAsBytes();
	}
}
