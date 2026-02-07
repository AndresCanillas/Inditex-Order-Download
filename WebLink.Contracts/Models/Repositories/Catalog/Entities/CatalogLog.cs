using System;

namespace WebLink.Contracts.Models
{
    public class CatalogLog
    {
        public int ID { get; set; } 
        public int CatalogID { get; set; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public string OldData { get; set; } 
        public string NewData { get; set; }
        public string User { get; set; }
        public DateTime Date { get; set; } 
        public int RecordID { get; set; }    

    }
}

