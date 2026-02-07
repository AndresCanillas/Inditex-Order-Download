using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    public interface IBrownieReportGeneratorService
    {
        void SendReport(int companyID, DateTime from, DateTime to);
    }
}
