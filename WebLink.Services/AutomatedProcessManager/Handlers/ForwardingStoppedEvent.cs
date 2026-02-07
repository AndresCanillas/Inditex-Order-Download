using Service.Contracts;
using Service.Contracts.PrintCentral;

namespace WebLink.Services
{
    public class ForwardingStoppedEvent : EQEventHandler<OrderStoppedEvent>
    {
        private IEventQueue events;

        public ForwardingStoppedEvent(
         IEventQueue events)
        {
            this.events = events;
        }

        public override EQEventHandlerResult HandleEvent(OrderStoppedEvent e)
        {
            events.Send(new OrderStoppedEvent(e.OrderGroupID, e.OrderID, e.OrderNumber, e.CompanyID, e.BrandID, e.ProjectID));
            return EQEventHandlerResult.OK;
        }
    }
}
