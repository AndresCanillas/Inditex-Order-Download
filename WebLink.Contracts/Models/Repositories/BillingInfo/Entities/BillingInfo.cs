using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class BillingInfo : IBillingInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

