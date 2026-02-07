using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
	public interface ICatalogPluginManager
	{
		void Start();
		List<ICatalogPluginInfo> GetByCompanyID(int companyid);
		List<ICatalogPluginInfo> GetByBrandID(int brandid);
		List<ICatalogPluginInfo> GetByProjectID(int projectid);
		List<ICatalogPluginInfo> GetByCatalogID(int catalogid);
		List<ICatalogPluginInfo> GetByDataCatalogID(int datacatalogid);
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class CatalogPluginTargetAttribute: Attribute
	{
		public string Company { get; set; }
		public string Brand { get; set; }
		public string Project { get; set; }
		public string Catalog { get; set; }

		public CatalogPluginTargetAttribute(string company, string brand, string project, string catalog)
		{
			Company = company;
			Brand = brand;
			Project = project;
			Catalog = catalog;
		}
	}

	public abstract class CatalogPlugin
	{
		public virtual void BeforeInsert(DynamicDBBeforeInsert e) { }
		public virtual void BeforeUpdate(DynamicDBBeforeUpdate e) { }
		public virtual void BeforeDelete(DynamicDBBeforeDelete e) { }
	}

	public interface ICatalogPluginInfo
	{
		int CompanyID { get; }
		string CompanyName { get; }
		int BrandID { get; }
		string BrandName { get; }
		int ProjectID { get; }
		string ProjectName { get; }
		int CatalogID { get; }
		string CatalogName { get; }
		int DataCatalogID { get; }
		string PluginName { get; }
		string PluginDescription { get; }
		bool ImplementsOnInsert { get; }
		string OnInsertDescription { get; }
		bool ImplementsOnUpdate { get; }
		string OnUpdateDescription { get; }
		bool ImplementsOnDelete { get; }
		string OnDeleteDescription { get; }
	}
}
