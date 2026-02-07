using System;

namespace WebLink.Contracts.Models
{
    public enum SystemOrderAction
    {
        Cancelled = 0, 
        Stoped = 1, 
    }

    public class SystemChangedOrdersLog 
    {
        public int ID { get; set; }  
        public string OrderNumber { get; set; }  
        public string BatchNumber { get; set; }  
        public string ArticleName { get; set; }  

        public SystemOrderAction ActionID { get; set; }  
        public DateTime CreatedDate { get; set; }
        public int ProjectID { get; set; }   

    }
}
