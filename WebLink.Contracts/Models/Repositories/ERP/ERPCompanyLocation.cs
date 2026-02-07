using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{

    public class ERPCompanyLocation : IERPCompanyLocation
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProductionLocationID { get; set; }
        public int ERPInstanceID { get; set; }
        public string Currency { get; set; }
        public string BillingFactoryCode { get; set; }
        public string ProductionFactoryCode { get; set; }
        public string ExpeditionAddressCode { get; set; }
        public int? DeliveryAddressID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        
    }
}
