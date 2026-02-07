using System.Collections.Generic;

namespace WebLink.Contracts.Models.Delivery
{
    public class Package
    {
        public int ID { get; set; }
        public int DeliveryNoteID { get; set; }         // ID of the parent record (DeliveryNote)   
        public int PackageNumber { get; set; }          // Package number
        public decimal NetWeight { get; set; }          // Package net weight  
        public decimal GrossWeight { get; set; }        // Package gross weight  
        public decimal Length { get; set; }             // Package length
        public decimal Width { get; set; }              // Package width 
        public decimal Height { get; set; }             // Package height
        public IEnumerable<PackageDetail> PackageDetails { get; set; } // Navigation property for package details   

    }
}
