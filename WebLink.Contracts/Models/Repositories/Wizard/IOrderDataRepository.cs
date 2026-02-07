using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public interface IOrderDataRepository
    {
        List<TableObject> GetFullOrderData(List<CatalogDefinition> catalogs, int orderId);
        TableObject GetTableObject(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null);
        List<RelColumn> GetRelColumn(List<FieldDefinition> fields, List<CatalogDefinition> catalogs);
        List<RelTable> GetRelTable(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null);
        List<RefColumn> GetRefColumn(List<FieldDefinition> fields, List<CatalogDefinition> catalogs);
        List<RefTable> GetRefTable(CatalogDefinition catalog, List<CatalogDefinition> catalogs, int? orderId = null);
        void GetRelIds(DynamicDB conn, List<TableObject> tables, string tableName, string articleCode);
        void GetEmbeddedIds(List<int> idList, TableObject embeddedTable);
    }
}
