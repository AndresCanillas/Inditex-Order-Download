using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface ICategoryRepository : IGenericRepository<ICategory>
    {
        List<ICategory> GetByProject(int id);
        List<ICategory> GetByProject(PrintDB ctx, int id);
    }
}
