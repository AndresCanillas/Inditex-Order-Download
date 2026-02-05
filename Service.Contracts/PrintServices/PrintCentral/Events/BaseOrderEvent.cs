namespace Service.Contracts.PrintCentral
{
	public abstract class BaseOrderEvent : EQEventInfo
	{
		public int OrderGroupID { get; set; }
		public int OrderID { get; set; }
		public string OrderNumber { get; set; }
		public int BrandID { get; set; }
		public int ProjectID { get; set; }

        public BaseOrderEvent() 
        { }

        public BaseOrderEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
		{
			OrderGroupID = orderGroupID;
			OrderID = orderID;
			OrderNumber = orderNumber;
			CompanyID = companyid;
			BrandID = brandid;
			ProjectID = projectid;

		}

		public BaseOrderEvent(BaseOrderEvent e)
		{
			OrderGroupID = e.OrderGroupID;
			OrderID = e.OrderID;
			OrderNumber = e.OrderNumber;
			CompanyID = e.CompanyID;
			BrandID = e.BrandID;
			ProjectID = e.ProjectID;
		}
	}
}
