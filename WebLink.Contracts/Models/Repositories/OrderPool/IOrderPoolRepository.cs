using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IOrderPoolRepository : IGenericRepository<IOrderPool>
    {
        OrderPoolDTO GetOrderByOrderNumber(string ordernumber, int projectid);
        List<OrderPoolDTO> GetOrdersByCompanyId(int companyid, int projectid);
        List<OrderPoolDTO> GetOrdersByProject(int projectid);
        IOrderPool CheckIfExist(IOrderPool order);
    }
}
