using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IOrderDetailRepository
    {
        List<IOrderDetail> GetOrderDetails(int orderid);
        List<IOrderDetail> GetOrderDetails(IOrder order);
    }
}
