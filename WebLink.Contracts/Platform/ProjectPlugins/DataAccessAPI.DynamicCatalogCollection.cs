using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public class DynamicCatalogCollection : IDynamicCatalogCollection
	{
		public static IDynamicCatalogCollection Create(IFactory factory, int projectid)
		{
			var repo = factory.GetInstance<ICatalogRepository>();
			var collection = new DynamicCatalogCollection(factory);
			collection.definitions = repo.GetByProjectID(projectid);
			collection.catalogs = new List<IDynamicCatalog>();
			for (int i = 0; i < collection.definitions.Count; i++)
				collection.catalogs.Add(null);
			return collection;
		}

		private IFactory factory;
		private List<ICatalog> definitions;
		private List<IDynamicCatalog> catalogs;

		private DynamicCatalogCollection(IFactory factory)
		{
			this.factory = factory;
		}

		public int Count
		{
			get { return definitions.Count; }
		}

		public IDynamicCatalog this[int index]
		{
			get
			{
				if (catalogs[index] != null) return catalogs[index];
				var catalog = DynamicCatalog.Create(factory, definitions[index]);
				catalogs[index] = catalog;
				return catalog;
			}
		}

		public IDynamicCatalog this[string catalogName]
		{
			get
			{
				int idx = catalogs.FindIndex(p => String.Compare(p.Name, catalogName, true) == 0);
				if (idx < 0) throw new Exception($"Could not find a catalog named '{catalogName}'");
				return this[idx];
			}
		}

		public IDynamicCatalog GetByID(int catalogid)
		{
			return catalogs.FirstOrDefault(p => p.ID == catalogid);
		}

		public IEnumerator<IDynamicCatalog> GetEnumerator()
		{
			for(int i = 0; i < definitions.Count; i++)
				yield return this[i];
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
