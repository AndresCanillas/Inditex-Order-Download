using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class CompanyProvider : ICompanyProvider
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public Company Company { get; set; }
        public int ProviderCompanyID { get; set; }
        [MaxLength(10)]
        public string ClientReference { get; set; }   // This is the code used by the client to reference to this provider
 
        public string Instructions { get; set; }
        public int? SLADays { get; set; }
        public int? DefaultProductionLocation { get; set; }
        public int? BillingInfoID { get; set; }

        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        [LazyLoad]
        [Service.Contracts.IgnoreField]
        [JsonIgnore]
        public ICollection<Order> Orders { get; set; }
    }
}

