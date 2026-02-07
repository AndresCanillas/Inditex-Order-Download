using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class OrderPrinterJobDetailDTO
    {
        public string ArticleCode { get;  set; }
        public int ID { get;  set; }
        public int OrderID { get;  set; }
        public int ProductDataID { get;  set; }
        public string PackCode { get;  set; }
        public int Quantity { get;  set; }
    }
}
