using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public class SetIDTFactoryStrategy : ISetIDTFactoryStrategy
    {
        public ProductionType GetProductionType(string sendToCode, IProject project, string articleCode)
        {
            return ProductionType.IDTLocation;
        }
    }
}
