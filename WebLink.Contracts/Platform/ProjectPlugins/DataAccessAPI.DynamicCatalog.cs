using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class DynamicCatalog : IDynamicCatalog
	{
		public static IDynamicCatalog Create(IFactory factory, ICatalog definition)
		{
			var catalog = new DynamicCatalog(factory);
			catalog.definition = definition;
			catalog.columns = DynamicCatalogColumnCollection.Create(definition);
			catalog.config = factory.GetInstance<IAppConfig>();
			return catalog;
		}

		private IFactory factory;
		private ICatalog definition;
		private IDynamicCatalogColumnCollection columns;
		private IAppConfig config;
		private string connstr;

		private DynamicCatalog(IFactory factory)
		{
			this.factory = factory;
			config = factory.GetInstance<IAppConfig>();
			connstr = config["Databases.CatalogDB.ConnStr"];
		}

		public int ID { get { return definition.ID; } }

		public string Name { get { return definition.Name; } }

		public IDynamicCatalogColumnCollection Columns { get { return columns; } }

		public int RowCount
		{
			get
			{
				using (var db = factory.GetInstance<DynamicDB>())
				{
					db.Open(connstr);
					return db.GetRowCount(definition.CatalogID);
				}
			}
		}

		public IEnumerable<IDynamicCatalogRow> Select()
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				db.Open(connstr);
				var data = db.Select(definition.CatalogID);
				return DynamicCatalogRowCollection.Create(this, data);
			}
		}

		public IEnumerable<IDynamicCatalogRow> Select(string query, params object[] args)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				db.Open(connstr);
				var data = db.Select(definition.CatalogID, query, args);
				return DynamicCatalogRowCollection.Create(this, data);
			}
		}

		public IDynamicCatalogRow GetByID(int id)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				db.Open(connstr);
				var data = db.SelectOne(definition.CatalogID, id);
				return new DynamicCatalogRow(this, data);
			}
		}

		public IDynamicCatalogRow Create()
		{
			return new DynamicCatalogRow(this, "{ \"ID\": 0 }");
		}

		public void Insert(IDynamicCatalogRow row)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				db.Open(connstr);
				row.Data = db.Insert(definition.CatalogID, row.Data);
			}
		}

		public IDynamicCatalogRow Insert(string jsonData)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				var row = Create();
				db.Open(connstr);
				row.Data = db.Insert(definition.CatalogID, jsonData);
				return row;
			}
		}

		public void Update(IDynamicCatalogRow row)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				db.Open(connstr);
				db.Update(definition.CatalogID, row.Data);
			}
		}

		public void Update(string jsonData)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				db.Open(connstr);
				db.Update(definition.CatalogID, jsonData);
			}
		}

		public void Delete(int id)
		{
			using (var db = factory.GetInstance<DynamicDB>())
			{
				var row = Create();
				db.Open(connstr);
				db.Delete(definition.CatalogID, id, null);
			}
		}
	}


	public class DynamicCatalogColumnCollection : IDynamicCatalogColumnCollection
	{
		public static IDynamicCatalogColumnCollection Create(ICatalog catalog)
		{
			var collection = new DynamicCatalogColumnCollection();
			var fields = catalog.Fields;
			foreach (var f in fields)
				collection.columns.Add(new DynamicCatalogColumn(f.Name, f.Type, f.CanBeEmpty));
			return collection;
		}

		private List<IDynamicCatalogColumn> columns = new List<IDynamicCatalogColumn>();

		private DynamicCatalogColumnCollection() { }

		public int Count
		{
			get
			{
				return columns.Count;
			}
		}

		public IDynamicCatalogColumn this[int index] { get { return columns[index]; } }

		public IDynamicCatalogColumn this[string columnName]
		{
			get
			{
				var index = columns.FindIndex(p => String.Compare(p.Name, columnName, true) == 0);
				if (index < 0) throw new Exception($"Cannot find column named '{columnName}'");
				return columns[index];
			}
		}

		public IEnumerator<IDynamicCatalogColumn> GetEnumerator()
		{
			for (int i = 0; i < columns.Count; i++)
				yield return columns[i];
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}


	public class DynamicCatalogColumn : IDynamicCatalogColumn
	{
		private string name;
		private ColumnType type;
		private bool canBeEmpty;

		public DynamicCatalogColumn(string name, ColumnType type, bool canBeEmpty)
		{
			this.name = name;
			this.type = type;
			this.canBeEmpty = canBeEmpty;
		}

		public string Name { get { return name; } }
		public ColumnType Type { get { return type; } }
		public bool CanBeEmpty { get { return canBeEmpty; } }
	}
}
