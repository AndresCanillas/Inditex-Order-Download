//using ADOX;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.OleDb;
//using System.Linq;
//using System.Text;


//namespace Service.Contracts.Database
//{
//	partial class DynamicDB
//	{
//        private ILogService log;
//        private IDBX db;

//        public void OpenSourceDB(string connStr)
//        {
//            var config = factory.GetInstance<IDBConfiguration>();
//            config.ProviderName = CommonDataProviders.SqlServer;
//            config.ConnectionString = connStr;
//            db = config.CreateConnection();  
//        }

//        public List<int> GetOrderData(int orderId)
//        {
//            var catalogIds = new List<int>();
//            StringBuilder getStatement = new StringBuilder(1000);
//            getStatement.AppendFormat(@"select CatalogID from CompanyOrders o
//                            join Catalogs c on o.ProjectID = c.ProjectID where o.ID = @orderId");

//            using (var reader = db.ExecuteReader(getStatement.ToString(), orderId))
//            {
//                while (reader.Read())
//                {
//                    //add this to a catalog list and return, then open a new connection to getCatalog by Id on DynamicDB
//                    catalogIds.Add(reader.GetInt32(0));

//                    //var rootCatalog = GetCatalog(reader.GetInt32(0));
//                }
//            }

//            return catalogIds;
//        }

//        public List<TableObject> CreateTables(List<CatalogDefinition> catalogs, string databaseProvider, int orderId)
//        {
//            //return the new catalog list objects (the one with the new object)
//            var exportTables = GetFullOrderData(catalogs, orderId);

//            //Export to Access accdb --- TODO: create config method, then do the same for mdb, and apply both functionalities and refactor
//            Catalog databaseCatalog = new Catalog();
//            databaseCatalog.Create(databaseProvider.Replace(" OLE DB Services=-4;", ""));

//            using (OleDbConnection connection = new OleDbConnection(databaseProvider))
//            {
//                OleDbCommand cmd = new OleDbCommand();
//                OleDbTransaction transaction = null;
//                try
//                {
//                    connection.Open();
//                    transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
//                    cmd.Connection = connection;
//                    cmd.Transaction = transaction;
//                    cmd.CommandType = CommandType.Text;
//                    foreach (var table in exportTables)
//                    {
//                        /*
//                         * here I need to create two methods, one is for the catalog and the other one is for the relation catalogs 
//                         */
//                        cmd.CommandText = string.Empty;
//                        CreateTableScript(table, cmd);
//                        cmd.ExecuteNonQuery();

//                        foreach (var rel in table.RelTables)
//                        {
//                            cmd.CommandText = string.Empty;
//                            CreateRelTableScript(rel, cmd);
//                            cmd.ExecuteNonQuery();
//                        }

//                        // try to see if its possible to execute this with the full tables creation
//                    }

//                    transaction.Commit();
//                }
//                catch (Exception ex)
//                {
//                    transaction.Rollback();
//                }

//                connection.Dispose();
//            }

//            return exportTables;
//        }

//        public void ExportData(DynamicDB conn, int orderId, List<TableObject> tables, string webLinkDataDB, string databaseProvider)
//        {
//            GetRelIds(conn, databaseProvider, tables);

//            using (OleDbConnection connection = new OleDbConnection(databaseProvider))
//            {
//                try
//                {
//                    connection.Open();
//                    OleDbCommand cmd = new OleDbCommand();
//                    cmd.Connection = connection;
//                    cmd.CommandType = CommandType.Text;

//                    foreach (var table in tables)
//                    {
//                        foreach (var id in table.ParentIds)
//                        {
//                            cmd.CommandText = string.Empty;
//                            var catalogData = conn.Select(table.Id, "select * from #TABLE where ID = @id", id);

//                            string tableData = string.Empty;
//                            foreach (JObject data in catalogData.Children<JObject>())
//                            {
//                                foreach (JProperty field in data.Properties())
//                                {
//                                    tableData += SetColumnValue(table.Fields.FirstOrDefault(f => Equals(f.Name, field.Name)).Type, field) + ",";
//                                }
//                            }

//                            tableData = tableData.TrimEnd(',');
//                            cmd.CommandText = "insert into  " + table.Name + " values (" + tableData + ")";

//                            cmd.ExecuteNonQuery();
//                        }

//                        foreach (var relTable in table.RelTables)
//                        {
//                            foreach (var id in relTable.ParentIds)
//                            {
//                                cmd.CommandText = string.Empty;

//                                var catalogDataList = conn.GetSet(table.Id, id, relTable.Field);

//                                foreach (JObject catalogData in catalogDataList.Children<JObject>())
//                                {
//                                    foreach (JProperty field in catalogData.Properties())
//                                    {
//                                        if (Equals(field.Name, "ID"))
//                                        {
//                                            cmd.CommandText = "insert into  " + relTable.Name + " values (" + id + "," + Convert.ToInt32(field.Value) + ");";
//                                            cmd.ExecuteNonQuery();
//                                        }
//                                    }
//                                }
//                            }
//                        }

//                        foreach (var refTable in table.RefTables)
//                        {
//                            var refColumns = tables.FirstOrDefault(x => Equals(x.Id, refTable.TargetId));
//                            foreach (var id in refTable.RefIds)
//                            {
//                                cmd.CommandText = string.Empty;

//                                var catalogDataList = conn.Select(refTable.TargetId, "select * from #TABLE where ID = @id", id);

//                                string tableData = string.Empty;
//                                foreach (JObject catalogData in catalogDataList.Children<JObject>())
//                                {
//                                    foreach (JProperty field in catalogData.Properties())
//                                    {
//                                        tableData += SetColumnValue(refColumns.Fields.FirstOrDefault(f => Equals(f.Name, field.Name)).Type, field) + ",";
//                                    }
//                                }

//                                tableData = tableData.TrimEnd(',');
//                                cmd.CommandText = "insert into  " + refTable.Name + " values (" + tableData + ")";

//                                cmd.ExecuteNonQuery();

//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    //MessageBox.Show(ex.Message, "Could not create ForeignKey");
//                }

//                connection.Dispose();
//            }
//        }

//        public void GetRelIds(DynamicDB conn, string databaseProvider, List<TableObject> tables)
//        {
//            try
//            {
//                foreach (var table in tables)
//                {
//                    foreach (var tableColumn in table.RelTables)
//                    {
//                        var idList = new List<int>();

//                        foreach (var id in tableColumn.ParentIds)
//                        {
//                            var catalogDataList = conn.GetSet(table.Id, id, tableColumn.Field);

//                            foreach (JObject catalogData in catalogDataList.Children<JObject>())
//                            {
//                                foreach (JProperty field in catalogData.Properties())
//                                {
//                                    if (Equals(field.Name, "ID"))
//                                        idList.Add(Convert.ToInt32(field.Value));
//                                }
//                            }

//                            tableColumn.RelIds.AddRange(idList);
//                            table.ParentIds.Add(id);
//                            var relTable = tables.FirstOrDefault(x => x.Name == tableColumn.TargetName + "_" + tableColumn.TargetId);
//                            relTable.ParentIds = idList;
//                        }
//                    }

//                    foreach (var tableColumn in table.RefTables)
//                    {
//                        var idList = new List<int>();

//                        foreach (var id in table.ParentIds)
//                        {
//                            var catalogDataList = conn.Select(table.Id, "select * from #TABLE where ID = @id", id);

//                            foreach (JObject catalogData in catalogDataList.Children<JObject>())
//                            {
//                                foreach (JProperty field in catalogData.Properties())
//                                {
//                                    if (Equals(field.Name, tableColumn.Field))
//                                        idList.Add(Convert.ToInt32(field.Value));
//                                }
//                            }

//                            tableColumn.ParentIds.Add(id);
//                            //var refTable = tables.FirstOrDefault(x => x.Name == tableColumn.TargetName + "_" + tableColumn.TargetId);
//                            //refTable.ParentIds = idList;
//                        }

//                        tableColumn.RefIds.AddRange(idList);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                //MessageBox.Show(ex.Message, "Could not create ForeignKey");
//            }
//        }

//        public void CreateTableScript(TableObject table, OleDbCommand cmd)
//        {
//            string databaseFields = string.Empty;

//            foreach (var field in table.Fields)
//            {
//                //TODO check what can I do with this ids
//                if (!Equals(field.Type, ColumnType.Set))
//                {
//                    databaseFields += "[" + field.Name + "] " + SetColumnType(field.Type) + ",";
//                }
//            }

//            databaseFields = databaseFields.TrimEnd(',');
//            cmd.CommandText += "create table " + table.Name + "(" + databaseFields + "); ";
//        }

//        public void CreateRelTableScript(RelTable rel, OleDbCommand cmd)
//        {
//           var databaseFields = "[SourceID] int, [TargetID] int ";
//            cmd.CommandText += "create table " + rel.Name + "(" + databaseFields + "); ";
//        }

//        public List<TableObject> GetFullOrderData(List<CatalogDefinition> catalogs, int orderId)
//        {
//            var exportTables = new List<TableObject>();
//            //get order catalog, this will be the starting point of the search
//            var orderCatalog = catalogs.FirstOrDefault(c => c.Name == "Orders");
//            //posble delete this and use the list one with the Add method
//            exportTables.Add(GetTableObject(orderCatalog, catalogs, orderId));

//            var catalogList = catalogs.Where(c => !Equals(c.ID, orderCatalog.ID)).ToList();

//            /*
//             * this will be the process that will get the table names
//             * id, tableName, rel columns, fields for the create table columns             * 
//             * 
//             */

//            //get the full db tables list
//            foreach (var catalog in catalogList)
//            {
//                exportTables.Add(GetTableObject(catalog, catalogs));
//            }

//            return exportTables;
//        }

//        public TableObject GetTableObject(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null)
//        {
//            var table = new TableObject
//            {
//                Id = catalog.ID,
//                Name = catalog.Name + "_" + catalog.ID,
//                RelColumns = GetRelColumn(catalog.Fields, catalogs),
//                RelTables = GetRelTable(catalog, catalogs, orderId),
//                RefColumns = GetRefColumn(catalog.Fields, catalogs),
//                RefTables = GetRefTable(catalog, catalogs, orderId),
//                Fields = catalog.Fields,
//                ParentIds = new List<int>()
//            };

//            return table;
//        }

//        public List<RelColumn> GetRelColumn(List<FieldDefinition> fields, List<CatalogDefinition> catalogs)
//        {
//            var relColumns = new List<RelColumn>();

//            foreach (var field in fields)
//            {
//                if (field.Type == ColumnType.Set)
//                {
//                    var relCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
//                    if (relCatalog != null)
//                    {
//                        relColumns.Add( 
//                            new RelColumn {
//                                Id = relCatalog.ID,
//                                Name = relCatalog.Name
//                        });
//                    }
//                }
//            }

//            return relColumns;
//        }

//        public List<RelTable> GetRelTable(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null)
//        {
//            var relTables = new List<RelTable>();

//            foreach (var field in catalog.Fields)
//            {
//                if (field.Type == ColumnType.Set)
//                {
//                    var relCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
//                    if (relCatalog != null)
//                    {
//                        relTables.Add(
//                            new RelTable
//                            {
//                                SourceId = catalog.ID,
//                                SourceName = catalog.Name,
//                                TargetId = relCatalog.ID,
//                                TargetName = relCatalog.Name,
//                                Name = "REL_" + catalog.Name + "_" + catalog.ID + "_" + relCatalog.Name + "_" + relCatalog.ID + "_" + field.Name,
//                                Field = field.Name,
//                                RelIds = new List<int>(),
//                                ParentIds = orderId != null ? new List<int> { orderId.Value } : new List<int>()
//                            });
//                    }
//                }
//            }

//            return relTables;
//        }

//        public List<RefColumn> GetRefColumn(List<FieldDefinition> fields, List<CatalogDefinition> catalogs)
//        {
//            var refColumns = new List<RefColumn>();

//            foreach (var field in fields)
//            {
//                if (field.Type == ColumnType.Reference)
//                {
//                    var refCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
//                    if (refCatalog != null)
//                    {
//                        refColumns.Add(
//                            new RefColumn
//                            {
//                                Id = refCatalog.ID,
//                                Name = refCatalog.Name
//                            });
//                    }
//                }
//            }

//            return refColumns;
//        }

//        public List<RefTable> GetRefTable(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null)
//        {
//            var refTables = new List<RefTable>();

//            foreach (var field in catalog.Fields)
//            {
//                if (field.Type == ColumnType.Reference)
//                {
//                    var refCatalog = catalogs.FirstOrDefault(c => c.ID == field.CatalogID.Value);
//                    if (refCatalog != null)
//                    {
//                        refTables.Add(
//                            new RefTable
//                            {
//                                SourceId = catalog.ID,
//                                SourceName = catalog.Name,
//                                TargetId = refCatalog.ID,
//                                TargetName = refCatalog.Name,
//                                Name = refCatalog.Name + "_" + refCatalog.ID,
//                                Field = field.Name,
//                                RefIds = new List<int>(),
//                                ParentIds = orderId != null ? new List<int> { orderId.Value } : new List<int>()
//                            });
//                    }
//                }
//            }

//            return refTables;
//        }

//        private string SetColumnType(ColumnType columnType)
//        {
//            var fieldType = string.Empty;

//            switch (columnType)
//            {
//                case ColumnType.Int:
//                    fieldType = ColumnType.Int.ToString() + " null";
//                    break;

//                case ColumnType.Date:
//                    fieldType = ColumnType.Date.ToString();
//                    break;

//                case ColumnType.Bool:
//                    fieldType = ColumnType.Bool.ToString();
//                    break;

//                default:
//                    fieldType = "Text";
//                    break;
//            }

//            return fieldType;
//        }

//        private string SetColumnValue(ColumnType columnType, JProperty field)
//        {
//            var value = string.Empty;

//            switch (columnType)
//            {
//                case ColumnType.Int:
//                    value = Convert.ToInt32(field.Value).ToString();
//                    break;

//                ///All fields but int will be text, im almost sure access support it
//                //case ColumnType.Date:
//                //    value = DateTime.Parse(field.Value.ToString()).ToString();
//                //    break;

//                //case ColumnType.Bool:
//                //    value = Convert.ToBoolean(field.Value).ToString();
//                //    break;

//                default:
//                    value = "'" + field.Value.ToString() + "'";
//                    break;
//            }

//            return value;
//        }
//    }

//    public class TableObject
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//        public List<RelColumn> RelColumns { get; set; }
//        public List<RelTable> RelTables { get; set; }
//        public List<RefColumn> RefColumns { get; set; }
//        public List<RefTable> RefTables { get; set; }
//        public List<FieldDefinition> Fields { get; set; }
//        public List<int> ParentIds { get; set; }
//    }

//    public class RelColumn
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//    }

//    public class RelTable
//    {
//        public int SourceId { get; set; }
//        public string SourceName { get; set; }
//        public int TargetId { get; set; }
//        public string TargetName { get; set; }
//        public string Name { get; set; }
//        public string Field { get; set; }
//        public List<int> ParentIds { get; set; }
//        public int ItemId { get; set; }
//        public List<int> RelIds { get; set; }
//    }

//    public class RefColumn
//    {
//        public int Id { get; set; }
//        public string Name { get; set; }
//    }

//    public class RefTable
//    {
//        public int SourceId { get; set; }
//        public string SourceName { get; set; }
//        public int TargetId { get; set; }
//        public string TargetName { get; set; }
//        public string Name { get; set; }
//        public string Field { get; set; }
//        public List<int> ParentIds { get; set; }
//        public int ItemId { get; set; }
//        public List<int> RefIds { get; set; }
//    }
//}
