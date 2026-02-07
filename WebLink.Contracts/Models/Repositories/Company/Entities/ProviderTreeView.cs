using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WebLink.Contracts.Models
{
	public class ProviderTreeView : IProviderTreeView
    {
		public int CompanyID { get; set; }
		public string Name { get; set; }
		public string ClientReference { get; set; }
		public int? ParentCompanyID { get; set; }
		public int? TopParentID { get; set; }
		public string Parents { get; set; }
		public int? DefaultBillingLocationID { get; set; }
		public int? DefaultProductionLocationID { get; set; }
		public string Currency { get; set; }
		public int? SLADays { get; set; }
		public int? ProviderRecordID { get; set; }
	}
}
