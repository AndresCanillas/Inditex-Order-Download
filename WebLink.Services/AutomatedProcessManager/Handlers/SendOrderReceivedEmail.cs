using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Service.Contracts;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class SendOrderReceivedEmail: EQEventHandler<StartOrderProcessingEvent>
	{
		private IOrderRepository orderRepo;
		private IOrderNotificationManager notificationMng;

        public SendOrderReceivedEmail(
            IOrderRepository orderRepo,
            IOrderNotificationManager notificationMng
        )
		{
			this.orderRepo = orderRepo;
            this.notificationMng = notificationMng;
		}

		public override EQEventHandlerResult HandleEvent(StartOrderProcessingEvent e)
		{
            if (e.Source == EventSource.Remote)
                return EQEventHandlerResult.OK;

            // if order has order workflow dont't process the event
            var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow)
                return EQEventHandlerResult.OK;

            notificationMng.RegisterReceivedNotification(order);
			//log.LogMessage($"Registered 'Order Received' email for order {e.OrderID}/{e.OrderNumber}.");
			return EQEventHandlerResult.OK;
		}
    }
}
