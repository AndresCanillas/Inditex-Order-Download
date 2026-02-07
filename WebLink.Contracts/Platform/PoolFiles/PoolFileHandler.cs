using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Platform.PoolFiles
{
    public interface IPoolFileHandler
    {
        /*
         Action received a ProjectID, and order received in current file
         */
        Task UploadAsync(IProject project, Stream stream, Action<int, IList<IOrderPool>> result = null);

        Task InsertListAsync(IProject project, List<OrderPool> orderPools, Action<int, IList<IOrderPool>> result = null);
            
    }
}
