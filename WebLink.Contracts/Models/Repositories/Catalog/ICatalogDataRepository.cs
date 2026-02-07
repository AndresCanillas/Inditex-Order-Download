using Service.Contracts.Database;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface ICatalogDataRepository
    {
        string GetByID(int catalogid, int id);
        string GetList(int catalogid);
        List<TableData> ExportData(int projectid, string catalogName, bool recursive, params NameValuePair[] filter);
        List<TableData> ExportData(int catalogid, bool recursive, params NameValuePair[] filter);
        string SearchFirst(int catalogid, string fieldName, string value);
        string FreeTextSearch(int catalogid, params TextSearchFilter[] filter);
        int FreeTextSearchCount(int catalogid, params TextSearchFilter[] filter);
        string FreeTextSearch(int catalogid, int pageNumber, int pageSize, params TextSearchFilter[] filter);
        string SubsetFreeTextSearch(int catalogid, int id, string fieldName, params TextSearchFilter[] filter);
        string SearchMultiple(int catalogid, List<string> barcodes);
        string Insert(int catalogid, string json);
        string Update(int catalogid, string json);
        void Delete(int catalogid, int id, int leftCatalogId, int? parentId = null);
        void DeleteAll(int catalogid);
        string GetSubset(int catalogid, int id, string fieldName);
        void ImportLookupCatalog(int catalogid, string json);
        string GetFullSubset(int catalogid, string fieldName);
        void AddSet(int catalogid, int id, int leftCatalogId, int ParentId);
        string GetListByPage(int catalogid, int pagenumber, int pagesize);
        int GetListCount(int catalogid);
        int GetListCount(int catalogid, TextSearchFilter[] filter);
        string GetBaseDataFromOrderId(int projectid, IOrder order);
    }
}



