using System;
using System.Collections.Generic;
using System.Text;


/// <summary>
/// Related objects for order reports
/// </summary>
namespace WebLink.Contracts.Models
{
    #region GetOrderReport
    public enum ConflictFilter
	{
		NoConflict, // like false
		InConflict, // like true
		Ignore// no apply filter
	}

	public enum BilledFilter
	{
		No,// like false
		Yes,// like true
		Ignore// no apply filter

	}

	public enum StopFilter
	{
		NoStoped,// like false
		Stoped,// like true
		Ignore// no apply filter
	}

	public class OrderReportFilter
	{
		public int OrderID { get; set; }
		public int CompanyID { get; set; }
		public int ProjectID { get; set; }
		public int ProductionType { get; set; }
		public OrderStatus OrderStatus { get; set; }
		public string OrderNumber { get; set; }
		public DateTime OrderDate { get; set; }
		public DateTime OrderDateTo { get; set; }
		public ConflictFilter InConflict { get; set; }
		public BilledFilter IsBilled { get; set; }
		public StopFilter IsStopped { get; set; }
		public int PageSize { get; set; }
		public int CurrentPage { get; set; }
		public int TotalRecords { get; set; }
        public int FactoryID { get; set; }
		public string ProviderClientReference { get; set; }
		public string ArticleCode { get; set; }
        public string OrderCategoryClient { get; set; }
        public bool CSV { get; set; }
        public List<string> ProductFields { get; set; }
        public List<string> CompositionFields { get; set; }
        public ReportType ReportType { get; set; }

        public DeliveryStatus DeliveryStatus { get; set; }  

        public OrderReportFilter()
		{
			OrderID = 0;
			CompanyID = 0;
			ProductionType = 0;
			OrderStatus = 0;
			OrderNumber = string.Empty;
			OrderDate = DateTime.Now.AddDays(-30);
			OrderDateTo = DateTime.Now;
			InConflict = ConflictFilter.Ignore; // 0 => All, 1=> no Conflict 2, => with conflict
			IsBilled = BilledFilter.Ignore; // 0 => All, 1 => no Billed, 2 => billed
			IsStopped = StopFilter.Ignore; // 0 => All, 1 => no Stopped, 2 => billed
			PageSize = 20;
			CurrentPage = 1;
			FactoryID = 0;
			ProviderClientReference = string.Empty;
			ArticleCode = string.Empty;
            CSV = false;
            ProductFields = new List<string>();
            CompositionFields = new List<string>();
            ReportType = ReportType.Detailed;
            DeliveryStatus = (DeliveryStatus)-1; // -1 => All
        }
	}

	#endregion GetOrderReport

	#region GetOrderArticles

	public enum OrderActiveFilter
	{
		All,
		Active,
		Rejected,
		Pending,
		NoRejected
	}

	// https://docs.microsoft.com/en-us/dotnet/api/system.enum?view=net-5.0#non-exclusive-members-and-the-flags-attribute
	//[Flags]
	public enum OrderSourceFilter
	{
		NotSet = 0, // TODO: change description to "From Every Where"
		FromWeb = 1,
		FromFtp = 2,
		FromApi = 3,
		FromValidation = 4,
		NotFromValidation = 5,
		FromRepetition = 6
	}

    public enum OrderStatusFilter
    {
        None = 0,
        Received = 1,
        Processed = 2,
        Printing = 3,
        Completed = 6,
        Cancelled = 7,
        InFlow = 20,
        Validated = 30,
        Billed = 40,
        ProdReady = 50,
        All = 999
    }

    public enum IncludeCompositionFilter
    {
        All = 0,
        Yes = 1,
        No = 2
    }


    public class OrderArticlesFilter
	{
		public List<int> OrderID { get; set; }
		public int OrderGroupID { get; set; }
		public string OrderNumber { get; set; }
		public ArticleTypeFilter ArticleType { get; set; }
		public OrderActiveFilter ActiveFilter { get; set; }
		public OrderSourceFilter Source { get; set; }
		public OrderStatusFilter OrderStatus { get; set; }
        public IncludeCompositionFilter IncludeCompo { get; set; }
        public int? SendToCompanyID { get; set; }

        public OrderArticlesFilter()
		{
			OrderID = new List<int>();
			OrderGroupID = 0;
			OrderNumber = string.Empty;
			ArticleType = ArticleTypeFilter.All;
			ActiveFilter = OrderActiveFilter.Active;
			Source = OrderSourceFilter.NotSet;
			OrderStatus = OrderStatusFilter.None;
		}

		public OrderArticlesFilter(OrderArticlesFilter toClone)
		{
			OrderID = toClone.OrderID;
			OrderGroupID = toClone.OrderGroupID;
			OrderNumber = toClone.OrderNumber;
			ArticleType = toClone.ArticleType;
			ActiveFilter = toClone.ActiveFilter;
			Source = toClone.Source;
			OrderStatus = toClone.OrderStatus;
		}

	}

    #endregion GetOrderArticles

    #region LabelGetOrders
    public class OrderByLabelFilter
    {
        public int Count { get; set; }
        public int ProjectID { get; set; }
        public int LabelId { get; set; }
        public string OrderNumber { get; set; }

        public OrderByLabelFilter()
        {
            Count = 25;
            ProjectID = 0;
            LabelId = 0;
            OrderNumber = string.Empty;
        }
    }
    #endregion

    #region CloneOrder
    public class CloneRequest
    {
        public int OrderId { get; set; }
        public bool IsBillable { get; set; }
        public string ArticleCode { get; set; }
    }
    #endregion
}
