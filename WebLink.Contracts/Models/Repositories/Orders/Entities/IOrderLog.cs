using Service.Contracts.Database;
using System;

namespace WebLink.Contracts.Models
{
    public enum OrderLogLevel
    {

        INFO,  // sysadmin, IsIDTRole, IsCompanyRole - lowes
        WARN,  // sysadmin, IsIDTRole, IsCompanyRole - low
        ERROR, // sysadmin, IsIDTRole - high
        DEBUG  // only sysadmin 
    }


    public interface IOrderLog : IEntity, IBasicTracing
    {
        int OrderID { get; set; }

        OrderLogLevel Level { get; set; }

        string Message { get; set; }

        string Comments { get; set; }
    }
}