using Service.Contracts;
using System.IO;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.OrderPool;

namespace WebLink.Contracts.Services
{
    public interface IManualEntryService
    {
        Task<OperationResult> SaveOrder(DataEntryRq rq, string username);
        Task<OrderPoolDTO> UploadFileOrder(Stream src, ManualEntryOrderFileDTO manualEntryOrderFileDTO);
        Task DeleteOrderPool (DeleteOrderPoolDTO dto); 
       
    }
}