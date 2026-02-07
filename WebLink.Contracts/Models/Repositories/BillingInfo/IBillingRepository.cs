using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IBillingRepository
    {
        List<IBillingInfo> GetByProviderID(int id);
        List<IBillingInfo> GetByProviderID(PrintDB ctx, int id);
    }
}
