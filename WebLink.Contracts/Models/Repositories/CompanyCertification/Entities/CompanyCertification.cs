using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Models
{
    public class CompanyCertification : ICompanyCertification
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public bool IsDeleted { get; set; }
        public string SupplierReference { get; set; }
        public string CertificateNumber { get; set; }
        public string CertifyingCompany { get; set; }
        public DateTime? CertificationExpiration { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        public static CompanyCertification FromDto(CompanyCertificationDTO data)
        {
            return new CompanyCertification
            {
                ID = data.ID,
                CompanyID = data.CompanyID,
                IsDeleted = data.IsDeleted,
                SupplierReference = data.SupplierReference,
                CertificateNumber = data.CertificateNumber,
                CertifyingCompany = data.CertifyingCompany,
                CertificationExpiration = data.CertificationExpiration
            };
        }
    }
}
