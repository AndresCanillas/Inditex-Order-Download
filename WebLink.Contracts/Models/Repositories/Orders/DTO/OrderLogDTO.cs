using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{ 
    public class OrderLogDTO
    {

        public int LogID { get; set; }

        public int OrderID { get; set; }

        public string OrderNumber { get; set; }

        public OrderLogLevel Level { get; set; }

        public string LevelText { get; set; }

        public string Message { get; set; }

        public string Comments { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        
        public string ArticleCode { get; set; }
    }
}
