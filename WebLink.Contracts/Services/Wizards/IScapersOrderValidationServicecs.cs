using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IScalpersOrderValidationService
    {
        bool IsEmptyOrder(int orderGroupID);
        bool IsEmptyOrder(IEnumerable<OrderDetailDTO> articles);

        List<OrderGroupSelectionDTO> GetOrderData(PrintDB ctx, List<OrderGroupSelectionDTO> selection);
        List<OrderGroupSelectionDTO> GetOrderData(List<OrderGroupSelectionDTO> selection);
        List<ScalpersOrderGroupQuantitiesDTO> UpdateOrders(List<ScalpersOrderGroupQuantitiesDTO> rq);
    }
}
