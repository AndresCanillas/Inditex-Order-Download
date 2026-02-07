using System.Collections.Generic;
using WebLink.Contracts.Models.Delivery.DTO;

namespace WebLink.Contracts.Models.Delivery
{
    // This interface don't implements IBasicTracing because data comes from Print Local
    public interface IDeliveryRepository
    {
        ImportReturnDTO ImportDeliveryNote(DeliveryNoteDTO deliveriNoteDTO);
        List<DeliveryNoteDetailsDTO> GetNotesForOrder(int OrderId);

        void ImportDeliveryFile(string user, string jsondata);
        string GetDeliveryInfo(string ordernumber);
        string GetShip24TrackingInfo(int deliveryNoteId);
        string CreateShip24Tracker(int deliveryNoteId);
    }

}