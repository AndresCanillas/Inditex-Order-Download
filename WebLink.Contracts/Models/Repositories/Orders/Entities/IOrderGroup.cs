using Service.Contracts.Database;
using System;

namespace WebLink.Contracts.Models
{
    public interface IOrderGroup: IEntity, IBasicTracing, IMapOrderWithSage
    {
        string OrderNumber { get; set; }
        int ProjectID { get; set; }
        bool IsCompleted { get; set; }
        bool IsActive { get; set; }
        bool IsRejected { get; set; }
        int BillToCompanyID { get; set; }
        int SendToCompanyID { get; set; }
        int? SendToAddressID { get; set; }
        DateTime? CompletedDate { get; set; }
        string ERPReference { get; set; } // MD ORder Number globally 
        string OrderCategoryClient { get; set; }
    }

}
