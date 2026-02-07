using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using WebLink.Contracts;

namespace WebLink.Services.Automated
{
    public class OrderResume : EQEventHandler<OrderResumedEvent>
	{
		private IEventQueue events;
		private ILogService log;
		private IOrderSetValidatorService orderSerValidatorService;

		public OrderResume(IEventQueue events, ILogService log, IOrderSetValidatorService orderSerValidatorService)
		{
			this.events = events;
			this.log = log;
			this.orderSerValidatorService = orderSerValidatorService;
		}

		public override EQEventHandlerResult HandleEvent(OrderResumedEvent e)
		{
			return new EQEventHandlerResult() { Success = true };
		}
	}
}
