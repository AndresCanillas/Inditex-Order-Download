using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.PrintCentral
{
    public class OrderAttachDocumentRequest
    {
        public int ProjectID { get; set; }
        public string OrderNumber { get; set; }
        public int CompanyID { get; set; }
    }
}
