using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface ILocationRepository : IGenericRepository<ILocation>
    {
        List<ILocation> GetByCompanyID(int companyid);
        List<ILocation> GetByCompanyID(PrintDB ctx, int companyid);

        List<ILocation> GetIDTFactories();
        List<ILocation> GetIDTFactories(PrintDB ctx);

        IEnumerable<ILocation> GetFactoriesInUseFor(IUserData userData);
        IEnumerable<ILocation> GetFactoriesInUseFor(PrintDB ctx, IUserData userData);

    }
}
