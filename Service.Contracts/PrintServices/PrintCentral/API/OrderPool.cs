using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.PrintServices.PrintCentral.API
{
    public class OrderPool
    {
        public int ProjectID { get; set; }
        public string OrderNumber { get; set; }
        public string Seasson { get; set; }
        public int Year { get; set; }
        public string ProviderCode1 { get; set; }
        public string ProviderName1 { get; set; }
        public string ProviderCode2 { get; set; }
        public string ProviderName2 { get; set; }
        public string Size { get; set; }
        public string ArticleCode { get; set; }
        public string CategoryCode1 { get; set; }
        public string CategoryCode2 { get; set; }
        public string CategoryCode3 { get; set; }
        public string CategoryCode4 { get; set; }
        public string CategoryCode5 { get; set; }
        public string CategoryCode6 { get; set; }
        public string CategoryText1 { get; set; }
        public string CategoryText2 { get; set; }
        public string CategoryText3 { get; set; }
        public string CategoryText4 { get; set; }
        public string CategoryText5 { get; set; }
        public string CategoryText6 { get; set; }
        public string ColorCode { get; set; }
        public string ColorName { get; set; }
        public DateTime CreationDate { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public DateTime? ExpectedProductionDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string Price1 { get; set; }
        public string Price2 { get; set; }
        public string ProcessedBy { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public int Quantity { get; set; }
        public string ExtraData { get; set; }
    }
}
