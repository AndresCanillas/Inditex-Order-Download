using System.Collections.Generic;

namespace WebLink.Contracts.Models.Delivery.DTO
{
    public class DeliveryNoteDTO
    {
        public DeliveryNote DeliveryNote { get; set; }
        public List<PackageDTO> Packages { get; set; }
        public Carrier Carrier { get; set; }    
    }
}
