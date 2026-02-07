using Newtonsoft.Json.Linq;
using Remotion.Linq.Clauses;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Emit;
using WebLink.Contracts.Migrations;

namespace WebLink.Contracts.Models
{
	// TODO: este objeto y CompanyOrderDTO son casi iguales, la diferencia es que uno es para etiquetas y otro para articulos de stock
	// seria mas facil unirlos
	public class OrderDetailDTO {

		public int ArticleID { get; set; }
		public string Article { get; set; }
		public string Description { get; set; }
		public string ArticleCode { get; set; }
        
		public int Quantity { get; set; }
		public int QuantityRequested { get; set; }
		public string ArticleBillingCode { get; set; }
		public bool IsItem { get; set; }
		public string UpdatedDate { get; set; }
		public int? LabelID { get; set; }
		public string Label { get; set; }
        public LabelType LabelType { get; set; }
        public string LabelTypeStr { get; set; }
		public string Size { get; set; }
		public string UnitDetails { get; set; }
		public int ProductDataID { get; set; }
		public int PrinterJobDetailID { get; set; }
		public int PrinterJobID { get; set; }
		public string PackCode { get; set; }
		public int PackConfigQuantity { get; set; }
		public bool? RequiresDataEntry { get; set; }
		public int OrderID { get; set; }
		public OrderStatus OrderStatus { get; set; }
		public bool IsBilled { get; set; }
		public int OrderGroupID { get; set; }
		public int QuantityDelta { get; set; } // TODO: review this field, can by calculated value
		public int MaxAllowed { get; set; }
		public int MinAllowed { get; set; }
		public bool HasPackCode { get; set; }
		public int ProjectID { get; set; }
        public string OrderNumber { get; set; }
		public int? SendToAddressID { get; set; }
		public bool SyncWithSage { get; set; }
		public string SageReference { get; set; }
		public string Color { get; set; }
        public int? MaxQuantityPercentage { get; set; }
        public int? MaxQuantity { get; set; }
        public int AllowQuantityEdition { get; set; }
        public string DisplayField { get; set; }
        public string GroupingField { get; set; }
        public JObject ProductData { get; set; }
        public bool IncludeComposition { get; set; }
        public int OrderDataID { get; set; }
        public int? LocationID { get; set; }
        public bool HasRFID { get; internal set; }
        public List<string> LabelGroupingFields { get; set; }

        public bool IsDetailedArticle { get; set; } = false;
        public string CategoryName { get; set; }
        public DocumentSource Source { get; set; }

        public OrderDetailDTO() { }

    }


	public class OrderDetailWithCompoDTO : OrderDetailDTO
    {
		public CompositionDefinition Composition { get; set; }
		public string KeyName { get; set; } // TODO: in the future could be a list of keys
		public string KeyValue { get; set; }// TODO: in the future could be a list of values
        public string SendTo { get; set; }
        public string ClientReference { get; set; }
        public string Location { get; set; }
        public string CreatedDate { get; set; }
        public string OrderDueDate { get; set; }
        public string ValidatedDate { get; set; }

        public OrderDetailWithCompoDTO() { }

        public OrderDetailWithCompoDTO(OrderDetailWithCompoDTO toClone)
        {
            ArticleID = toClone.ArticleID;
            Article = toClone.Article;
            Description = toClone.Description;
            ArticleCode = toClone.ArticleCode;
            Quantity = toClone.Quantity;
            QuantityRequested = toClone.QuantityRequested;
            ArticleBillingCode = toClone.ArticleBillingCode;
            IsItem = toClone.IsItem;
            UpdatedDate = toClone.UpdatedDate;
            LabelID = toClone.LabelID;
            Label = toClone.Label;
            LabelType = toClone.LabelType;
            LabelTypeStr = toClone.LabelTypeStr;
            Size = toClone.Size;
            UnitDetails = toClone.UnitDetails;
            ProductDataID = toClone.ProductDataID;
            PrinterJobDetailID = toClone.PrinterJobDetailID;
            PrinterJobID = toClone.PrinterJobID;
            PackCode = toClone.PackCode;
            PackConfigQuantity = toClone.PackConfigQuantity;
            RequiresDataEntry = toClone.RequiresDataEntry;
            OrderID = toClone.OrderID;
            OrderStatus = toClone.OrderStatus;
            IsBilled = toClone.IsBilled;
            OrderGroupID = toClone.OrderGroupID;
            QuantityDelta = toClone.QuantityDelta; // TODO: review this field, can by calculated value
            MaxAllowed = toClone.MaxAllowed;
            MinAllowed = toClone.MinAllowed;
            HasPackCode = toClone.HasPackCode;
            ProjectID = toClone.ProjectID;
            OrderNumber = toClone.OrderNumber;
            SendToAddressID = toClone.SendToAddressID;
            SyncWithSage = toClone.SyncWithSage;
            SageReference = toClone.SageReference;
            Color = toClone.Color;
            MaxQuantityPercentage = toClone.MaxQuantityPercentage;
            MaxQuantity = toClone.MaxQuantity;
            AllowQuantityEdition = toClone.AllowQuantityEdition;
            DisplayField = toClone.DisplayField;
            GroupingField = toClone.GroupingField;
            ProductData = toClone.ProductData;
            Composition = toClone.Composition;
            KeyName = toClone.KeyName;
            KeyValue = toClone.KeyValue;
            SendTo = toClone.SendTo;
            ClientReference = toClone.ClientReference;
            Location = toClone.Location;
            CreatedDate = toClone.CreatedDate;
            OrderDueDate = toClone.OrderDueDate;
            ValidatedDate = toClone.ValidatedDate;
        }

        public OrderDetailWithCompoDTO(OrderDetailDTO toClone)
        {
            ArticleID = toClone.ArticleID;
            Article = toClone.Article;
            Description = toClone.Description;
            ArticleCode = toClone.ArticleCode;
            Quantity = toClone.Quantity;
            QuantityRequested = toClone.QuantityRequested;
            ArticleBillingCode = toClone.ArticleBillingCode;
            IsItem = toClone.IsItem;
            UpdatedDate = toClone.UpdatedDate;
            LabelID = toClone.LabelID;
            Label = toClone.Label;
            LabelType = toClone.LabelType;
            LabelTypeStr = toClone.LabelTypeStr;
            Size = toClone.Size;
            UnitDetails = toClone.UnitDetails;
            ProductDataID = toClone.ProductDataID;
            PrinterJobDetailID = toClone.PrinterJobDetailID;
            PrinterJobID = toClone.PrinterJobID;
            PackCode = toClone.PackCode;
            PackConfigQuantity = toClone.PackConfigQuantity;
            RequiresDataEntry = toClone.RequiresDataEntry;
            OrderID = toClone.OrderID;
            OrderStatus = toClone.OrderStatus;
            IsBilled = toClone.IsBilled;
            OrderGroupID = toClone.OrderGroupID;
            QuantityDelta = toClone.QuantityDelta; // TODO: review this field, can by calculated value
            MaxAllowed = toClone.MaxAllowed;
            MinAllowed = toClone.MinAllowed;
            HasPackCode = toClone.HasPackCode;
            ProjectID = toClone.ProjectID;
            OrderNumber = toClone.OrderNumber;
            SendToAddressID = toClone.SendToAddressID;
            SyncWithSage = toClone.SyncWithSage;
            SageReference = toClone.SageReference;
            Color = toClone.Color;
            MaxQuantityPercentage = toClone.MaxQuantityPercentage;
            MaxQuantity = toClone.MaxQuantity;
            AllowQuantityEdition = toClone.AllowQuantityEdition;
            DisplayField = toClone.DisplayField;
            GroupingField = toClone.GroupingField;
            ProductData = toClone.ProductData;
        }
    }
}
