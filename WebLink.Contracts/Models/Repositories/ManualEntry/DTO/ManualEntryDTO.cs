namespace WebLink.Contracts.Models.Repositories.ManualEntry.DTO
{
    public class ManualEntryDTO
    {
        public string Url { get; set; } 
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public int ProjectID { get; set; } 
        public string Name { get; set; }

        public bool EnabledFilePool { get; set; }
        public bool EnabledOrderPool { get; set; }
    }
}
