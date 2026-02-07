using System;
using System.ComponentModel.DataAnnotations;

namespace WebLink.Contracts.Models
{
    public class InlayConfig : IInlayConfig
    {     
        public int ID { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        [MaxLength(12)]
        public string Description { get; set; }
        public int? CompanyID { get; set; }
        public int? ProjectID { get; set; }
        public int? BrandID { get; set; }
        public bool IsAuthorized { get; set; }
        public int InlayID { get; set; }
    }
}
