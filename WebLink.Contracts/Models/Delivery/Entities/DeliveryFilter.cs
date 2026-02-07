using System;

namespace WebLink.Contracts.Models.Delivery
{
    public class DeliveryFilter
    {
        public string NoteNumber { get; set; }
        public string Vendor { get; set; }
        public string TrackingCode { get; set; }
        public DeliveryNoteStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }  
    }
}
