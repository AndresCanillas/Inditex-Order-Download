using System;
using System.Collections.Generic;
using System.Text;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class SendOrderCompletedEmail: EQEventHandler<OrderCompletedEvent>
	{
		private IEventQueue events;
		private ILogService log;
		private IOrderEmailService mailService;
		private IProjectRepository projectRepo;

		public SendOrderCompletedEmail(IEventQueue events, ILogService log, IOrderEmailService mailService, IProjectRepository projectRepo)
		{
			this.events = events;
			this.log = log;
			this.mailService = mailService;
			this.projectRepo = projectRepo;
		}

		public override EQEventHandlerResult HandleEvent(OrderCompletedEvent e)
		{
			var recipients = projectRepo.GetEmailRecipients(e.ProjectID);
			foreach(var usr in recipients)
			{
				var token = mailService.GetTokenFromUser(usr, EmailType.OrderCompleted);
				if(token != null)
					mailService.AddOrderIfNotExists(token, e.OrderID);
			}
			log.LogMessage($"Registered 'Order Validated' email for order {e.OrderID}/{e.OrderNumber}.");
			return new EQEventHandlerResult() { Success = true };
		}
	}
}
