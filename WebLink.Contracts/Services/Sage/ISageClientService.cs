using Service.Contracts;
using WebLink.Contracts.Sage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Sage
{
    public interface ISageClientService
    {
        Task<string> Request(string xml, string url, string soapAction);
        Task<ISageBpc> GetCustomerDetail(string customerRef);
        Task<ISageItem> GetItemDetail(string itemRef);
        Task<ISageOrder> RegisterOrder(ISageOrder order); // TODO: Use Interface ISageOrder
        Task<ISageOrder> UpdateOrderItemsAsync(ISageOrder order, string referece);
        Task<ISageOrder> GetOrderDetailAsync(string reference);
        Task<bool> CheckIfOrderExistAsync(string reference, int orderID);
        Task<IEnumerable<ISageItemQuery>> GetAllItemsByTerm(IEnumerable<IWsKey> searchKey, int listSize);
        bool IsEnabled();
    }
}
