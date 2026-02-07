using Service.Contracts;
using Services.Core;
using System;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class CreateOrderDocuments : EQEventHandler<OrderBillingCompletedEvent>
	{
		private IEventQueue events;
		private IOrderDocumentService docSrv;
		private ILogService log;
		private IOrderRepository orderRepo;
        private readonly IFactory factory;


        public CreateOrderDocuments(IEventQueue events, IOrderDocumentService docSrv, ILogService log, IOrderRepository orderRepo, 
			IFactory factory)
		{
			this.events = events;
			this.docSrv = docSrv;
			this.log = log;
			this.orderRepo = orderRepo;
            this.factory = factory;
			        }

		public override EQEventHandlerResult HandleEvent(OrderBillingCompletedEvent e)
		{
            // if order has order workflow dont't process the event
			var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow) 
				return EQEventHandlerResult.OK;

			log.LogMessage($"OrderBillingCompletedEvent Received for OrderID: [{e.OrderID}]");

			bool success = true;
			var task = CreateDocuments(e);

			if (order.ProductionType != ProductionType.CustomerLocation)
			{
				// Only wait for the result of this process if it is an order that will be printed in a Factory.
				success = task.Result;
				if (success == true)
				{
					events.Send(new OrderDocumentsCompletedEvent(e));
					log.LogMessage($"Documents for OrderID: [{e.OrderID}] were created successfully.");
				}
				else
					log.LogMessage($"Error while creating documents for OrderID: [{e.OrderID}], see previous exception in the log.");
			}
			else
			{
				events.Send(new OrderDocumentsCompletedEvent(e));
				log.LogMessage($"Document creation for OrderID: [{e.OrderID}] will execute in background, next process will run immediately... IMPORTANT: In case of error, this background process will NOT be retried.");
				
			}

			return new EQEventHandlerResult() { Success = success };
		}


		private async Task<bool> CreateDocuments(OrderBillingCompletedEvent e)
		{
			try
			{
				
                //ijsanchezm verify is include support files
                var project = factory.GetInstance<IProjectRepository>();

                var includeFiles = project.GetByID(e.ProjectID).IncludeFiles;

                if (includeFiles)
                {
                    log.LogMessage($"Creating Order Details Document for OrderID: [{e.OrderID}]");
                    await docSrv.CreateOrderDetailDocument(e.OrderID);
                }

                log.LogMessage($"Creating Order Preview Document for OrderID: [{e.OrderID}]");
                await docSrv.CreatePreviewDocument(e.OrderID);

                log.LogMessage($"Creating Production Sheet Document for OrderID: [{e.OrderID}]");

                await docSrv.CreateProdSheetDocument(e.OrderID);
				return true;
			}
			catch (Exception ex)
			{
				log.LogException($"Error while creating documents for order [{e.OrderID}]", ex);
				return false;
			}
		}
	}
}
