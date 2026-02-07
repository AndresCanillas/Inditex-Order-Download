using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IOrderRegisterInERP : IOrderWorkFlowAction
    {
        bool DisabledBilling { get; }
        bool CanBill(OrderInfoDTO orderInfo);

        string GetMDReference(OrderInfoDTO orderInfo, IEnumerable<string> articleCodes);
        string GetMDReference(PrintDB ctx, OrderInfoDTO orderInfo, IEnumerable<string> articleCodes);

        string GetProjectCodeShared(OrderInfoDTO orderInfo);
        string GetProjectCodeShared(PrintDB ctx, OrderInfoDTO orderInfo);

        void MarkAsBilled(OrderInfoDTO orderInfo);
        void MarkAsBilled(PrintDB ctx, OrderInfoDTO orderInfo);

		IOrder UpdateOrderReference(OrderInfoDTO orderInfo);
		IOrder UpdateOrderReference(PrintDB ctx, OrderInfoDTO orderInfo);
        IOrder UpdateDueDate(PrintDB ctx, OrderInfoDTO orderInfo);
    }
}
