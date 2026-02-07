using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    public class OrderPoolGrouping
    {
        public int ProjectID { get; set; }
        public int CompanyID { get; set; } 
        public int BrandID {  get; set; }
        public List<string> Orders { get; set; }
        public string Pattern { get; set; }
        public string ManualEntryGroupingService { get; set; }   
    }
}
