//using Microsoft.Extensions.DependencyInjection;
//using Service.Contracts;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using WebLink.Contracts;

//namespace WebLink.Services.Automated
//{
//    public class HandleOrderReceived : EQEventHandler<OrderReceivedEvent>
//    {
//		private IFactory factory;
//		private IOrderUpdateService orderUpdateService;
//		private IEventQueue events;
//		private ILogService log;

//		public HandleOrderReceived(
//			IFactory factory,
//			IOrderUpdateService orderUpdateService,
//			IEventQueue events,
//			ILogService log)
//		{
//			this.factory = factory;
//			this.orderUpdateService = orderUpdateService;
//			this.events = events;
//			this.log = log;
//		}

//		public override EQEventHandlerResult HandleEvent(OrderReceivedEvent e)
//		{
//			var response = orderUpdateService.Execute(e.OrderGroupID, e.OrderID, e.OrderNumber, e.ProjectID, e.BrandID);

//			var result = new EQEventHandlerResult() { Success = false };

//			switch (response)
//			{
//				case 0: // New Order
//				case 1: // Auto Updated
//					result.Success = true;
//					events.Send(new OrderExistVerifiedEvent(e));
//					//log.LogMessage($"Order (ID:{e.OrderID}) has been accepted.");
//					break;

//				case 2:
//					result.Success = true;
//					events.Send(new OrderDocumentsCompletedEvent(e)); // by pass for ProductionLocation.CustomerLocation
//					break;

//				case -2: // Rejected - Disabled Updates
//				case -3: // Rejected - Is the same order
//					result.Success = true;
//					break;

//				case -4: // conflict detected
//					events.Send(new OrderConflictEvent(e));
//					result.Success = true; // cancel event, triggered after conflict resolution
//					break;

//				case 3:
//					result.Success = true; // cancel event
//					break;

//				case -1: // Wait
//				default: // Unknown
//					result.Success = false;
//					break;
//			}

//			return result;
//		}
//	}
//}
