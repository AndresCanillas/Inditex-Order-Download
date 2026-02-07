using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public interface IBandFRFIDReportGeneratorService
    {
        void SendReport(int companyID, DateTime from, DateTime to);
    }
}
