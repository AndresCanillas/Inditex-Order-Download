using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class PerformOrderBilling : EQEventHandler<SageFileDropAckEvent>
	{
		private IFactory factory;
		private IEventQueue events;
		private IOrderRepository repo;
		private ILogService log;
		private static object _locker = new Object();

		public PerformOrderBilling(IFactory factory, IEventQueue events, IOrderRepository repo, ILogService log)
		{
			this.factory = factory;
			this.events = events;
			this.repo = repo;
			this.log = log;
		}

		public override EQEventHandlerResult HandleEvent(SageFileDropAckEvent e)
		{
			// TODO: Perform billing process...
			/* 1) Check if billing is enabled for this order, if not we are done (raise OrderBillingCompleted)
			 * 2) Check if a bill has already been created for this order, if not, then create it and delay event a couple minutes
			 * 3) Check if MD middleware has completed processing the bill created earlier in step 2, if not, delay notification a couple minutes
			 *		3.1) If bill is donde over at MD, retrieve all required data (MDOrderNumber, documents, etc.), and mark the bill as complete
			 * 4) Bill is complete at this point, so raise OrderBillingCompleted
			 */

            // if order has order workflow dont't process the event
            var order = repo.GetByID(e.OrderID);
			if (order.HasOrderWorkflow)
			{
                events.Send(new SageFileCompletedEvent(e.OrderID));
                return EQEventHandlerResult.OK;
			}

            log.LogMessage("SageFileDropAckEvent Received {0}", e.OrderID);
			EQEventHandlerResult responseEvent = EQEventHandlerResult.OK; // default to end event

			lock (_locker)
			{
				try
				{
					IOrderRegisterInERP registerInSage = factory.GetInstance<IOrderRegisterInERP>();
					var orderInfo = repo.GetProjectInfo(e.OrderID);
					var response = registerInSage.Execute(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.ProjectID, orderInfo.BrandID);

					switch (response)
					{
						case 0:
							// Finally move the order along by raising the OrderBillingCompleted event
							events.Send(new OrderBillingCompletedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
							log.LogMessage($"Billing for order [{e.OrderID}] completed.");
							break;

						case 1:

							events.Send(new OrderBillingCompletedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
							log.LogMessage($"Order [{e.OrderID}] not required to be billed, can continue.");
							break;

						case -1:
							// TODO: log event cancelled, order status was reset
							log.LogMessage("Order Cannot Register for billing, because Order status was changed for OrderID [{0}]", e.OrderID);
							break;

						default:
							// TODO: log unkonw issue, event was delayed
							// TODO: notify administrators problem to run process "BillingProcess"
							log.LogMessage("SageFileDropAckEvent will be repeated for Order ID [{0}]", e.OrderID);
							responseEvent.Success = false;
							break;
					}
				} catch (Exception ex)
				{
					log.LogException($"PerformOrderBilling - OrderID: [{e.OrderID}]", ex);
					throw ex;
				}
			}

			return responseEvent;
		}
	}
}
