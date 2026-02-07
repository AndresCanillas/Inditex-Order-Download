using Service.Contracts.Database;
using System;

namespace WebLink.Contracts.Models
{
    public interface IFtpWatcherLog : IEntity
    {
        string FileName { get; set; }
        string Server { get; set; }
        string User { get; set; }
        string Message { get; set; }
        FtpFileStatus Status { get; set; }
        string StackTrace { get; set; }
        int ProjectID { get; set; }
        DateTime CreatedDate { get; set; }
    }
}