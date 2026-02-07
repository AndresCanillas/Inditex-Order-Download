namespace WebLink.Contracts.Models.Delivery
{
    public enum DeliveryNoteSource
    {
        Unknown,
        PrintLocal = 10,    // Delivery note created in Print Local
        ExcelFile   = 20,   // Delivery note created in excel file
        SAGE = 30,          // Delivery note created in SAGE    
        Dinamo = 40,        // Delivery note created in Dinamo  
        Other = 90          // Delivery note created in another system
    }
}
