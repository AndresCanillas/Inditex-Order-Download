using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IOrderLogService
    {
        void Log(int orderID, string message, OrderLogLevel level, string comments = null);

        void Info(int orderID, string message, string comments = null);

        void Warn(int orderID, string message, string comments = null);

        void Error(int orderID, string message, string comments = null);

        void Debug(int orderID, string message, string comments = null);

        // asyn interface

        Task LogAsync(int orderID, string message, OrderLogLevel level, string comments = null);

        Task InfoAsync(int orderID, string message, string comments = null);

        Task WarnAsync(int orderID, string message, string comments = null);

        Task ErrorAsync(int orderID, string message, string comments = null);

        Task DebugAsync(int orderID, string message, string comments = null);


    }
}
