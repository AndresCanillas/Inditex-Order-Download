using System;

namespace WebLink.Contracts.Models
{
    public class OrderToUpdateDTO
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public int UpdatePropertiesID { get; set; }
        public bool IsActive { get; set; }
        public bool IsReject { get; set; }
        public DateTime OrderCreatedAt { get; set; }
        public DateTime UpdatePropertiesCreatedAt { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public int? LabelID { get; set; }
        public ProductionType ProductionType { get; set; }
        public bool IsStopped { get; set; }
        public bool IsBillable { get; set; }
        public bool ArticleHasEnableConflicts { get; set; }
        public string PackCode { get; set; }
    }
}