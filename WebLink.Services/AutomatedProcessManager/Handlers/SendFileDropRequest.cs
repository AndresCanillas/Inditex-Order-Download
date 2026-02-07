using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class SendFileDropRequest : EQEventHandler<OrderValidatedEvent>
	{
		private IFactory factory;
		private IOrderRegisterInERP registerInSage;
		private IOrderRepository orderRepo;
		private IEventQueue events;
		private ILogService log;
		private IAppConfig config;
		private bool IsQa { get { return config.GetValue<bool>("WebLink.IsQA"); } }
		
		public SendFileDropRequest(
			IFactory factory,
			IOrderRegisterInERP registerInSage,
			IOrderRepository repo,
			IEventQueue events,
			ILogService log,
			IAppConfig config
		)
		{
			this.factory = factory;
			this.registerInSage = registerInSage;
			this.orderRepo = repo;
			this.events = events;
			this.log = log;
			this.config = config;
		}

		public override EQEventHandlerResult HandleEvent(OrderValidatedEvent e)
		{
            // if order has order workflow dont't process the event
            var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow)
                return EQEventHandlerResult.OK;

            try
			{
				using (var ctx = factory.GetInstance<PrintDB>())
				{
					var orderInfo = orderRepo.GetBillingInfo(ctx, e.OrderID);
					var orderData = registerInSage.UpdateOrderReference(ctx, orderInfo);

					if (registerInSage.CanBill(orderInfo) && orderInfo.ProductionType == ProductionType.IDTLocation && orderInfo.BillToSyncWithSage == true && !string.IsNullOrEmpty(orderInfo.BillToSageRef))
					{
						if (IsQa)
						{
							log.LogMessage("Simulando FileDrop para continuar proceso de facturacion localmente  de la orden [{0}]", orderInfo.OrderID);
							events.Send(new SageFileDropAckEvent() { OrderID = e.OrderID, SAGEOrderNumber = orderData.MDOrderNumber, ProjectPrefix = orderData.ProjectPrefix });
						}
						else
						{
							log.LogMessage("Enviando Evento para colocar archivos en Servidor sage y continuar proceso de facturacion de la orden [{0}]", orderInfo.OrderID);
							events.Send(new SageFileDropEvent(e.OrderID, orderData.MDOrderNumber, orderData.ProjectPrefix));
						}
					}
					else
					{
						registerInSage.MarkAsBilled(ctx, orderInfo);
						events.Send(new OrderBillingCompletedEvent(e));
					}

					return EQEventHandlerResult.OK;
				}
			}catch (Exception _ex)
			{
				log.LogException($"SendFileDropRequest Failed for [{e.OrderID}]", _ex);
				throw _ex;
			}

		}
	}
}
