namespace WebLink.Contracts.Models
{
    public interface IProviderTreeView
	{
		int CompanyID { get; set; }
		string Name { get; set; }
		string ClientReference { get; set; }
		int? ParentCompanyID { get; set; }
		int? TopParentID { get; set; }
		string Parents { get; set; }
		int? DefaultBillingLocationID { get; set; }
		int? DefaultProductionLocationID { get; set; }
		string Currency { get; set; }
		int? SLADays { get; set; }
		int? ProviderRecordID { get; set;	 }

	}
}
