using Service.Contracts;
using System.Threading.Tasks;

namespace WebLink.Contracts.Services
{
    public interface IManualEntryFilterService
    {
        Task<OperationResult> GetOrdersFromFilter(OrderPoolFilter filter);
    }
}