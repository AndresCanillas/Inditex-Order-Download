using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public interface IExtractSizeRangeService
    {
        List<string> ExtractOrderSizesListByLines(IEnumerable<OrderDetailDTO> detail, int projectId);
        List<string> ExtractOrderSizesListByUseInSizes(IEnumerable<OrderDetailDTO> detail, int projectId);

    }
}
