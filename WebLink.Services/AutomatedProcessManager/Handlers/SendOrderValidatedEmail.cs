using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class SendOrderValidatedEmail: EQEventHandler<OrderValidatedEvent>
	{
		private ILogService log;
		private IOrderNotificationManager notificationMng;
        private IOrderRepository orderRepo;
		

        public SendOrderValidatedEmail(ILogService log, IOrderNotificationManager notificationMng, IOrderRepository orderRepo)
		{
			this.log = log;
			this.notificationMng = notificationMng;
            this.orderRepo = orderRepo;
		}

		public override EQEventHandlerResult HandleEvent(OrderValidatedEvent e)
		{
            // if order has order workflow dont't process the event
            var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow)
                return EQEventHandlerResult.OK;

            notificationMng.RegisterEmailNotificationForOrder(order, EmailType.OrderValidated);
			//var recipients = projectRepo.GetEmailRecipients(e.ProjectID);
			//foreach(var usr in recipients)
			//{
			//	var token = mailService.GetTokenFromUser(usr, EmailType.OrderValidated);
			//	if(token != null)
			//		mailService.AddOrderIfNotExists(token, e.OrderID);
			//}
			log.LogMessage($"Registered 'Order Validated' email for order {e.OrderID}/{e.OrderNumber}.");
			return new EQEventHandlerResult() { Success = true };
		}
	}
}
