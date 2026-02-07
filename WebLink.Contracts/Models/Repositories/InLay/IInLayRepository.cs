using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IInLayRepository
    {
        IEnumerable<IInLay> GetInlays(PrintDB ctx, int Projectid, int BrandId, int CompanyId);
        IEnumerable<IInlayConfig> GetInLayConfig(PrintDB ctx, int Projectid, int BrandId, int CompanyId);
    }
}