using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Models
{
    public interface ICompanyCertificationRepository : IGenericRepository<ICompanyCertification>
    {
        IEnumerable<CompanyCertification> SaveRange(IEnumerable<CompanyCertificationDTO> data);
    }
}
