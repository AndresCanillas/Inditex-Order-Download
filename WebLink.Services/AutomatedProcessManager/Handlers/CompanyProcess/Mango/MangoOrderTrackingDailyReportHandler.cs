using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services
{
    public class MangoOrderTrackingDailyReportHandler : EQEventHandler<MangoOrderTrackingDailyReportEvent>
    {
        private readonly MangoOrderTrackingReportGeneratorService reportService;

        public MangoOrderTrackingDailyReportHandler(MangoOrderTrackingReportGeneratorService reportService)
        {
            this.reportService = reportService;
        }

        public override EQEventHandlerResult HandleEvent(MangoOrderTrackingDailyReportEvent e)
        {


            reportService.SendReport(e.CompanyID, e.StartDate, e.EndDate);
            return EQEventHandlerResult.OK;
        }
    }
}
