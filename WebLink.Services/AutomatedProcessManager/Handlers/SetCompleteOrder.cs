using System;
using System.Collections.Generic;
using System.Text;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class ChangeOrderStatus: EQEventHandler<OrderChangeStatusEvent>
	{
		private IEventQueue events;
		private IOrderLogService log;
        private IOrderRepository orderRepo;

		public ChangeOrderStatus(IEventQueue events, IOrderLogService log, IOrderRepository orderRepo)
		{
			this.events = events;
			this.log = log;
            this.orderRepo = orderRepo;
		}

		public override EQEventHandlerResult HandleEvent(OrderChangeStatusEvent e)
		{

            var order = orderRepo.GetByID(e.OrderID);

            if(order.OrderStatus == (OrderStatus)e.OrderStatus) return EQEventHandlerResult.OK;

            orderRepo.ChangeStatus(e.OrderID, (OrderStatus)e.OrderStatus);
            log.Info(e.OrderID, $"Order was Completed");

            return EQEventHandlerResult.OK;

        }
	}
}
