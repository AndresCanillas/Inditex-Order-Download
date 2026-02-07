using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
	public class DynamicCatalogRowCollection: IDynamicCatalogRowCollection
	{
		public static IDynamicCatalogRowCollection Create(IDynamicCatalog catalog, JArray data)
		{
			var collection = new DynamicCatalogRowCollection();
			collection.catalog = catalog;
			foreach (JToken t in data)
				collection.list.Add(new DynamicCatalogRow(catalog, t as JObject));
			return collection;
		}

		private IDynamicCatalog catalog;
		private List<IDynamicCatalogRow> list = new List<IDynamicCatalogRow>();

		private DynamicCatalogRowCollection() { }
		public int Count { get { return list.Count; } }
		public IDynamicCatalogRow this[int index] { get { return list[index]; } }

		public IEnumerator<IDynamicCatalogRow> GetEnumerator()
		{
			for (int i = 0; i < list.Count; i++)
				yield return list[i];
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}







	public class DynamicCatalogRow: IDynamicCatalogRow
	{
		private IDynamicCatalog catalog;
		private JObject data;

		public DynamicCatalogRow(IDynamicCatalog catalog, string data)
		{
			this.catalog = catalog;
			this.data = JObject.Parse(data);
		}

		public DynamicCatalogRow(IDynamicCatalog catalog, JObject data)
		{
			this.catalog = catalog;
			this.data = data;
		}

		public IDynamicCatalog Catalog { get { return catalog; } }

		public string Data
		{
			get
			{
				return data.ToString();
			}
			set
			{
				data = JObject.Parse(value);
			}
		}

		public string this[string columnName]
		{
			get
			{
				return data.Value<string>(columnName);
			}
		}

		public T GetValue<T>(string columnName)
		{
			return data.Value<T>(columnName);
		}

		public void SetValue<T>(string columnName, T value)
		{
			data.Add(columnName, JToken.FromObject(value));
		}
	}
}
