using System;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{

    public class DataEntryRq
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int BrandID { get; set; }
        public string OrderNumber { get; set; }
        public int SeasonID { get; set; }
        public int SectionID { get; set; }
        public int SubSectionID { get; set; }
        public int SizeSetID { get; set; }
        public string ArticleQuality1 { get; set; }
        public string ArticleQuality2 { get; set; }
        
        public string Description { get; set; }
        public int MarketOriginID { get; set; }
        public DateTime? DeliveryDate { get; set; }
        
        public string Price1 { get; set; }
        
        
        public string Price2 { get; set; }
        public string Price3 { get; set; }
        public string Price4 { get; set; }
        public string Price5 { get; set; }
        public string Currency1 { get; set; }
        public string Currency2 { get; set; }
        public string Currency3 { get; set; }
        public string Currency4 { get; set; }
        public string Currency5 { get; set; }
        public string ProviderReference { get; set; }
        public int ProviderCompanyID { get; set; }
        public OrderStatus? Status { get; set; }
        public string RowJsonData { get; set; }
        public int AdditonalTagsPercentage { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string OrderJsonData { get; set; }
        public bool IsNew { get; set; }

        public IList<OrderArticle> Articles { get; set; }

        public string ManualEntryService {  get; set; }  

        public string CampaignID { get; set; } 

        public bool RequiresChineseColor { get; set; }
        public DateTime? ExpectedProductionDate { get; set; }
        public List<Sizes> Sizes { get; set; }
        public string CompanyName { get; set; } 
        public string EAN { get; set; }
        public string UN { get; set; }
        public string ArticleCode { get;  set; }
        public string FreeText1 { get; set; }
        public string FreeText2 { get; set; }
        public string CountryDestination { get; set; }
    }

    public class Sizes
    {
        public string Size { get; set; }
        public string Color { get; set; }
        public int Quantity { get; set; } 

        public string Description { get; set; } 
        public string EAN { get; set; } 
        public string UN { get; set; }   
        public string Price1 { get; set; }
        public string Price2 { get; set; }
        public string Currency1 { get; set; }
        public string Currency2 { get; set; }
    }


        public class OrderArticle
    {
        public int ID { get; set; }
        public int ArticleID { get; set; }
        public int OrderID { get; set; }
        public string ArticleCode { get; set; }
        public string PackCode { get; set; }
        public int? LabelID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        internal bool IsNew;
    }

}



