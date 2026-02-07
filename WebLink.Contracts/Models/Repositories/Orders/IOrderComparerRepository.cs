using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IOrderComparerRepository : IGenericRepository<IOrder>
    {
        ComparerConfiguration GetComparerType(int orderId);
        OrderComparerViewModel Compare(int id, int prevOrderId, string article = null);
        OrderData GetBaseData(int id, int prevOrderId);
        void CompareByRow(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates);
        void CompareByColumn(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates, string key);
        //void CompareByArticle(List<Dictionary<string, string>> newOrderData, List<Dictionary<string, string>> prevOrderData, List<List<string>> updates, string article);
    }
}