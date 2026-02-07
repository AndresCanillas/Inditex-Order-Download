using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintLocal;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class ProcessOrderStatusChange: EQEventHandler<PLOrderStatusChangeEvent>
	{
		private IOrderRepository repo;
		private IOrderLogService log;
        private IEventQueue events;
        private IProjectRepository projects;    


		public ProcessOrderStatusChange(IOrderRepository repo, IOrderLogService log, IEventQueue events, ProjectRepository projects)
		{
			this.repo = repo;
			this.log = log;
            this.events = events;
            this.projects = projects;
		}

		public override EQEventHandlerResult HandleEvent(PLOrderStatusChangeEvent e)
		{
			var status = ConvertFromPrintLocalStatus(e.Status);
			if(status != OrderStatus.None)
			{
                log.Log(e.OrderID, $"OrderStatus changed by PrintLocal. New Status: {(int)status} - {status.GetText()} ", OrderLogLevel.DEBUG);
                repo.ChangeStatus(e.OrderID, status);

                if (status == OrderStatus.Completed)
                {
                    var order = repo.GetByID(e.OrderID, true);
                    var project = projects.GetByID(order.ProjectID);

                    events.Send(new OrderCompletedEvent(order.OrderGroupID, e.OrderID, e.OrderNumber, e.CompanyID, project.BrandID, order.ProjectID));
                } 
			}
			return new EQEventHandlerResult();
		}

		private OrderStatus ConvertFromPrintLocalStatus(PLOrderStatus status)
		{
			switch (status)
			{
				case PLOrderStatus.Printing:
					return OrderStatus.Printing;
				case PLOrderStatus.Completed:
					return OrderStatus.Completed;
				case PLOrderStatus.Cancelled:
					return OrderStatus.Cancelled;
				default:
					return OrderStatus.None;
			}
		}
	}
}

