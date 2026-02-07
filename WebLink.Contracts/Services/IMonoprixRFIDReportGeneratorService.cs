using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public interface IMonoprixRFIDReportGeneratorService 
    {
        void SendHistory(int companyID, DateTime from, DateTime to);
        void SendReport(int companyID, DateTime from, DateTime to);
    }
}
