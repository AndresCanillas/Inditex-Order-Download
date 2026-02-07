using Microsoft.Extensions.Configuration;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class GrupoTendamManualEntryFilterService : IManualEntryFilterService
    {

        private IFactory factory;

        public GrupoTendamManualEntryFilterService(IFactory factory)
        {
            this.factory = factory;
        }

        public async Task<OperationResult> GetOrdersFromFilter(OrderPoolFilter filter)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var result = ctx.OrderPools.Where(o => o.ProjectID == filter.ProjectID &&
                                            o.CategoryCode1 == filter.CategoryCode1 && 
                                            o.ProcessedDate == null);
                return new OperationResult(success:true, message:"OK",data:result.ToList());
            }
        }
    }
}
