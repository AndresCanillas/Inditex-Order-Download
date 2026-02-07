using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IGroupFileColumnRepository : IGenericRepository<IGroupFileColumn>
    {
        IEnumerable<GroupFileColumn> GetByProject(int projectid);
        IEnumerable<GroupFileColumn> GetByProject(PrintDB ctx, int projectid);
    }
}
