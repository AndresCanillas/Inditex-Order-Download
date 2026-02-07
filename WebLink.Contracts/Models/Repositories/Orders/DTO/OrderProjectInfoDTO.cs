using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class OrderInfoDTO
    {
        public int OrderGroupID { get; set; }
        public int OrderID { get; set; }
        public int OrderDataID { get; set; }
        public string OrderNumber { get; set; }
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public int ProjectID { get; set; }
        public string ProjectCode { get; set; }
        public ProductionType ProductionType { get; set; }
        public string SendTo { get; set; }
        public string BillTo { get; set; }
        public bool IsBilled { get; set; }
        public bool IsBillable { get; set; }
        public int SendToCompanyID { get; set; }
        public int BillToCompanyID { get; set; }
        public int? SendToAddressID { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public bool BillToSyncWithSage { get; set; }
        public string BillToSageRef { get; set; }
        public string BillToCompanyCode { get; set; }
        public string FabricCode { get; set; }
        public int? LocationID { get; set; }
        public DateTime? DueDate { get; set; }
        public string MDOrderNumber { get; set; }
        public string CompanyCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public string BrandName { get; set; }
        public int? ProviderRecordID { get; set; }
        public bool HasOrderWorkflow {  get; set; }
        public long? ItemID { get; set; }   
        public DocumentSource Source { get; set; }   
    }
}
