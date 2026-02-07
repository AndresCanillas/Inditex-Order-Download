using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	/* ===================================================================================
	 * Sends any pending emails to its recipients
	 * ===================================================================================*/
	public class OrderEmailSender : IAutomatedProcess
	{
		private IFactory factory;
		private bool isQA;
        private double delta;
        private ILogService log;

		public OrderEmailSender(IFactory factory, IAppConfig config, ILogService log)
		{
			this.factory = factory;
			isQA = config.GetValue<bool>("WebLink.IsQA");
            delta = config.GetValue("WebLink.Email.Processes.OrderEmailSender.FrequencyInSeconds", 1800);
            this.log = log;//.GetSection("OrderEmailSender");

        }

		public TimeSpan GetIdleTime()
		{
			return TimeSpan.FromSeconds(delta);
		}

		public void OnLoad() { }

		public void OnUnload() { }

		public void OnExecute()
		{
			try
			{
                log.LogMessage("Executing Task OrderEmailSender");
				var emailService = factory.GetInstance<IOrderEmailService>();
				//AddPendingForValidationEmails(emailService);
				emailService.SendNotifications().Wait();
				PerformItemMaintenance();
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
		}

		private void AddPendingForValidationEmails(IOrderEmailService emailService)
		{
			var projects = new Dictionary<int, pdt>();
			var orderRepo = factory.GetInstance<IOrderRepository>();
			var projectRepo = factory.GetInstance<IProjectRepository>();
			var orders = orderRepo.GetOrdersByFilter(new OrderFilter() { OrderStatus = OrderStatus.InFlow });
			foreach(var order in orders)
			{
				if(!projects.TryGetValue(order.ProjectID, out var project))
				{
					var p = projectRepo.GetByID(order.ProjectID, true);
					var recipients = projectRepo.GetEmailRecipients(order.ProjectID);
					project = new pdt()
					{
						ProjectID = p.ID,
						EnableValidationWorkflow = p.EnableValidationWorkflow,
						ProjecRecipients = recipients
					};
					projects.Add(order.ProjectID, project);
				}
				if (project.EnableValidationWorkflow)
				{
					foreach(var usr in project.ProjecRecipients)
					{
						var token = emailService.GetTokenFromUser(usr, EmailType.OrderPendingValidation);
						if(token != null)
							emailService.AddOrderIfNotExists(token, order.ID);
					}
				}
			}
		}

		class pdt
		{
			public int ProjectID;
			public bool EnableValidationWorkflow;
			public List<string> ProjecRecipients;
		}

		private void PerformItemMaintenance()
		{
			using (var ctx = factory.GetInstance<PrintDB>())
			{
				var threshold = DateTime.Now.AddMonths(-6);
				var items = from item in ctx.EmailTokenItems where item.Notified == true && item.NotifyDate < threshold select item;
				foreach (var item in items)
					ctx.EmailTokenItems.Remove(item);
				ctx.SaveChanges();

				var errorItems = from item in ctx.EmailTokenItemErrors where item.Notified == true && item.NotifyDate < threshold select item;
				ctx.EmailTokenItemErrors.RemoveRange(errorItems);
				ctx.SaveChanges();
			}
		}
	}
}
