using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class PackArticleConfigDTO
    {
        public int PackID { get; set; }

        public string PackCode { get; set; }

        public int? ArticleID { get; set; }

        public string ArticleCode { get; set; }

        public int Quantity { get; set; }

    }
}
