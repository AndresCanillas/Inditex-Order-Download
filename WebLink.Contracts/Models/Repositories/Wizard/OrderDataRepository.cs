using Newtonsoft.Json.Linq;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
    /*
        Get Catalogs and Tables structure for a given Order 
    */
    public class OrderDataRepository : IOrderDataRepository
    {
        /// <summary>
        /// get the of List<TableObject> created manually, not contains rows info, only is added ID of the root catalogs
        /// </summary>
        /// <param name="catalogs"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public List<TableObject> GetFullOrderData(List<CatalogDefinition> catalogs, int orderId)
        {
            var exportTables = new List<TableObject>();
            var orderCatalog = catalogs.FirstOrDefault(c => c.Name == "Orders");
            exportTables.Add(GetTableObject(orderCatalog, catalogs, orderId));

            var catalogList = catalogs.Where(c => !Equals(c.ID, orderCatalog.ID)).ToList();

            var ignoreCompositionTables = new List<string> {
                    Catalog.COMPOSITIONLABEL_CATALOG,
                    Catalog.CMP_USER_SECTIONS_CATALOG,
                    Catalog.CMP_USER_FIBERS_CATALOG,
                    Catalog.BRAND_FIBERS_CATALOG,
                    Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG,
                    Catalog.BRAND_CAREINSTRUCTIONS_CATALOG
            };

            //get the full db tables list
            foreach (var catalog in catalogList.Where(_w => !ignoreCompositionTables.Any(_a => _w.Name.StartsWith(_a))  ))
            {
                exportTables.Add(GetTableObject(catalog, catalogs));
            }

            return exportTables;
        }

        public TableObject GetTableObject(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null)
        {
            var table = new TableObject
            {
                Id = catalog.ID,
                Name = catalog.Name + "_" + catalog.ID,
                RelColumns = GetRelColumn(catalog.Fields, catalogs),
                RelTables = GetRelTable(catalog, catalogs, orderId),
                RefColumns = GetRefColumn(catalog.Fields, catalogs),
                RefTables = GetRefTable(catalog, catalogs, orderId),
                Fields = catalog.Fields,
                ParentIds = orderId != null ? new List<int> { orderId.Value } : new List<int>()
            };

            return table;
        }

        public List<RelColumn> GetRelColumn(List<FieldDefinition> fields, List<CatalogDefinition> catalogs)
        {
            var relColumns = new List<RelColumn>();

            foreach (var field in fields)
            {
                if (field.Type == ColumnType.Set)
                {
                    var relCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
                    if (relCatalog != null)
                    {
                        relColumns.Add(
                            new RelColumn
                            {
                                Id = relCatalog.ID,
                                Name = relCatalog.Name
                            });
                    }
                }
            }

            return relColumns;
        }

        public List<RelTable> GetRelTable(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null)
        {
            var relTables = new List<RelTable>();
            var orderDetails = catalogs.First(_f => _f.Name.StartsWith(Catalog.ORDERDETAILS_CATALOG));

            foreach (var field in catalog.Fields)
            {
                //XXX: only include OrderDetails Set RelationShip
                if (field.Type == ColumnType.Set && field.CatalogID == orderDetails.ID)
                {
                    var relCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
                    if (relCatalog != null)
                    {
                        relTables.Add(
                            new RelTable
                            {
                                SourceId = catalog.ID,
                                SourceName = catalog.Name,
                                TargetId = relCatalog.ID,
                                TargetName = relCatalog.Name,
                                Name = "REL_" + catalog.Name + "_" + catalog.ID + "_" + relCatalog.Name + "_" + relCatalog.ID + "_" + field.Name,
                                Field = field.Name,
                                RelIds = new List<int>(),
                                ParentIds = orderId != null ? new List<int> { orderId.Value } : new List<int>()
                            });
                    }
                }
            }

            return relTables;
        }

        public List<RefColumn> GetRefColumn(List<FieldDefinition> fields, List<CatalogDefinition> catalogs)
        {
            var refColumns = new List<RefColumn>();

            foreach (var field in fields)
            {
                if (field.Type == ColumnType.Reference)
                {
                    var refCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
                    if (refCatalog != null)
                    {
                        refColumns.Add(
                            new RefColumn
                            {
                                Id = refCatalog.ID,
                                Name = refCatalog.Name
                            });
                    }
                }
            }

            return refColumns;
        }

        public List<RefTable> GetRefTable(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null)
        {
            var refTables = new List<RefTable>();

            var ignoreCompositionTables = new List<string> {
                    Catalog.COMPOSITIONLABEL_CATALOG,// ref
                    Catalog.CMP_USER_SECTIONS_CATALOG, //rel
                    Catalog.CMP_USER_FIBERS_CATALOG, // rel
                    Catalog.BRAND_FIBERS_CATALOG, //ref
                    Catalog.CMP_USER_CAREINSTRUCTIONS_CATALOG, // rel
                    Catalog.BRAND_CAREINSTRUCTIONS_CATALOG // ref
            };

            var ignoreCatalogs = catalogs.Where(_w => ignoreCompositionTables.Any(_a => _w.Name.StartsWith(_a))).Select(_s => _s.ID).ToList();



            foreach (var field in catalog.Fields)
            {
                if (field.Type == ColumnType.Reference && !ignoreCatalogs.Contains(field.CatalogID.Value))
                {
                    var refCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
                    if (refCatalog != null)
                    {
                        refTables.Add(
                            new RefTable
                            {
                                SourceId = catalog.ID,
                                SourceName = catalog.Name,
                                TargetId = refCatalog.ID,
                                TargetName = refCatalog.Name,
                                Name = refCatalog.Name + "_" + refCatalog.ID,
                                Field = field.Name,
                                RefIds = new List<int>(),
                                ParentIds = orderId != null ? new List<int> { orderId.Value } : new List<int>()
                            });
                    }
                }
            }

            return refTables;
        }

        public void GetRelIds(DynamicDB conn, List<TableObject> tables, string tableName, string articleCode)
        {
            var table = tables.FirstOrDefault(x => Equals(x.Name, tableName));
            foreach (var relTable in table.RelTables)
            {
                var filterData = false;
                JProperty data = null;


                if (Equals(relTable.TargetName, Catalog.ORDERDETAILS_CATALOG))
                    filterData = true;// hardcode activation of the filter for table OrderDetails

                var idList = new List<int>();
                var refIdList = new Dictionary<string, List<int>>();

                foreach (var id in relTable.ParentIds)
                {
                    var catalogDataList = conn.GetSubset(table.Id, id, relTable.Field);

                    var filteredData = new List<JToken>();
                    if (filterData && catalogDataList.Count > 0)
                    {
                        filteredData = catalogDataList.Where(x => x["ArticleCode"].ToString() == articleCode || x["ArticleCode"].ToString() == Article.EMPTY_ARTICLE_CODE).ToList();
                        data = ((catalogDataList.First() as JObject).Properties().ToList() as List<JProperty>).FirstOrDefault(x => x.Name == "Product");
                    }
                    else
                    {
                        filteredData = catalogDataList.ToList();
                    }

                    foreach (JObject catalogData in filteredData)
                    {

                        if (filterData)
                        {
                            var product = (catalogData.Properties().ToList() as List<JProperty>).FirstOrDefault(x => x.Name == "Product");

                            if (data == null || product == null)
                                continue;
                        }

                        JProperty field = catalogData.Properties().First(_p => _p.Name == "ID");
                        idList.Add(Convert.ToInt32(field.Value));

                    }
                }

                relTable.RelIds = relTable.RelIds.Union(idList).Distinct().ToList();
                var parentRelTable = tables.FirstOrDefault(x => x.Name == relTable.TargetName + "_" + relTable.TargetId);
                GetEmbeddedIds(idList, parentRelTable);
                GetRelIds(conn, tables, parentRelTable.Name, articleCode);
            }

            foreach (var refTable in table.RefTables)
            {
                var idList = new List<int>();

                foreach (var id in refTable.ParentIds)
                {
                    var catalogDataList = conn.Select(table.Id, "select * from #TABLE where ID = @id", id);

                    foreach (JObject catalogData in catalogDataList.ToList())
                    {
                        JToken refValue = catalogData[refTable.Field];

                        if (refValue.Type != JTokenType.Null)
                            idList.Add(int.Parse(refValue.ToString()));
                    }
                }

                refTable.RefIds = refTable.RefIds.Union(idList).Distinct().ToList();
                var parentRefTable = tables.FirstOrDefault(x => x.Name == refTable.TargetName + "_" + refTable.TargetId);
                GetEmbeddedIds(idList, parentRefTable);
                GetRelIds(conn, tables, parentRefTable.Name, articleCode);
            }
        }

        public void GetEmbeddedIds(List<int> idList, TableObject embeddedTable)
        {
            foreach (var id in idList)
            {
                if (!embeddedTable.ParentIds.Contains(id))
                    embeddedTable.ParentIds.Add(id);
            }

            foreach (var embeddedReltable in embeddedTable.RelTables)
            {
                embeddedReltable.ParentIds = idList;
            }

            foreach (var embeddedReltable in embeddedTable.RefTables)
            {
                embeddedReltable.ParentIds = idList;
            }
        }
    }

    public class TableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<RelColumn> RelColumns { get; set; }
        public List<RelTable> RelTables { get; set; }
        public List<RefColumn> RefColumns { get; set; }
        public List<RefTable> RefTables { get; set; }
        public List<FieldDefinition> Fields { get; set; }
        public List<int> ParentIds { get; set; }
        public List<int> NewRefIds { get; set; }
        public bool Processed { get; set; }
        public bool IsEditble { get; set; }

    }

    public class RelColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RelTable
    {
        public int SourceId { get; set; }
        public string SourceName { get; set; }
        public int TargetId { get; set; }
        public string TargetName { get; set; }
        public string Name { get; set; }
        public string Field { get; set; }
        public List<int> ParentIds { get; set; }
        public int ItemId { get; set; }
        public List<int> RelIds { get; set; }
    }

    public class RefColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class RefTable
    {
        public int SourceId { get; set; }
        public string SourceName { get; set; }
        public int TargetId { get; set; }
        public string TargetName { get; set; }
        public string Name { get; set; }
        public string Field { get; set; }
        public List<int> ParentIds { get; set; }
        public int ItemId { get; set; }
        public List<int> RefIds { get; set; }
    }
}
