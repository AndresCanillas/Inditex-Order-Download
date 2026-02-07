using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class ERPCompanyConfigDTO
    {
        public string BillingFactoryCode { get; set; }
        public int CompanyID { get; set; }
        public string Currency { get; set; }
        public int? DeliveryAddressID { get; set; }
        public string ExpeditionAddressCode { get; set; }
        public int ERPInstanceID { get; set; }
        public string ERPName { get; set; }
        public string LocationName { get; set; }
        public string ProductionFactoryCode { get; set; }
        public int ProductionLocationID { get; set; }
        public int ID { get; set; }
    }
}
