using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public interface IManualEntryGrouppingService
    {
        Task<OperationResult> GroupOrders (OrderPoolGrouping orderPoolGrouping);
    }
}
