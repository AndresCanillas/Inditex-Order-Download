using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    // Structure Changes impact over all orders in proyect
    public class CatalogStructureUpdatedEvent : EQEventInfo
    {
        public int ProjectID { get; set; }

        public int CatalogID { get; set; }

        public string CatalogName { get; set; }

        public string DefinitionBeforeUpdate { get; set; }

        public string DefinitionAfterUpdate { get; set; }

        public int TotalOrders { get; set; }

        public CatalogStructureUpdatedEvent()
        {

        }

    }
}
