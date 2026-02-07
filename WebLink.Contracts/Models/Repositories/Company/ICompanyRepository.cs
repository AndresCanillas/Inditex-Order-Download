using System.Collections.Generic;
using WebLink.Contracts.Models.Repositories.ManualEntry.DTO;

namespace WebLink.Contracts.Models
{
    public interface ICompanyRepository : IGenericRepository<ICompany>
    {
        List<ICompany> GetAll();
        List<ICompany> GetAll(PrintDB ctx);

        IRFIDConfig GetRFIDParams(int companyid);
        IRFIDConfig GetRFIDParams(PrintDB ctx, int companyid);

        byte[] GetLogo(int companyid);
        byte[] GetLogo(PrintDB ctx, int companyid);

        void UpdateLogo(int companyid, byte[] content);
        void UpdateLogo(PrintDB ctx, int companyid, byte[] content);

        List<ICompany> GetProvidersList(int companyId);
        ICompany GetProjectCompany(int projectid);

        ICompany GetSelectedCompany();
        ICompany GetSelectedCompany(PrintDB ctx);

        ICompany GetByCompanyCode(string code);
        ICompany GetByCompanyCode(PrintDB ctx, string code);

        ICompany GetByCompanyCodeOrReference(int projectID, string code);
        ICompany GetByCompanyCodeOrReference(PrintDB ctx, int projectID, string code);

        ICompany GetByCompanyCodeOrReference(int projectID, string code, out int? providerRecordID);
        ICompany GetByCompanyCodeOrReference(PrintDB ctx, int projectID, string code, out int? providerRecordID);

        //bool IsValidProvider(int companyid, int billToCompanyID);

        void AssignRFIDConfig(int companyid, int configid);
        void AssignRFIDConfig(PrintDB ctx, int companyid, int configid);

        void UpdateOrderSorting(List<Company> companies);
        void UpdateOrderSorting(PrintDB ctx, List<Company> companies);

        IEnumerable<ICompany> GetForOwnerOrProvider(int providerID);
        IEnumerable<ICompany> GetForOwnerOrProvider(PrintDB ctx, int providerID);

        List<string> GetContactEmails(int companyID);
        List<string> GetContactEmails(PrintDB ctx, int companyID);

        IEnumerable<ICompany> GetListForExternalManager(int factoryID, bool showAsACompany = true);
        IEnumerable<ICompany> GetListForExternalManager(PrintDB ctx, int factoryID, bool showAsACompany = true);

        List<ManualEntryDTO> GetAvailablesManualEntry(int companyID, int brandID, int projectId);
        IEnumerable<ICompany> GetFullList();

        List<ManualEntryDTO> GetAvailablesOrderPoolManager(int companyID, int brandID, int projectID);
        List<ManualEntryDTO> GetAvailablesPDFExtractors(int companyID, int brandID, int projectID);

        IList<ICompany> FilterByName(string filterBy);

    }
}
