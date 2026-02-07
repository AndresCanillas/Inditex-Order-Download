using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class OrderGroup : IOrderGroup
    {
        public int ID { get; set; }
        public string OrderNumber { get; set; }
        public int ProjectID { get; set; }
        public bool IsCompleted { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool IsActive { get; set; }
        public bool IsRejected { get; set; }

        public int BillToCompanyID { get; set; }

        public int SendToCompanyID { get; set; }

        public int? SendToAddressID { get; set; }       // Address that will be used to deliver the order to the client
        public string ERPReference { get; set; }
        public string OrderCategoryClient { get; set; }

        #region SAGE Fields
        public string ProjectPrefix { get; set; }
        public bool SyncWithSage { get; set; }
        public string SageReference { get; set; }
        public SageInvoiceStatus InvoiceStatus { get; set; }
        public SageDeliveryStatus DeliveryStatus { get; set; }
        public SageOrderStatus SageStatus { get; set; }
        public SageCreditStatus CreditStatus { get; set; }
        public DateTime? RegisteredOn { get; set; }
        public string InvoiceStatusText { get; }
        public string DeliveryStatusText { get; }
        public string SageStatusText { get; }
        public string CreditStatusText { get; }
        #endregion SAGE Fields

        public string CreatedBy { get; set; } // aqui no tiene mucho sentido, ya que cada parcialidad puede ser creada de forma separada en diferentes momentos por diferentes usuarios
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
       
    }
}
