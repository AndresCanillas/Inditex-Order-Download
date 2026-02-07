using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{

    public class OrderGroupSelectionDTO
    {
        public string OrderNumber { get; set; }
        public int OrderGroupID { get; set; }
        public int[] Orders { get; set; }
        public List<OrderDetailDTO> Details { get; set; }
        public int? ShippingAddressID { get; set; }
        public int WizardStepPosition { get; set; }
        public int SendToCompanyID { get; set; }
        public DateTime DueDate { get; set; }
        public int ProjectID { get; set; }


        public OrderGroupSelectionDTO()
        {
            OrderNumber = string.Empty;
            OrderGroupID = -1;
            Orders = (new List<int>()).ToArray();
            Details = new List<OrderDetailDTO>();
            ShippingAddressID = null;
            ProjectID = 0;
        }

        public OrderGroupSelectionDTO(OrderGroupSelectionDTO toClone)
        {
            OrderNumber = toClone.OrderNumber;
            OrderGroupID = toClone.OrderGroupID;
            Orders = (new List<int>(toClone.Orders)).ToArray();
            Details = new List<OrderDetailDTO>(toClone.Details);
            ShippingAddressID = toClone.ShippingAddressID;
            ProjectID = toClone.ProjectID;

        }
    }

    public class OrderGroupSelectionCompositionDTO : OrderGroupSelectionDTO
    {
        public List<CompositionDefinition> Compositions { get; set; }

        public OrderGroupSelectionCompositionDTO() : base()
        {
            Compositions = new List<CompositionDefinition>();
        }
        public string GenerateCompoText { get; set; }
        public OrderGroupSelectionCompositionDTO(OrderGroupSelectionCompositionDTO toClone)
        {
            OrderNumber = toClone.OrderNumber;
            OrderGroupID = toClone.OrderGroupID;
            Orders = (new List<int>(toClone.Orders)).ToArray();
            Details = new List<OrderDetailDTO>(toClone.Details);
            ShippingAddressID = toClone.ShippingAddressID;
            ProjectID = toClone.ProjectID;
            Compositions = new List<CompositionDefinition>(toClone.Compositions);
        }
    }


    public class SizesSetRange
    {
        public string Code { get; set; }
        public Dictionary<string, string> Equivalences { get; set; }
    }

    public class CustomValuesDTO
    {
        public string[] SelectedSizes { get; set; }
        public List<SizesSetRange> SizesSetRanges { get; set; }
    }

    public class CustomDetailSelectionWithSizeSetDTO : CustomDetailSelectionDTO
    {
        //public string[] SelectedSizes { get; set; }
        //public List<SizesSetRange> SizesSetRanges { get; set; }

        public List<CustomValuesDTO> CustomValues { get; set; }
    }

    /// <summary>
    /// Received selection from UI and Product fields list from Variable data
    /// </summary>
    public class CustomDetailSelectionDTO
    {
        public List<OrderGroupSelectionItemAssigmentDTO> Selection { get; set; }
        public List<string> ProductFields { get; set; } // only list of names to include in queries
        public bool GetAllArticles { get; set; } = false;
    }

    public class OrderGroupSelectionItemAssigmentDTO : OrderGroupSelectionDTO
    {
        public OrderGroupSelectionItemAssigmentDTO(OrderGroupSelectionItemAssigmentDTO toClone)
        {
            OrderNumber = toClone.OrderNumber;
            OrderGroupID = toClone.OrderGroupID;
            Orders = (new List<int>(toClone.Orders)).ToArray();
            Details = new List<OrderDetailDTO>(toClone.Details);
            ShippingAddressID = toClone.ShippingAddressID;
            ProjectID = toClone.ProjectID;
            SelectedArticles = new List<CustomArticle>(toClone.SelectedArticles);
            ProductFields = new List<ProductField>(toClone.ProductFields); // new values for every product field 
            CustomDetails = new List<CustomDetails>(toClone.CustomDetails);
        }

        public OrderGroupSelectionItemAssigmentDTO() : base()
        {
            SelectedArticles = new List<CustomArticle>();
            ProductFields = new List<ProductField>();
            CustomDetails = new List<CustomDetails>();
        }

        public List<CustomArticle> SelectedArticles { get; set; }
        public List<ProductField> ProductFields { get; set; }
        public List<CustomDetails> CustomDetails { get; set; }


    }



    public class CustomArticle
    {
        public int ArticleID { get; set; }
        [Obsolete("move to the OrderGroupSelectionItemAssigmentDTO.ProductFields, all articles inner the same ordergroup contain the same product field values")]
        public List<ProductField> ProductFields { get; set; }

        public string PackCode { get; set; }
    }

    public class ProductField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class CustomDetails
    {
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
