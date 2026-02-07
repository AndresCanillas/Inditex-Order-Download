using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintLocal;
using WebLink.Contracts;

namespace WebLink.Services
{
	public class SendSageFileDropEvent : EQEventHandler<SageFileDropEvent>
    {
        private IEventQueue events;

        public SendSageFileDropEvent(
            IEventQueue events)
        {
            this.events = events;
        }

        public override EQEventHandlerResult HandleEvent(SageFileDropEvent e)
        {
            events.Send(new SageFileDropEvent(e.OrderID,e.SAGEOrderNumber,e.ProjectPrefix)); 

            return EQEventHandlerResult.OK;
        }
    }
}