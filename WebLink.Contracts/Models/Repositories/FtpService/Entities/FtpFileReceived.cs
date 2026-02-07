using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{

    public interface IFTPFileReceived : IEntity, IBasicTracing
    {
        string FileName { get; set; }
        int ProjectID { get; set; }
        int FactoryID { get; set; }
        string UploadOrderDTO { get; set; } // JSON string
        bool IsProcessed { get; set; }
    }

    public class FtpFileReceived : IFTPFileReceived
    {
        public int ID { get; set; }

        public string FileName { get; set; }
        public int ProjectID { get; set; }
        public int FactoryID { get; set; }
        public bool IsProcessed { get; set; }
        public string UploadOrderDTO { get; set; } // JSON string

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }

        

    }

    
}
