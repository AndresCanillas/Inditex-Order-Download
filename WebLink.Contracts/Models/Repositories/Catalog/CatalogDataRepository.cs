using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace WebLink.Contracts.Models
{
    public class CatalogDataRepository : ICatalogDataRepository
	{
		private IFactory factory;
		private ICatalogRepository catalogRepo;
		private ICompanyRepository companyRepo;
		private IAppConfig config;
		private string connstr;
		private IEventQueue events;
        
        private  IConnectionManager connManager;
        public CatalogDataRepository(
            IFactory factory,
            ICatalogRepository catalogRepo,
            ICompanyRepository companyRepo,
            IAppConfig config,
            IEventQueue events,
            
            IConnectionManager connManager
            )
        {
            this.factory = factory;
            this.catalogRepo = catalogRepo;
            this.companyRepo = companyRepo;
            this.config = config;
            connstr = config["Databases.CatalogDB.ConnStr"];
            this.events = events;
            
            this.connManager = connManager;
        }

        public string GetByID(int catalogid, int id)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.SelectOne(cat.CatalogID, id).ToString();
			}
		}

		public string GetList(int catalogid)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.Select(cat.CatalogID).ToString();
			}
		}

        public int  GetListCount ( int catalogid)
        {
            //var cat = catalogRepo.GetByCatalogID(catalogid);
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                //return dynamicDB.Select(cat.CatalogID).Count();
                return dynamicDB.GetRowCount(catalogid); 
            }
        }

        public string GetListByPage (int catalogid, int pagenumber, int pagesize)
        {
            var cat = catalogRepo.GetByCatalogID(catalogid);
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                return dynamicDB.Select(cat.CatalogID, pagenumber, pagesize).ToString();
            }
        }

        public List<TableData> ExportData(int projectid, string catalogName, bool recursive, params NameValuePair[] filter)
		{
			var cat = catalogRepo.GetByName(projectid, catalogName);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.ExportData(cat.CatalogID, recursive, filter);
			}
		}

		public List<TableData> ExportData(int catalogid, bool recursive, params NameValuePair[] filter)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.ExportData(cat.CatalogID, recursive, filter);
			}
		}

		public string SearchFirst(int catalogid, string fieldName, string value)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			var fields = cat.Fields;
			var field = fields.FirstOrDefault(f => f.Name == fieldName);
			if (field == null)
				throw new Exception($"Field {field} does not exist in catalog {cat.Name}.");
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.SelectOne(cat.CatalogID, $"select * from #TABLE where CHARINDEX(@value, {fieldName}) > 0", value).ToString();
			}
		}

		public string FreeTextSearch(int catalogid, params TextSearchFilter[] filter)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.FreeTextSearch(cat.CatalogID, filter).ToString();
			}
		}

        public string GetBaseDataFromOrderId(int projectid,  IOrder order)
        {

            var allCatalogs = catalogRepo.GetByProjectID(projectid);
            var orderCt = allCatalogs.Single(s => s.Name == Catalog.ORDER_CATALOG);
            var detailCt = allCatalogs.Single(s => s.Name == Catalog.ORDERDETAILS_CATALOG);
            var varDataCt = allCatalogs.Single(s => s.Name == Catalog.VARIABLEDATA_CATALOG);
            var baseDataCt = allCatalogs.Single(s => s.Name == Catalog.BASEDATA_CATALOG);


            var relField = orderCt.Fields.Single(s => s.Name == "Details");


            using(var dynamicDB = connManager.OpenDB("CatalogDB"))
            {
                var baseData = dynamicDB.SelectToJson(
                    $@"SELECT l.*
                    FROM {orderCt.TableName} o
                    INNER JOIN REL_{orderCt.CatalogID}_{detailCt.CatalogID}_{relField.FieldID} as r1 ON o.ID = r1.SourceID
                    INNER JOIN {detailCt.TableName} d ON d.ID = r1.TargetID
                    INNER JOIN {varDataCt.TableName} v ON v.ID = d.Product
                    INNER JOIN {baseDataCt.TableName} l ON v.IsBaseData = l.ID
                    WHERE o.ID = @orderID",
                    order.OrderDataID);
                return baseData.ToString();

            }

        }

        public int FreeTextSearchCount(int catalogid, params TextSearchFilter[] filter)
        {
            var cat = catalogRepo.GetByCatalogID(catalogid);
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                
                return dynamicDB.FreeTextSearchCount(cat.CatalogID, filter);
            }
        }

        public string FreeTextSearch(int catalogid, int pageNumber, int pageSize, params TextSearchFilter[] filter)
        {
            var cat = catalogRepo.GetByCatalogID(catalogid);
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                return dynamicDB.FreeTextSearch(cat.CatalogID, pageNumber, pageSize, filter).ToString();
            }
        }


        public string SubsetFreeTextSearch(int catalogid, int id, string fieldName, params TextSearchFilter[] filter)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.SubsetFreeTextSearch(cat.CatalogID, id, fieldName, filter).ToString();
			}
		}

		public string SearchMultiple(int catalogid, List<string> barcodes)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.SearchMultiple(cat.CatalogID, "Barcode", barcodes).ToString();
			}
		}

		public string Insert(int catalogid, string json)
		{
			var userData = factory.GetInstance<IUserData>();
			var cat = catalogRepo.GetByCatalogID(catalogid);
			if (cat.IsReadonly && !userData.IsIDT)
				throw new Exception("Not authorized");
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				var result = dynamicDB.Insert(cat.CatalogID, json);
				return result;
			}
		}

		public string Update(int catalogid, string json)
		{
			var userData = factory.GetInstance<IUserData>();
			var cat = catalogRepo.GetByCatalogID(catalogid);
			if (cat.IsReadonly && !userData.IsIDT)
				throw new Exception("Not authorized");

			var company = companyRepo.GetProjectCompany(cat.ProjectID);
			#region detect changes

			int id = GetObjectID(json);
            // this open another database connections
            var current = GetByID(catalogid, id);
            var fields = cat.Fields.ToList();
            if (IsRowDataUpdated(current, json, fields))
			{
				events.Send(new CatalogDataUpdatedEvent()
				{
					CompanyID = company.ID,
					CatalogID = catalogid,
					CatalogName = cat.Name,
					ProjectID = cat.ProjectID,
					RowID = id,
					JsonDataBeforeUpdate = current,
					JsonDataAfterUpdate = json
				});
			}

			#endregion detect changes


			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				dynamicDB.Update(cat.CatalogID, json);
				return json;
			}
		}

		public void Delete(int catalogid, int id, int leftCatalogId,int? parentId = null)
		{
			var userData = factory.GetInstance<IUserData>();
			var cat = catalogRepo.GetByCatalogID(catalogid);
			if (cat.IsReadonly && !userData.IsIDT)
				throw new Exception("Not authorized");
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
                CatalogDefinition parentCatalog = null;
                if (leftCatalogId > 0)
                {                    
                    var parent = catalogRepo.GetByCatalogID(leftCatalogId);
                    parentCatalog = new CatalogDefinition();
                    parentCatalog.ID = parent.CatalogID;
                    parentCatalog.Definition = parent.Definition;
                }

                dynamicDB.Open(connstr);
				dynamicDB.Delete(cat.CatalogID, id, parentCatalog, parentId);
			}
		}

		public void DeleteAll(int catalogid)
		{
			var userData = factory.GetInstance<IUserData>();
			var cat = catalogRepo.GetByCatalogID(catalogid);
			if (cat.IsReadonly && !userData.IsIDT)
				throw new Exception("Not authorized");
			using (var scope = new TransactionScope())
			{
				using (var dynamicDB = factory.GetInstance<DynamicDB>())
				{
					dynamicDB.Open(connstr);
					dynamicDB.DeleteAll(cat.CatalogID);
					scope.Complete();
				}
			}
		}

		public string GetSubset(int catalogid, int id, string fieldName)
		{
			var cat = catalogRepo.GetByCatalogID(catalogid);
			using (var dynamicDB = factory.GetInstance<DynamicDB>())
			{
				dynamicDB.Open(connstr);
				return dynamicDB.GetSubset(cat.CatalogID, id, fieldName).ToString();
			}
		}

        public string GetFullSubset(int catalogid, string fieldName)
        {
            var cat = catalogRepo.GetByCatalogID(catalogid);
            using (var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                return dynamicDB.GetFullSubset(cat.CatalogID, fieldName).ToString();
            }
        }

        public void AddSet(int catalogid, int id, int leftCatalogId, int parentId)
        {
            var userData = factory.GetInstance<IUserData>();
            var cat = catalogRepo.GetByCatalogID(catalogid);
            if (cat.IsReadonly && !userData.IsIDT)
                throw new Exception("Not authorized");
            using (var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                CatalogDefinition parentCatalog = null;
                if (leftCatalogId > 0)
                {
                    var parent = catalogRepo.GetByCatalogID(leftCatalogId);
                    parentCatalog = new CatalogDefinition();
                    parentCatalog.ID = parent.CatalogID;
                    parentCatalog.Definition = parent.Definition;
                }

                var setField = parentCatalog.Fields.FirstOrDefault(f => f.Type == ColumnType.Set && f.CatalogID == cat.CatalogID);

                if (setField != null)
                {
                    dynamicDB.Open(connstr);
                    dynamicDB.InsertRel(parentCatalog.ID, cat.CatalogID, setField.FieldID, parentId, id);
                }
            }
        }


        public int GetObjectID(string json)
		{
			var o = JObject.Parse(json);

			if (o["ID"] != null)
			{
				return Convert.ToInt32(o["ID"]);
			}

			return -1;

		}

		private bool IsRowDataUpdated(string actual, string data, List<FieldDefinition> fields)
		{
			// compare fields only

			JObject currentObj = JObject.Parse(actual);
			JObject newDataObj = JObject.Parse(data);

			bool hasDiff = false;

			foreach (var p in newDataObj.Properties())
			{
                var field = fields.FirstOrDefault(x => x.Name.Equals(p.Name));
				// could be better use definition  to correctly field types  casting
				if ((field != null && field.Type == ColumnType.Set) || currentObj[p.Name].ToString() != p.Value.ToString())
				{
					hasDiff = true;
					// can register fields changed
					break; // first diff, get out
				}
			}


			return hasDiff;
		}


		public void ImportLookupCatalog(int catalogid, string json)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				ImportLookupCatalog(ctx, catalogid, json);
			}
		}

		public void ImportLookupCatalog(PrintDB ctx, int catalogid, string json)
		{
			var catalog = ctx.Catalogs.Where(c => c.ID == catalogid).FirstOrDefault();
			if (catalog == null)
				throw new InvalidOperationException($"Catalog {catalogid} could not be found");

			using (var db = factory.GetInstance<DynamicDB>())
			{
				var connStr = config["Databases.CatalogDB.ConnStr"];
				db.Open(connStr);
				var array = JArray.Parse(json);
				foreach (var elm in array)
				{
					var row = elm as JObject;
					var id = row.GetValue<int>("ID");
					var existing = db.SelectOne(catalog.CatalogID, id);
					if (existing == null || !existing.HasValues)
					{
						existing = new JObject();
						Copy(existing, row);
						db.Insert(catalog.CatalogID, existing, true);
					}
					else
					{
						db.Update(catalog.CatalogID, existing);
					}
				}
			}
		}


		private void Copy(JObject target, JObject source)
		{
			foreach(var prop in source.Properties())
				target[prop.Name] = source[prop.Name];
		}

        public int GetListCount(int catalogid, TextSearchFilter[] filter)
        {
            var cat = catalogRepo.GetByCatalogID(catalogid);
            using(var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(connstr);
                return dynamicDB.FreeTextSearchCount(cat.CatalogID, filter);
            }

        }
    }
}
