using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
    public class OrderSetValidator : EQEventHandler<OrderExistVerifiedEvent>
	{
		private IFactory factory;
		private IEventQueue events;
		private IOrderRepository orderRepo;

		public OrderSetValidator(IFactory factory, IEventQueue events, IOrderRepository orderRepo)
		{
			this.factory = factory;
			this.events = events;
			this.orderRepo = orderRepo;
		}

		public override EQEventHandlerResult HandleEvent(OrderExistVerifiedEvent e)
		{
            // if order has order workflow dont't process the event
            var order = orderRepo.GetByID(e.OrderID);
            if (order.HasOrderWorkflow)
                return EQEventHandlerResult.OK;

            if (!e.ContinueProcessing) 
				return EQEventHandlerResult.OK;

			var setValidatorService = factory.GetInstance<IOrderSetValidatorService>();

			var response = setValidatorService.Execute(e.OrderGroupID, e.OrderID, e.OrderNumber, e.ProjectID, e.BrandID);

			var result = new EQEventHandlerResult() { Success = false };

			switch (response)
			{

				case 0: // validator was assigned
					result.Success = true;

					// not fire event, order state will be change manually
					break;

				case 1: // validator is not required
					result.Success = true;

					// next step
					events.Send(new OrderValidatedEvent(e));
					break;
			}

			return result;
		}
	}
}
