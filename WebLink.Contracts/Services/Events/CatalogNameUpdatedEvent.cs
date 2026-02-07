using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public class CatalogNameUpdatedEvent : EQEventInfo
    {
        public int ProjectID { get; set; }

        public int CatalogID { get; set; }

        public string CatalogName { get; set; }

        public string NameBeforeUpdate { get; set; }

        public string NameAfterUpdate { get; set; }

        public int TotalOrders { get; set; }
    }
}
