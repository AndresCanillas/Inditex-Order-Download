using Service.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebLink.Contracts.Services
{
    public interface IGrupoTendamWriter
    {
        Task<OperationResult> WriteTendamFile(DataEntryRq rq, string fileName, List<TendamMapping> mappings);
    }
}