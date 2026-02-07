using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IAlvaroMorenoOrderValidationService
    {
        List<OrderGroupSelectionDTO> GetOrderData(List<OrderGroupSelectionDTO> selection);
        List<OrderGroupSelectionDTO> GetOrderData(PrintDB ctx, List<OrderGroupSelectionDTO> selection);
        void UpdateOrdersMadeIn(List<OrderGroupQuantitiesDTO> rq);
    }
}
