using System;

namespace WebLink.Contracts.Services.Mango
{
    public interface IMangoOrderTrackingDailyReportService
    {
        void SendReport(int companyID, DateTime from, DateTime to);
    }
}
