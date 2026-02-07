using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public class OrderGroupQuantitiesDTO
    {
        public string OrderNumber { get; set; }
        public int OrderGroupID { get; set; }
        public int WizardStepPosition { get; set; }
        public int ProjectID { get; set; }
        public List<QuantityState> Quantities { get; set; } = new List<QuantityState>();
        public string MadeIn { get; set; }
    }

    public class OrderGroupExtraItemsDTO
    {
        public string OrderNumber { get; set; }
        public int OrderGroupID { get; set; }
        public int WizardStepPosition { get; set; }
        public int ProjectID { get; set; }
        public List<ExtraQuantityState> Items { get; set; }
        public OrderGroupExtraItemsDTO()
        {
            Items = new List<ExtraQuantityState>();
        }
    }

    public class ExtraQuantityState
    {
        public int ArticleID { get; set; }
        public string ArticleCode { get; set; }
        public int? PrinterJobID { get; set; }
        public int? PrinterJobDetailID { get; set; }
        public int? OrderID { get; set; }
        public int Value { get; set; }
    }

    public class OrderLabelRequest
    {
        public int ArticleID { get; set; }
    }


    // Scalpers Quantity Wizard
    public class ScalpersOrderGroupQuantitiesDTO : OrderGroupQuantitiesDTO
    {
        // TODO: variable data fields maybe storage inner object property, 
        // every company can pass his owned properties - require to review
        public string CodeSection { get; set; }
        public string CodeGama { get; set; }
        public string MadeInCode { get; set; }
        public string Logistic { get; set; } // logistic article code
        public string Adhesive { get; set; } // Adhesive article code
        public string Cardboard { get; set; } // Cardboard article code
        public string Composition { get; set; } // Composition article code
        public int Importer { get; set; }

    }


   
}
