using Service.Contracts;
using System;
using System.IO;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.OrderPool;

namespace WebLink.Contracts.Services
{
    public class BershkaManualEntryService : IManualEntryService
    {
        public Task DeleteOrderPool(DeleteOrderPoolDTO dto)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResult> SaveOrder(DataEntryRq rq, string username)
        {
            throw new NotImplementedException();
        }

        public Task<OrderPoolDTO> UploadFileOrder(Stream src, ManualEntryOrderFileDTO manualEntryOrderFileDTO)
        {
            throw new NotImplementedException();
        }
    }
}
