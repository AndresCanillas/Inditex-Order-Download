using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;

namespace OrderDonwLoadService
{
    public class SyncQrProductToPrintCentral : EQEventHandler<QrProductSyncRequestedEvent>
    {
        private readonly IQrProductSyncService qrProductSyncService;

        public SyncQrProductToPrintCentral(IQrProductSyncService qrProductSyncService)
        {
            this.qrProductSyncService = qrProductSyncService;
        }

        public override EQEventHandlerResult HandleEvent(QrProductSyncRequestedEvent e)
        {
            if (e?.Order == null)
                return EQEventHandlerResult.OK;

            qrProductSyncService.SyncAsync(e.Order).GetAwaiter().GetResult();
            return EQEventHandlerResult.OK;
        }
    }
}
