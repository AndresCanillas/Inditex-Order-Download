using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public  interface IOrderWithArticleDetailedService 
    {
        void Execute(int orderId, 
                     int sendToComapnyID,
                     Action<string, string, int, OrderDetailDTO> callbackDetailed,
                     Action<int, OrderDetailDTO> callbackNotDetailed,
                     Action<int, OrderDetailDTO, int> callbackArtifacts = null);
    }
}
