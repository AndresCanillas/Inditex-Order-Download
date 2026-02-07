using Service.Contracts;
using Service.Contracts.PrintCentral;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class HandlePrintPackageReadyEvent : EQEventHandler<PrintPackageReadyEvent>
    {
        private IEventQueue events;

        public HandlePrintPackageReadyEvent(
            IEventQueue events)
        {
            this.events = events;
        }

		public override EQEventHandlerResult HandleEvent(PrintPackageReadyEvent e)
		{
			events.Send(new PrintPackageReadyEvent(e.OrderID,e.ProductionLocation,e.ProjectPrefix));
			return EQEventHandlerResult.OK;
		}
    }
}