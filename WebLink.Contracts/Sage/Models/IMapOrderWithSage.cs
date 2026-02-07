using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public interface IMapOrderWithSage : IMapObjectWithSage
    {
        string ProjectPrefix { get; set; }
        SageInvoiceStatus InvoiceStatus { get; set; }
        SageDeliveryStatus DeliveryStatus { get; set; }
        SageOrderStatus SageStatus { get; set; }
        SageCreditStatus CreditStatus { get; set; }
        DateTime? RegisteredOn { get; set; }
        string InvoiceStatusText { get; }
        string DeliveryStatusText { get; }
        string SageStatusText { get; }
        string CreditStatusText { get; }
    }

    public enum SageInvoiceStatus
    {
        Unknow = 0,
        NoInvoiced = 1,
        PartialInvoiced = 2,
        Invoiced = 3
    }

    public enum SageDeliveryStatus
    {
        Unknow = 0,
        NoShipped = 1,
        PartialShipped = 2,
        Shipped = 3
    }

    public enum SageOrderStatus
    {
        Unknow = 0,
        Open, // No Saldado
        Closed,// Saldado
        NotFound
    }

    public enum SageCreditStatus
    {
        Unknow = 0,
        OK = 1,
        Locked = 2, 
        LimitExceeded = 3,
        PrepaymentNotDeposited = 4
    }

}
