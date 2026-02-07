using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class ArticleInfoDTO
    {
        public int ArticleID { get; set; }
        public string ArticleName { get; set; }
        public string ArticleCode { get; set; }
        public string BillingCode { get; set; }
        public string CategoryName { get; set; }
        public int? CategoryID { get; set; }
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public int? LabelId { get; set; }   
    }
}
