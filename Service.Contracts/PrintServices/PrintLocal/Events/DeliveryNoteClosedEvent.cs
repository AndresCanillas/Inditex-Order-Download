using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintServices.PrintLocal.Events
{
    public class DeliveryNoteClosedEvent : EQEventInfo
    {
        public int DeliveryNoteID { get; set; }
        

        public DeliveryNoteClosedEvent(int deliveryNoteId)
        {
            DeliveryNoteID = deliveryNoteId;
        }
    }
}
