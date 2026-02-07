using System;
using System.Collections;
using System.Collections.Generic;

namespace WebLink.Contracts.Models.Delivery
{
    public class DeliveryNote
    {
        public int ID { get; set; }

        public int? DeliveryID { get; set; }         // ID of the delivery note in Print Central

        public int FactoryID { get; set; }          // ID of the factory in Print Central   
        public int SendToCompanyID { get; set; }
        public int? SendToAddressID { get; set; }    // ID of the send to address
        public string Number { get; set; }                
        public DateTime ShippingDate { get; set; }
        public int? CarrierID { get; set; }
        public string TrackingCode { get; set; }
        public DeliveryNoteStatus Status { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Carrier Carrier { get; set; } // Navigation property for the carrier 
        public DeliveryNoteSource Source { get; set; } 
        public IEnumerable<Package> Packages { get; set; }
        public int? AddressCentralID { get; set; }
    }
}
