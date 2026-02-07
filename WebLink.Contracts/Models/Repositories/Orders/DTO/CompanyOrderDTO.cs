using Service.Contracts;
using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public class CompanyOrderDTO
    {
        public int OrderID { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string OrderNumber { get; set; }
        public string MDOrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? OrderDueDate { get; set; }
        public string UserName { get; set; }
        public DocumentSource Source { get; set; }
        public int? Quantity { get; set; }
        public ProductionType ProductionType { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string OrderStatusText { get; set; }
        public bool ConfirmedByMD { get; set; }
        public int? LocationID { get; set; }
        public string LocationName { get; set; }
        public int BrandID { get; set; }
        public string Brand { get; set; }
        public int SendToCompanyID { get; set; }        // workshop companyid		
        public string SendTo { get; set; }       // workshop name
        public string SendToCode { get; set; }       // workshop code
        public int? BillToCompanyID { get; set; }
        public string BillTo { get; set; }
        public string BillToCode { get; set; }
        public int ProjectID { get; set; }
        public string Project { get; set; }
        public string Fabric { get; set; }
        public float ValidationProgress { get; set; }
        public IEnumerable<NextOrderState> NextStates { get; set; }
        public bool IsStopped { get; set; }
        public bool IsBilled { get; set; }
        public bool IsInConflict { get; set; }
        public string CompanyCode { get; set; }
        public string BrandCode { get; set; }
        public string ProjectCode { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int OrderGroupID { get; set; }
        public bool IsGroup { get; set; }
        public string ArticleCode { get; set; }
        public string ArticleName { get; set; }
        public int ArticleID { get; set; }
        public bool IsItem { get; set; }
        public int OrderDataID { get; set; }
        public bool RequireValidation { get; set; }
        public int? ParentOrderID { get; set; }
        public int AggregatedQuantity { get; set; }
        public string PackCode { get; set; }
        public int PrintJobId { get; set; }
        public string OrderCategoryClient { get; set; }
        public long? WFItemID { get; set; }

        public string ExceptionMessage { get; set; }

        public DeliveryStatus DeliveryStatusId { get; set; }
        public string DeliveryNoteStatusText { get => DeliveryStatusId.GetText(); }

        // Sage
        public string SageReference { get; set; }
        public SageOrderStatus SageStatus { get; set; }
        public string SageStatusText { get => SageStatus.GetText(); }
        public SageInvoiceStatus InvoiceStatus { get; set; }
        public string InvoiceStatusText { get => InvoiceStatus.GetText(); }
        public SageDeliveryStatus DeliveryStatus { get; set; }
        public string DeliveryStatusText { get => DeliveryStatus.GetText(); }
        public SageCreditStatus CreditStatus { get; set; }
        public string CreditStatusText { get => CreditStatus.GetText(); }
        public string FactoryCode { get; set; }
        public string ProviderClientReference { get; set; }
        public string ProjectPrefix { get; set; }
        public DateTime? ValidationDate { get; set; }
        public string SageItemRef { get; set; }
        public bool AllowRepeatedOrders { get; set; }    



    }


    public class CSVCompanyOrderDTO
    {
        public int OrderID { get; set; }
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string OrderNumber { get; set; }
        public string MDOrderNumber { get; set; }
        public string OrderDate { get; set; }
        public string OrderDueDate { get; set; }
        public string UserName { get; set; }
        public DocumentSource Source { get; set; }
        public int? Quantity { get; set; }
        public ProductionType ProductionType { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string OrderStatusText { get; set; }
        public int? LocationID { get; set; }
        public string LocationName { get; set; }
        public int BrandID { get; set; }
        public string Brand { get; set; }
        public int SendToCompanyID { get; set; }        // workshop companyid		
        public string SendTo { get; set; }       // workshop name
        public string SendToCode { get; set; }       // workshop code
        public int? BillToCompanyID { get; set; }
        public string BillTo { get; set; }
        public string BillToCode { get; set; }
        public int ProjectID { get; set; }
        public string Project { get; set; }
//        public string Fabric { get; set; }
        public float ValidationProgress { get; set; }
        public bool IsStopped { get; set; }
        public bool IsBilled { get; set; }
        public bool IsInConflict { get; set; }
        public string CompanyCode { get; set; }
        public string BrandCode { get; set; }
        public string ProjectCode { get; set; }
        public bool IsCompleted { get; set; }
        public string CompletedDate { get; set; }
        public int OrderGroupID { get; set; }
        //public bool IsGroup { get; set; }
        public string ArticleCode { get; set; }
        public string ArticleName { get; set; }
        public int ArticleID { get; set; }
        public bool IsItem { get; set; }
        public int OrderDataID { get; set; }
        public bool RequireValidation { get; set; }
        public int? ParentOrderID { get; set; }
        public int AggregatedQuantity { get; set; }
        public string PackCode { get; set; }
        public int PrintJobId { get; set; }

        // Sage
        public string SageReference { get; set; }
        public SageOrderStatus SageStatus { get; set; }
        public string SageStatusText { get => SageStatus.GetText(); }
        public SageInvoiceStatus InvoiceStatus { get; set; }
        public string InvoiceStatusText { get => InvoiceStatus.GetText(); }
        public SageDeliveryStatus DeliveryStatus { get; set; }
        public string DeliveryStatusText { get => DeliveryStatus.GetText(); }
        public SageCreditStatus CreditStatus { get; set; }
        public string CreditStatusText { get => CreditStatus.GetText(); }
        public string FactoryCode { get; set; }
        public string ProviderClientReference { get; set; }
        public string ProjectPrefix { get; set; }
        public string ValidationDate { get; set; }
        public string OrderCategoryClient { get; set; }
        public string SageItemRef { get; set; }

        public CSVCompanyOrderDTO()
        {

        }

        public CSVCompanyOrderDTO(CompanyOrderDTO toClone)
        {
            OrderID = toClone.OrderID;
            CompanyID = toClone.CompanyID;
            CompanyName = toClone.CompanyName;
            OrderNumber = toClone.OrderNumber;
            MDOrderNumber = toClone.MDOrderNumber;
            OrderDate = toClone.OrderDate.ToCSVDateFormat();
            OrderDueDate = toClone.OrderDueDate.ToCSVDateFormat();
            UserName = toClone.UserName;
            Source = toClone.Source;
            Quantity = toClone.Quantity;
            ProductionType = toClone.ProductionType;
            OrderStatus = toClone.OrderStatus;
            OrderStatusText = toClone.OrderStatusText;
            LocationID = toClone.LocationID;
            LocationName = toClone.LocationName;
            BrandID = toClone.BrandID;
            Brand = toClone.Brand;
            SendToCompanyID = toClone.SendToCompanyID;        // workshop companyid		
            SendTo = toClone.SendTo;       // workshop name
            SendToCode = toClone.SendToCode;       // workshop code
            BillToCompanyID = toClone.BillToCompanyID;
            BillTo = toClone.BillTo;
            BillToCode = toClone.BillToCode;
            ProjectID = toClone.ProjectID;
            Project = toClone.Project;
            //Fabric = toClone.Fabric;
            ValidationProgress = toClone.ValidationProgress;
            IsStopped = toClone.IsStopped;
            IsBilled = toClone.IsBilled;
            IsInConflict = toClone.IsInConflict;
            CompanyCode = toClone.CompanyCode;
            BrandCode = toClone.BrandCode;
            ProjectCode = toClone.ProjectCode;
            IsCompleted = toClone.IsCompleted;
            CompletedDate = toClone.CompletedDate.ToCSVDateFormat();
            OrderGroupID = toClone.OrderGroupID;
            //IsGroup = toClone.IsGroup;
            ArticleCode = toClone.ArticleCode;
            ArticleName = toClone.ArticleName;
            ArticleID = toClone.ArticleID;
            IsItem = toClone.IsItem;
            OrderDataID = toClone.OrderDataID;
            RequireValidation = toClone.RequireValidation;
            ParentOrderID = toClone.ParentOrderID;
            AggregatedQuantity = toClone.AggregatedQuantity;
            PackCode = toClone.PackCode;
            //PrintJobId = toClone.PrintJobId;

            // Sage
            SageReference = toClone.SageReference;
            SageStatus = toClone.SageStatus;
            //SageStatusText = toClone.SageStatusText;
            InvoiceStatus = toClone.InvoiceStatus;
            //InvoiceStatusText = toClone.InvoiceStatusText;
            DeliveryStatus = toClone.DeliveryStatus;
            //DeliveryStatusText { get => DeliveryStatus.GetText(); }
            CreditStatus = toClone.CreditStatus;
            //string CreditStatusText { get => CreditStatus.GetText(); }
            FactoryCode = toClone.FactoryCode;
            ProviderClientReference = toClone.ProviderClientReference;
            ProjectPrefix = toClone.ProjectPrefix;
            ValidationDate = toClone.ValidationDate.ToCSVDateFormat();
            OrderCategoryClient = toClone.OrderCategoryClient;
            SageItemRef = toClone.SageItemRef;
    }

    }
}
