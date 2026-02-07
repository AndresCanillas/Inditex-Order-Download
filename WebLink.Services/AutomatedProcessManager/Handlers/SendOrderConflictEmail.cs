using System;
using System.Collections.Generic;
using System.Text;
using Service.Contracts;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class SendOrderConflictEmail: EQEventHandler<OrderConflictEvent>
	{
		private ILogService log;
		private IOrderEmailService mailService;
		private IProjectRepository projectRepo;
		private IOrderRepository orderRepo;

		public SendOrderConflictEmail(ILogService log, IOrderEmailService mailService,
			IProjectRepository projectRepo, IOrderRepository orderRepo)
		{
			this.log = log;
			this.mailService = mailService;
			this.projectRepo = projectRepo;
			this.orderRepo = orderRepo;	
		}

		public override EQEventHandlerResult HandleEvent(OrderConflictEvent e)
		{
            // if order has order workflow dont't process the event
            var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow)
                return EQEventHandlerResult.OK;

            var recipients = projectRepo.GetEmailRecipients(e.ProjectID);
			foreach(var usr in recipients)
			{
				var token = mailService.GetTokenFromUser(usr, EmailType.OrderConflict);
				if(token != null)
					mailService.AddOrderIfNotExists(token, e.OrderID);
			}
			log.LogMessage($"Registered 'Order Conflict' email for order {e.OrderID}/{e.OrderNumber}.");
			return new EQEventHandlerResult() { Success = true };
		}
	}
}
