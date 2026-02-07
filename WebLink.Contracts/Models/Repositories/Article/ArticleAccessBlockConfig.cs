using System;

namespace WebLink.Contracts.Models
{
    public class ArticleAccessBlockConfig
    {
        public int ArticleID { get; set; }
        public int? ProjectID { get; set; }
        public string ExportBlockedLocationIds { get; set; }
    }
}

