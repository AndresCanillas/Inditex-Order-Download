using Service.Contracts.Documents;
using Service.Contracts.WF;
using System;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Workflows
{
    public class IntakeWorkflowInput
    {
        public int CompanyID;                   // ID of the company uploading the order
        public int BrandID;                     // ID of the brand
        public int ProjectID;                   // ID of the project being used to process the order
        public string UserName;                 // The user that is performing the upload operation
        public DocumentSource Source;           // Determines the source of the order file
        public ProductionType ProductionType;   // Determines if the order will be produced at a customer location, or in an Indet factory
        public int? CustomerLocationID;         // Id of the customer location (used only if ProductionType is CustomerLocation (2))
        public int? CustomerPrinterID;          // Id of the printer where the order will be printed (used only if ProductionType is CustomerLocation (2))
        public int? ProductionLocationID;       // Id of the Indet/Smartdots Location (factory) where the order will be produced (used only if ProductionType is IDTLocation (1))
        public bool IsStopped;                  // Flag indicating if the order should be stopped
        public bool IsBillable;                 // Flag indicating if the order should be billed
        public bool IsTestOrder;                // Flag indicating if the order should be treated as a test order (it should not be billed, no notifications should be sent to client contacts, should be clearly displayed as test)
        public string ClientCategory;           // Information field used for querying (allows client to search for groups of related orders)
        public string ERPReference;             // ERP reference number, used in case the order has already been processed by another system/tool and the order is already registered in the ERP system
        public string FileName;                 // Name of the received file (only the name of the file, not its path)
        public Guid InputFile;                  // File Guid of the file to be processed by the workflow (as received by the web server). NOTE: Caller must add the file to a file store before inserting the item into the workflow.
        public int WorkflowFileID;
    }

    public class OrderFileItem : WorkItem
    {
        public OrderFileItem() { }

        public OrderFileItem(IntakeWorkflowInput input)
        {
            CompanyID = input.CompanyID;
            ProjectID = input.ProjectID;
            UserName = input.UserName;
            ProductionType = input.ProductionType;
            Source = input.Source;
            CustomerLocationID = input.CustomerLocationID;
            CustomerPrinterID = input.CustomerPrinterID;
            ProductionLocationID = input.ProductionLocationID;
            IsStopped = input.IsStopped;
            IsBillable = input.IsBillable;
            IsTestOrder = input.IsTestOrder;
            ClientCategory = input.ClientCategory;
            ERPReference = input.ERPReference;
            FileName = input.FileName;
            InputFile = input.InputFile;
            WorkflowFileID = input.WorkflowFileID;

            if(Source == DocumentSource.Web)
            {
                Priority = ItemPriority.High;
            }
            else
            {
                Priority = ItemPriority.Normal;
            }
        }

        // INPUTS: The following fields must be initialized prior to inserting an item into the workflow

        public int CompanyID;
        public string CompanyName;
        public string CompanyCode;
        public int BrandID;
        public string BrandName;
        public int ProjectID;
        public string ProjectName;
        public string UserName;
        public ProductionType ProductionType;
        public DocumentSource Source;
        public int? CustomerLocationID;
        public int? CustomerPrinterID;
        public int? ProductionLocationID;
        public bool IsStopped;
        public bool IsBillable;
        public bool IsTestOrder;
        public string ClientCategory;
        public string ERPReference;
        public string FileName;
        public Guid InputFile;
        public int WorkflowFileID;
        public int CreateInDBProgress;  

        // STATE VALUES: The following fields are initialized and used within the workflow itself,
        //				 it is not necesary to initialize them to insert the item.

        public MappingSource MappingSource = MappingSource.Database;
        public DocumentImportConfiguration ImportConfiguration;
        public Guid VariableDataFile;
        public Guid GroupsDataFile;
        public Guid OrdersDataFile;

        public List<OrderHeader> CreatedOrders = new List<OrderHeader>();
        public int GenericFailureCount;
        public string MissingArticleCode;
        public string MissingSupplierCode;
        public string MissingCatalog;
        public bool OrderProcessedSuccessfully;
        public bool TransformationFromJsonCompleted;
		public string PrimaryCustomer;
		public string SecondaryCustomer;
	}


	public class OrderHeader
    {
        public int OrderGroupID;
        public int OrderID;
        public string OrderNumber;
        public string SupplierCode;
        public string ArticleCode;
        public int Quantity;
    }

    public enum MappingSource
    {
        Database = 1,
        Dynamic = 2
    }


    public class OrderGroupInfo
    {
        public string OrderNumber;
        public string SendTo;
        public int SendToCompanyID;
        public int? ProviderRecordID;
        public string BillTo;
        public int BillToCompanyID;
        public string ArticleCode;
        public int Quantity;
        public ImportedData Data;

        public OrderGroupInfo() { }

        public OrderGroupInfo(int projectid, ImportedData data)
        {
            Data = data;
            OrderNumber = (string)data.GetValue("OrderNumber");
            SendTo = (string)data.GetValue("SendTo");
            BillTo = (string)data.GetValue("BillTo");
            ArticleCode = (string)data.GetValue("Details.ArticleCode");
            Quantity = data.Sum("Details.Quantity");
        }
    }


    public class OrderInfo
    {
        public string OrderNumber;
        public string SendTo;
        public int SendToCompanyID;
        public int? ProviderRecordID;
        public string BillTo;
        public int BillToCompanyID;
        public int? ProductionLocationID;
        public string ArticleCode;
        public int? ArticleID;
        public string PackCode;   // used when the order was created because of a pack
        public int Quantity;
        public bool IsItem;
        public int? VariableDataID;  // Points to the corresponding OrderDetails table in Variable Data DB
        public int? SLADays;
        public DateTime? DueDate;
        public int? ArticleLabelID;
        public string ArticleName;
        public bool ArticleEncodeRIFD;
        public string ArticleLabelType;
        public ImportedData Data;

        public List<OrderDetailInfo> Details;

        public int? OrderGroupID;
        public int? OrderID;

        public OrderInfo() { }

        public OrderInfo(OrderGroupInfo group, ImportedData data)
        {
            Data = data;
            OrderNumber = group.OrderNumber;
            SendTo = group.SendTo;
            SendToCompanyID = group.SendToCompanyID;
            ProviderRecordID = group.ProviderRecordID;
            BillTo = group.BillTo;
            BillToCompanyID = group.BillToCompanyID;
            ArticleCode = (string)data.GetValue("Details.ArticleCode");
            Quantity = data.Sum("Details.Quantity");
        }
    }


    public class OrderDetailInfo
    {
        public int Quantity;
    }
}
