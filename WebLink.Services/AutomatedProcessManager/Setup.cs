using Service.Contracts;
using Service.Contracts.Infrastructure.Encoding.Tempe;
using Service.Contracts.PrintCentral;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services.Automated
{
    public class SetupPrintCentralProcesses
    {
        private IAppConfig config;
        private IEventQueue events;
        private bool IsQa { get { return config.GetValue<bool>("WebLink.IsQA"); } }

        public SetupPrintCentralProcesses(IAppConfig config, IEventQueue events)
        {
            this.config = config;
            this.events = events;
            // get event before serialized
            events.OnEventRegistered += Events_OnEventRegistered;
        }

        public void Setup(IAutomatedProcessManager apm)
        {
            apm.AddProcess<DBMtto>();
            apm.AddProcess<TempeEpcServiceMtto>();
            apm.AddProcess<ForceCollection>(); // Forces Full GarbageCollection every 5 minutes

            if(!IsQa)
                apm.AddProcess<RemoveOrphanedOrderData>();

            apm.AddProcess<CatalogProcess>();
            apm.AddProcess<ValidationFlowListenerProcess>();
            apm.AddProcess<CheckSageOrderStateProcess>();
            apm.AddProcess<CheckSageArticlesProcess>();
            apm.AddProcess<OrderEmailSender>();
            apm.AddProcess<IrisSyncProcess>();
            apm.AddProcess<PDFLibProcess>();

            // Reverse Flow Daily
            apm.AddProcess<MonoprixReverseEANsProcess>();
            apm.AddProcess<BandFReverseEANsProcess>();
            apm.AddProcess<MangoOrderTrackingDailyReport>();
            apm.AddProcess<BrownieReverseCompositionsProcess>();
            apm.AddProcess<ArmandThieryReverseEANsProcess>();
            apm.AddHandler<MangoOrderTrackingDailyReportEvent, MangoOrderTrackingDailyReportHandler>();

            // to keep backward compatible events fired by printcentral
            apm.AddHandler<OrderFileReceivedEvent, HandleOrderReceivedEvent>();

            apm.AddHandler<APMErrorNotification, CreateErrorNotification>();
            apm.AddHandler<PrinterConnectedEvent, UpdatePrinterState>();

            apm.AddHandler<StartOrderProcessingEvent, SendOrderReceivedEmail>();
            apm.AddHandler<StartOrderProcessingEvent, OrderExistVerifier>();

            apm.AddHandler<OrderExistVerifiedEvent, OrderSetValidator>();

            apm.AddHandler<OrderConflictEvent, SendOrderConflictEmail>();

            apm.AddHandler<OrderValidatedEvent, SendOrderValidatedEmail>();
            apm.AddHandler<OrderValidatedEvent, SendFileDropRequest>();

            if(IsQa)
                apm.AddHandler<SageFileDropAckEvent, PerformOrderBilling>();

            // Print Local Send Event SageFileDropAckEvent, register in PrintCentral/Startup.cs 

            apm.AddHandler<OrderBillingCompletedEvent, CreateOrderDocuments>();
            apm.AddHandler<OrderDocumentsCompletedEvent, CreatePrintPackage>();
            apm.AddHandler<PrintPackageReadyEvent, SendOrderReadyForProductionEmail>();

            //apm.AddHandler<OrderCompletedEvent, SendOrderCompletedEmail>();
            apm.AddHandler<OrderChangeStatusEvent, ChangeOrderStatus>();

            // TODO: create standard event to report ECPs 
            apm.AddHandler<MonoprixDailyEANReportEvent, MonoprixSendEANReportHandler>();
            apm.AddHandler<BandFDailyEANReportEvent, BandFSendRFIDReportHandler>();
            apm.AddHandler<BrownieDailyReportEvent, BrownieSendReportHandler>();
            apm.AddHandler<ArmandThieryDailyEANsReportEvent, ArmandThieryRFIDReportHandler>();

            apm.AddHandler<Service.Contracts.PrintLocal.DuplicatedEPCEvent, SendDuplicatedEPCEmail>();
            apm.AddHandler<FileOrdersManagerEvent, FileOrdersManagerEventHandler>(); 
        }


        private void Events_OnEventRegistered(EQEventInfo e)
        {
            if((e is EntityEvent))
            {
                var evt = (e as EntityEvent);

                if(evt.EntityName == "Order")
                {
                    var entity = (evt.Entity as IOrder);

                    events.Send(new OrderEntityEvent(evt.CompanyID,
                        new Order()
                        {
                            ID = entity.ID,
                            OrderStatus = entity.OrderStatus,
                            IsInConflict = entity.IsInConflict,
                            IsBilled = entity.IsBilled,
                            IsStopped = entity.IsStopped,
                            SageReference = entity.SageReference,
                            ProjectPrefix = entity.ProjectPrefix,
                            SyncWithSage = entity.SyncWithSage,
                            InvoiceStatus = entity.InvoiceStatus,
                            DeliveryStatus = entity.DeliveryStatus,
                            CreditStatus = entity.CreditStatus,
                            MDOrderNumber = entity.MDOrderNumber
                        }
                        , evt.Operation));
                }
            }

        }
    }
}
