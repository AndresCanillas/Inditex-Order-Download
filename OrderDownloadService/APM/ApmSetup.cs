using Service.Contracts;

namespace OrderDonwLoadService
{
    public class ApmSetup
    {
        public void Setup(IAutomatedProcessManager apm)
        {
            apm.AddHandler<FileReceivedEvent, SendFileToPrintCentral>();
            apm.AddHandler<NotificationReceivedEvent, SendNotificationToPrintCentral>();
            apm.AddHandler<QrProductSyncRequestedEvent, SyncQrProductToPrintCentral>();
        }


    }
}