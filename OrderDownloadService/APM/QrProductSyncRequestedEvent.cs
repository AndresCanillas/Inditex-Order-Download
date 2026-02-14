using Service.Contracts;
using StructureInditexOrderFile;

namespace OrderDonwLoadService
{
    public class QrProductSyncRequestedEvent : EQEventInfo
    {
        public InditexOrderData Order { get; set; }
    }
}
