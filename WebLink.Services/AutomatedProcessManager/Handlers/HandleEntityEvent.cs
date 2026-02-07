using Service.Contracts;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class HandleEntityEvent : EQEventHandler<EntityEvent>
    {
        private IEventQueue events;

        public HandleEntityEvent(
            IEventQueue events)
        {
            this.events = events;
        }

        public override EQEventHandlerResult HandleEvent(EntityEvent e)
        {
            //Este handler es necesario para que la pantalla principal responda a los cambios de estado de las ordenes
            return EQEventHandlerResult.OK;
        }
    }
}
