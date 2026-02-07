using System;

namespace WebLink.Contracts.Models.Delivery.DTO
{
    public class DeliveryNoteDetailsDTO
    {
        public int DeliveryNoteID { get; set; }
        public string DeliveryNote { get; set; }
        public string FactoryCode { get; set; }
        public string FactoryName { get; set; }
        public string CarrierName { get; set; }
        public DateTime ShippingDate { get; set; }
        public string TrackingCode { get; set; }
        public int PackageNumber { get; set; }
        public string ArticleCode { get; set; }
        public string Description { get; set; }
        public string Size { get; set; }
        public string Colour { get; set; }
        public int Quantity { get; set; }
        public int QuantitySent { get; set; }
        public string SendToName { get; set; }
        public string Country { get; set; }
    }
}
