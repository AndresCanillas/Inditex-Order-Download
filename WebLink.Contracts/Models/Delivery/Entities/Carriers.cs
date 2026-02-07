using System;

namespace WebLink.Contracts.Models.Delivery
{
    public class Carrier
    {
        public int ID { get; set; }
        public int? CarrierID { get; set; }
        public int? FactoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TrackingURL { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
