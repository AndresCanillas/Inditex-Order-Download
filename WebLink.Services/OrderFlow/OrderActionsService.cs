using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class OrderActionsService : IOrderActionsService
	{
		private IFactory factory;
		private IOrderRepository orderRepo;
		private IProjectRepository projectRepo;
		private IEventQueue events;

		public OrderActionsService(
			IFactory factory,
			IOrderRepository orderRepo,
			IProjectRepository projectRepo,
			IEventQueue events)
		{
			this.factory = factory;
			this.orderRepo = orderRepo;
			this.projectRepo = projectRepo;
			this.events = events;
		}

		public void StopOrder(int orderid)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				StopOrder(ctx, orderid);
			}
		}


		public void StopOrder(PrintDB ctx, int orderid)
		{
			var order = orderRepo.GetByID(ctx, orderid);
			order.IsStopped = true;
			orderRepo.Update(ctx, order);

			var project = projectRepo.GetByID(ctx, order.ProjectID);
			events.Send(new OrderStoppedEvent(order.OrderGroupID, order.ID, order.OrderNumber, order.CompanyID, project.BrandID, order.ProjectID));
		}

        public void OrderWithDuplicatedEPC(int orderid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                OrderWithDuplicatedEPC(ctx, orderid);
            }
        }

        public void ActiveOrderWithDuplicatedEPC(int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var order = orderRepo.GetByID(ctx, orderid);
                order.DuplicatedEPC = false;
                order.IsStopped = false;
                orderRepo.Update(ctx, order);
            }
        }


        public void OrderWithDuplicatedEPC(PrintDB ctx, int orderid)
        {
            var order = orderRepo.GetByID(ctx, orderid);
            order.DuplicatedEPC = true;
            orderRepo.Update(ctx, order);

            var project = projectRepo.GetByID(ctx, order.ProjectID);
            events.Send(new OrderDuplicatedEPCEvent(order.OrderGroupID, order.ID, order.OrderNumber, order.CompanyID, project.BrandID, order.ProjectID));
        }


        public void ResumeOrder(int orderid)
		{
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				ResumeOrder(ctx, orderid);
			}
		}

		public void ResumeOrder(PrintDB ctx, int orderid)
		{
			var order = orderRepo.GetByID(ctx, orderid);
			order.IsStopped = false;
			orderRepo.Update(ctx, order);

			var project = projectRepo.GetByID(ctx, order.ProjectID);
			events.Send(new OrderResumedEvent(order.OrderGroupID, order.ID, order.OrderNumber, order.CompanyID, project.BrandID, order.ProjectID));


			// after resume order, order need to conitnue workflow, relauch event on on curren state
			// TODO: for now, only reset event for received orders, 
			// if you want to remove this validation, please, ensure other status don't have problem after reset event
			if (order.OrderStatus == OrderStatus.Received) {
				orderRepo.ResetStatusEvent(order.ID);
			}
		}


		public void RejectOrder(int orderid)
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				RejectOrder(ctx, orderid);
			}
		}


		public void RejectOrder(PrintDB ctx, int orderid)
		{
			var order = orderRepo.GetBillingInfo(ctx, orderid);
			events.Send(new OrderRejectedEvent(order.OrderGroupID, order.OrderID, order.OrderNumber, order.CompanyID, order.BrandID, order.ProjectID));
		}


		public void MoveOrder(int orderid, int locationId)
        {
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				MoveOrder(ctx, orderid, locationId);
			}
		}


		public void MoveOrder(PrintDB ctx, int orderid, int locationId)
		{
			var order = orderRepo.GetByID(ctx, orderid);
			order.LocationID = locationId;
			orderRepo.Update(ctx, order);

			var project = projectRepo.GetByID(ctx, order.ProjectID);
            if (order.OrderStatus == OrderStatus.Printing || order.OrderStatus == OrderStatus.ProdReady)
            {
                events.Send(new OrderMovedEvent(order.OrderGroupID, order.ID, order.OrderNumber, order.CompanyID, project.BrandID, order.ProjectID, order.LocationID.Value));
                events.Send(new PrintPackageReadyEvent(order.ID, order.LocationID.Value, order.ProjectPrefix));
            }
        }
	}
}
