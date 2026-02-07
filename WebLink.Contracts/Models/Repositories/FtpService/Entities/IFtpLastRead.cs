using Service.Contracts.Database;
using System;

namespace WebLink.Contracts.Models
{
    public interface IFtpLastRead : IEntity
    {
        int ProjectID { get; set; }
        DateTime LastRead { get; set; }
        string Server { get; set; }
        string UserName { get; set; }
    }
}