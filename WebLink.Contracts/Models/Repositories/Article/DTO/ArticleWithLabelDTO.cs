using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class ArticleWithLabelDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string ArticleCode { get; set; }
        public bool EncodeRFID { get; set; }
        public bool IncludeComposition { get; set; }
    }
}
