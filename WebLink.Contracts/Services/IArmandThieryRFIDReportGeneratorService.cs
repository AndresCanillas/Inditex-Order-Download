using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    public interface IArmandThieryRFIDReportGeneratorService
    {
        void SendReport(int companyID);
    }
}
