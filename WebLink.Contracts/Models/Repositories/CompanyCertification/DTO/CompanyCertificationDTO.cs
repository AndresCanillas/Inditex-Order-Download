using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class CompanyCertificationDTO
    {
        public int ID { get; set; }
        [Required]
        public string SupplierReference { get; set; }
        [Required]
        public string CertificateNumber { get; set; }
        [Required]
        public string CertifyingCompany { get; set; }
        [Required]
        public DateTime? CertificationExpiration { get; set; }
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public bool IsDeleted { get; set; } = false;
        public CompanyCertificationDTO ToDto()
        {
            return new CompanyCertificationDTO
            {
                ID = this.ID,
                CompanyID = this.CompanyID,
                IsDeleted = this.IsDeleted,
                SupplierReference = this.SupplierReference,
                CertificateNumber = this.CertificateNumber,
                CertifyingCompany = this.CertifyingCompany,
                CertificationExpiration = this.CertificationExpiration
            };
        }
    }


}
