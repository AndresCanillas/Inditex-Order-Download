using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public class OrderPoolDTO
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
        public OrderStatus Status { get; set; }
        public string RowJsonData { get; set; }
        public int AdditonalTagsPercentage { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string OrderJsonData { get; set; }
        internal bool IsNew;
        public List<OrderPoolSizeInfoDTO> Sizes { get; set; } = new List<OrderPoolSizeInfoDTO>();
        public string SizeSetName { get; set; }
        public string SectionName { get; set; }
        public string SizeRange { get; set; }

        public string SizeCategory { get; set; }
        public string MarketOriginName { get; set; }

        public string CampaignName { get; set; }
        public string ArticleCode { get; set; }
        public string UN { get; set; }
        public string EAN { get; set; }
        public DateTime? ExpectedProductionDate { get; set; }

        public string ProcessedBy { get; set; }
        public DateTime? ProcessedDate { get; set; } 

        public string ProviderLocationName { get; set; }     
    }
}
