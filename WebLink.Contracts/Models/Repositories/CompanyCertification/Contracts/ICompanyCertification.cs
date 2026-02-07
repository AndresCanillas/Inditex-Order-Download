using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public interface ICompanyCertification : IEntity, IBasicTracing
    {
        string SupplierReference { get; set; }
        string CertificateNumber { get; set; }
        string CertifyingCompany { get; set; }
        DateTime? CertificationExpiration { get; set; }
        int CompanyID { get; set; }
        bool IsDeleted { get; set; }
    }
}
