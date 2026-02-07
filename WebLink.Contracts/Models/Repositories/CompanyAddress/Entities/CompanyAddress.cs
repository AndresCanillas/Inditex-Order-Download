using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class CompanyAddress : ICompanyAddress
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public Company Company { get; set; }
        public int AddressID { get; set; }
        public Address Address { get; set; }
        [MaxLength(50)]
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [MaxLength(50)]
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}

