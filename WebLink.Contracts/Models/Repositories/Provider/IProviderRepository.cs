using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IProviderRepository
    {
        ICompanyProvider GetByID(int providerRecordID);
        ICompanyProvider GetByID(PrintDB ctx, int providerRecordID);

        List<ProviderDTO> GetByCompanyID(int companyid);
        List<ProviderDTO> GetByCompanyIDME(int companyid);

        CompanyProvider GetByProviderID(int companyid, int providerid);
        CompanyProvider GetByProviderID(PrintDB ctx, int companyid, int providerid);

        ProviderDTO GetProvider(int companyid, int providerid);
        ProviderDTO GetProvider(PrintDB ctx, int companyid, int providerid);

        ICompanyProvider GetProviderBy(int companyId, int providerCompanyID);
        ICompanyProvider GetProviderBy(PrintDB ctx, int companyId, int providerCompanyID);

        void UpdateProvider(ProviderDTO data);
        void UpdateProvider(PrintDB ctx, ProviderDTO data);

        int AddProviderToCompany(int companyid, ProviderDTO provider);
        int AddProviderToCompany(PrintDB ctx, int companyid, ProviderDTO provider);

        void RemoveProviderFromCompany(int providerid);
        void RemoveProviderFromCompany(PrintDB ctx, int providerid);

        ProviderTreeView GetBillingProviderInfo(int companyId, int billingToId);
        ProviderTreeView GetBillingProviderInfo(PrintDB ctx, int companyId, int billingToId);
        List<ProviderDTO> GetByCompanyIDWithArticleDetails(int companyid);
        ICompanyProvider GetProviderByClientReference(int companyId, string clientReference);
        ICompanyProvider GetProviderByClientReference(PrintDB ctx, int companyId, string clientReference);

        string GetProviderLocationName (string clientReference, int companyid); 
        string GetProviderLocationName (PrintDB ctx,string clientReference, int companyid);  
    }
}
