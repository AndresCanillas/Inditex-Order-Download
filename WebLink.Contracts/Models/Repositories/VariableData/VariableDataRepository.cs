using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using WebLink.Contracts;

namespace WebLink.Contracts.Models
{
	public class VariableDataRepository : IVariableDataRepository
	{
		private IFactory factory;
		private IAppConfig config;
		private IProjectRepository projectRepo;
		
		public VariableDataRepository(
			IFactory factory,
			IAppConfig config,
			IProjectRepository projectRepo
			)
		{
			this.factory = factory;
			this.config = config;
			this.projectRepo = projectRepo;
		}


		public IVariableData GetByID(int projectid, int productid, bool removeIds)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByID(ctx, projectid, productid, removeIds);
			}
		}


		public IVariableData GetByID(PrintDB ctx, int projectid, int productid, bool removeIds)
		{
			var userData = factory.GetInstance<IUserData>();
			if (projectid == 0) projectid = userData.SelectedProjectID;
			var project = projectRepo.GetByID(ctx, projectid); // Ensures the user can access the specified project
			var catalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == "VariableData").Single();
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);
				Dictionary<string, string> data = dynamicDB.FlattenObject(catalog.CatalogID, removeIds, new NameValuePair("ID", productid));
				if (data == null || data.Count == 0)
					throw new Exception($"Could not find product with barcode {productid}.");
				return new VariableData(data);
			}
		}


		public IVariableData GetByBarcode(int projectid, string barcode, bool removeIds)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByBarcode(ctx, projectid, barcode, removeIds);
			}
		}


		public IVariableData GetByBarcode(PrintDB ctx, int projectid, string barcode, bool removeIds)
		{
			var userData = factory.GetInstance<IUserData>();
			if (projectid == 0) projectid = userData.SelectedProjectID;
			var project = projectRepo.GetByID(ctx, projectid); // Ensures the user can access the specified project
			var catalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == "VariableData").Single();
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);
				Dictionary<string, string> data = dynamicDB.FlattenObject(catalog.CatalogID, removeIds, new NameValuePair("Barcode", barcode));
				if (data == null || data.Count == 0)
					throw new Exception($"Could not find product with barcode {barcode}.");
				return new VariableData(data);
			}
		}


		public IVariableData GetByDetailID(int projectid, int detailid, bool removeIds)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetByDetailID(ctx, projectid, detailid, removeIds);
			}
		}


		public IVariableData GetByDetailID(PrintDB ctx, int projectid, int detailid, bool removeIds)
		{
			var userData = factory.GetInstance<IUserData>();
			if (projectid == 0) projectid = userData.SelectedProjectID;
			var project = ctx.Projects.FirstOrDefault(x => x.ID == projectid);
			var catalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == Catalog.VARIABLEDATA_CATALOG).Single();
            var detailCatalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == Catalog.ORDERDETAILS_CATALOG).Single();

            using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);


                JObject detaildata =  dynamicDB.SelectOne(detailCatalog.CatalogID, detailid);

                Dictionary<string, string> data = dynamicDB.FlattenObject(catalog.CatalogID, removeIds, new NameValuePair("ID", detaildata.GetValue<int>("Product")));
				if (data == null || data.Count == 0)
					throw new System.Exception($"Could not find product with barcode {detailid}.");
				return new VariableData(data);
			}
		}

		public IVariableData GetProductDataFromDetail(int projectid, int detailid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetProductDataFromDetail(ctx, projectid, detailid);
			}
		}

		public IVariableData GetProductDataFromDetail(PrintDB ctx, int projectid, int detailid)
		{
			var userData = factory.GetInstance<IUserData>();
			if (projectid == 0) projectid = userData.SelectedProjectID;
			var project = projectRepo.GetByID(ctx, projectid); // Ensures the user can access the specified project
			var detailsCatalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == "OrderDetails").Single();
			var variableDataCatalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == "VariableData").Single();
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);
				var det = dynamicDB.SelectOne(detailsCatalog.CatalogID, detailid);
				Dictionary<string, string> data = dynamicDB.FlattenObject(variableDataCatalog.CatalogID, false, new NameValuePair("ID", det.GetValue<int>("Product")));
				if (data == null || data.Count == 0)
					throw new System.Exception($"Could not find product with barcode {detailid}.");
				return new VariableData(data);
			}
		}

		public IEnumerable<IVariableData> GetAllByDetailID(int projectid, bool removeIds, bool showDetailId, params int[] ids)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				return GetAllByDetailID(ctx, projectid, removeIds, showDetailId, ids);
			}
		}

		public IEnumerable<IVariableData> GetAllByDetailID(PrintDB ctx, int projectid, bool removeIds, bool showDetailId, params int[] ids)
		{
			var userData = factory.GetInstance<IUserData>();
			if (projectid == 0) projectid = userData.SelectedProjectID;
			var project = ctx.Projects.FirstOrDefault(x => x.ID == projectid);
			var detailCatalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == "OrderDetails").Single();
			var productCatalog = ctx.Catalogs.Where(c => c.ProjectID == projectid && c.Name == "VariableData").Single();
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);
				List<Dictionary<string, string>> data = dynamicDB.FlattenObjectsByIds(detailCatalog.CatalogID, productCatalog.CatalogID, removeIds, showDetailId, ids).ToList();
				//if (data == null || data.Count == 0)
				//	throw new System.Exception($"Could not find product with barcode {detailid}.");
				//var x = new VariableData(data);
				return data.Select(s => new VariableData(s));
			}
		}
	}
}
