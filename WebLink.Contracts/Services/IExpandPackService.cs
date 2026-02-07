using Service.Contracts.WF;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Services
{
    public interface IExpandPackService
    {
        List<OrderInfo> Execute(OrderInfo order, int projectID, CancellationToken cancellationToken);
        OrderInfo GenerateOrderInfo(int orderId, int projectID, string packCode);
    }
}