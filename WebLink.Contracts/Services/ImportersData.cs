namespace WebLink.Contracts.Services
{
    public class Importers
    {
        public int ID { get; set; }  
        public string ImporterData { get; set; } 
        public string MadeIn { get; set; }

        public bool IsNew { get; set; } = false;    
        public string VendorCode { get; set; }  

    }

    public class ImportersMadeIN
    { 
        public string English { get; set; } 
    }
}