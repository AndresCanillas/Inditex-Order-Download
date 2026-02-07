using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class HandleOrderReceivedEvent: EQEventHandler<OrderFileReceivedEvent>
	{
		private IEventQueue events;
        private IOrderRepository orderRepo;
        private IProjectRepository projectRepo;
        private IOrderUpdateService orderUpdateService;
        private ICompanyRepository companyRepo;

        public HandleOrderReceivedEvent(
            IEventQueue events, 
            IOrderRepository orderRepo,
            IProjectRepository projectRepo,
            IOrderUpdateService orderUpdateService,
           	ICompanyRepository companyRepo
		)
		{
			this.events = events;
            this.orderRepo = orderRepo;
            this.projectRepo = projectRepo;
            this.orderUpdateService = orderUpdateService;
            this.companyRepo = companyRepo; 
        }

		public override EQEventHandlerResult HandleEvent(OrderFileReceivedEvent e)
		{
			// if company has order workflow dont't process the event
			var company = companyRepo.GetByID(e.CompanyID);
            var order = orderRepo.GetByID(e.OrderID);
            order.HasOrderWorkflow = company.HasOrderWorkflow ?? false;
            orderRepo.Update(order);

            if (order.HasOrderWorkflow)
            {
                events.Send(new StartOrderWorkflowEvent(e));
				return EQEventHandlerResult.OK;
			}

            var project = projectRepo.GetByID(order.ProjectID);
            var flag = 0;

			// order.ProductionType == ProductionType.CustomerLocation && project.UpdateType != UpdateHandlerType.RequestConfirm -> more readable option, require testing
			if (order.ProductionType == ProductionType.CustomerLocation 
                //&& ((int)project.UpdateType < (int)UpdateHandlerType.RequestConfirm || project.UpdateType == UpdateHandlerType.AlwaysNew)
                )
            {
                // ready for print inmediately
                orderUpdateService.Accept(order.ID, OrderStatus.ProdReady);
            }
            if (order.ProductionType != ProductionType.CustomerLocation)
            {
                events.Send(new StartOrderProcessingEvent(e.OrderGroupID, e.OrderID, e.OrderNumber, e.CompanyID, e.BrandID, e.ProjectID));
            }else
            {
                flag++;
            }


            //if (project != null && !String.IsNullOrWhiteSpace(project.OrderPlugin))
            //{

            //    var orderData = new OrderPluginData()
            //    {
            //        CompanyID = order.CompanyID,
            //        BrandID = project.BrandID,
            //        ProjectID = order.ProjectID,
            //        OrderID = order.ID,
            //        OrderGroupID = order.OrderGroupID,
            //        OrderNumber = order.OrderNumber
            //    };

            //    try
            //    {
            //        using (var plugin = pluginManager.GetInstanceByName(project.OrderPlugin))
            //        {
            //            plugin.OrderReceived(orderData);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        // ???: Register notification
            //        log.LogException($"OrderFileReceivedEvent: Error while executing order plugin {project.OrderPlugin} for order {order.ID}.", ex);
            //    }
            //}



            if (flag > 0)
                throw new System.Exception($"OrderFileReceivedEvent: order not handled: Event Data {Newtonsoft.Json.JsonConvert.SerializeObject(e)}" );


			return EQEventHandlerResult.OK;
		}
	}
}