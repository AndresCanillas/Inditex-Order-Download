using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public enum FtpFileStatus
    {
        Error = 0,
        Processed,
        Ignored
    }

    public class FtpWatcherLog : IFtpWatcherLog
    {
        public int ID { get; set; }
        public int ProjectID { get; set; }
        public string FileName { get; set; }
        public string FileContainer { get; set; }
        public string Server { get; set; }
        public string User { get; set; }
        public FtpFileStatus Status { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public DateTime CreatedDate { get; set; }
       
    }
}
