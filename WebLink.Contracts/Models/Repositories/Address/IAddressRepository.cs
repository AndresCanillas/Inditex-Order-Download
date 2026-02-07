using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IAddressRepository : IGenericRepository<IAddress>
    {
        void AddToCompanyAddress(int companyId, int id);
        void AddToCompanyAddress(PrintDB ctx, int companyId, int id);

        List<IAddress> GetByCompany(int id);
        List<IAddress> GetByCompany(PrintDB ctx, int id);

        IAddress GetDefaultByCompany(int Id);
        IAddress GetDefaultByCompany(PrintDB ctx, int Id);

        void SetDefaultAddress(int companyId, int id, bool isDefault);
        void SetDefaultAddress(PrintDB ctx, int companyId, int id, bool isDefault);
    }
}
