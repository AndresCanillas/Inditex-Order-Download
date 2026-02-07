using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public class CatalogDataUpdatedEvent : EQEventInfo
    {
        public int ProjectID { get; set; }

        public int CatalogID { get; set; }

        public string CatalogName { get; set; }

        public string JsonDataBeforeUpdate { get; set; }

        public string JsonDataAfterUpdate { get; set; }

        public int TotalOrders { get; set; }

        // TODO: definir una forma para determinar que cambio en la data,
        // por ahora pudiera ser lista de filas modificadas

        public int RowID {get;set;}


    }
}
