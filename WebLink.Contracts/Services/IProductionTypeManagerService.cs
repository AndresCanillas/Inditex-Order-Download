using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IProductionTypeManagerService
    {
        ProductionType GetProductyonType(string sendToCode, IProject project, string articleCode);
    }
}
