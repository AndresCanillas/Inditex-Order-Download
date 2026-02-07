using System;

namespace WebLink.Contracts.Models
{
    public class OrderGroupIndexDTO
	{
		public int OrderGroupID { get; set; }

		public string OrderNumber { get; set; }

		public int CompanyID { get; set; }

		public string CompanyName { get; set; }

		public string CompanyCode { get; set; }

		public int BrandID { get; set; }

		public string BrandName { get; set; }

		public string BrandCode { get; set; }

		public int ProjectID { get; set; }

		public string ProjectName { get; set; }

		public string ProjectCode { get; set; }

		public int QuantitiesRequested { get; set; }

		public int QuantitiesConfirmed { get; set; }

		public int QantitiesValidated { get; set; }

		public DateTime CreatedDate { get; set; }

		public DateTime UpdatedDate { get; set; }


	}
}
