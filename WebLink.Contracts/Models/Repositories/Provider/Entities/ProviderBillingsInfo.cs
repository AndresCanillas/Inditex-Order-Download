namespace WebLink.Contracts.Models
{
    public class ProviderBillingsInfo : IProviderBillingsInfo
    {
        public int ID { get; set; }
        public int ProviderID { get; set; }
        public int BillingInfoID { get; set; }
    }
}

