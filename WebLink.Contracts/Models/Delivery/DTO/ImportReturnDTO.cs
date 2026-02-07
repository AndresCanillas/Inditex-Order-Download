namespace WebLink.Contracts.Models.Delivery.DTO
{
    public class ImportReturnDTO
    {
        public int DeliveryNoteID { get; set; }    
        public int CarrierID { get; set; } 
        public bool Success { get; set; }   
        public string Message { get; set; } 
    }
}
