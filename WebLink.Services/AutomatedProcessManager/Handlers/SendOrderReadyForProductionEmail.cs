using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class SendOrderReadyForProductionEmail: EQEventHandler<PrintPackageReadyEvent>
	{
		private IEventQueue events;
		private ILogService log;
		private IOrderEmailService mailService;
		private IOrderRepository orderRepo;
        //private IProjectRepository projectRepo;
        private IOrderNotificationManager notificationManager;

		public SendOrderReadyForProductionEmail(IEventQueue events, ILogService log, IOrderEmailService mailService, IOrderRepository orderRepo, IOrderNotificationManager notificationManager)
		{
			this.events = events;
			this.log = log;
			this.mailService = mailService;
			this.orderRepo = orderRepo;
			this.notificationManager = notificationManager;
		}

		public override EQEventHandlerResult HandleEvent(PrintPackageReadyEvent e)
		{
            var order = orderRepo.GetByID(e.OrderID, true);
            notificationManager.RegisterEmailNotificationForOrder(order, EmailType.OrderReadyForProduction);
			//var orderInfo = orderRepo.GetProjectInfo(e.OrderID);
   //         var recipients = notificationManager.GetIDTStakeholders(orderInfo.ProjectID, orderInfo.LocationID);

			//foreach (var usr in recipients)
			//{
			//	var token = mailService.GetTokenFromUser(usr, EmailType.OrderReadyForProduction);
			//	if (token != null)
			//		mailService.AddOrderIfNotExists(token, e.OrderID);
			//}

			log.LogMessage($"Registered 'Order Ready For Production' email for order {e.OrderID} / {order.OrderNumber}.");
			return new EQEventHandlerResult() { Success = true };
		}
	}
}
