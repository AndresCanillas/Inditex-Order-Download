using Service.Contracts;
using Services.Core;
using System;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services.Automated
{
    public class MangoOrderTrackingDailyReport : IAutomatedProcess
    {
        private readonly IAppConfig config;
        private readonly ILogService log;
        private readonly IEventQueue events;

        public MangoOrderTrackingDailyReport(IAppConfig config, ILogService log, IEventQueue events)
        {
            this.config = config;
            this.log = log;
            this.events = events;
        }

        public TimeSpan GetIdleTime()
        {
            var scheduledAt = config.GetValue<string>("CustomSettings.Mango.DailyReport.ExecutionDailyTime", "15:00:00");
            var now = DateTime.Now;
#if DEBUG
            scheduledAt = now.AddMinutes(1).ToString("HH:mm:ss");
#endif


            var scheduledAtSplit = scheduledAt.Split(':');

            var nextExecutionTime = new DateTime(now.Year, now.Month, now.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));

            if(nextExecutionTime < now)
            {
                var nextDay = now.AddDays(1).Date;
                nextExecutionTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));
            }

            log.LogMessage($"Mango OrderTracking Report time config hour: {scheduledAt}  next: {nextExecutionTime}");


            return nextExecutionTime - now;
        }

        public void OnLoad()
        {
        }

        public void OnExecute()
        {
            var Enabled = config.GetValue<bool>("CustomSettings.Mango.DailyReport.Enabled", false);
            var companyID = config.GetValue("CustomSettings.Mango.DailyReport.CompanyID", 44); // 44 is mango CompanyID in Production

             var daysBefore = config.GetValue("CustomSettings.Mango.DailyReport.DaysBefore", 270 ); // 270 days before (9 months aprox)

            if(!Enabled) {

                log.LogWarning("Mango OrderTracking Report is DISABLED");

                return;
            }

            var now = DateTime.Now;
            
            var to = now.Date; // <
            var from = to.AddDays(-daysBefore).Date; //

            // {"StartDate":"2025-03-31T00:00:00-06:00","EndDate":"2025-04-01T00:00:00-06:00","OrderGroupID":0,"OrderID":0,"OrderNumber":"","BrandID":0,"ProjectID":0,"Source":0,"EventName":"MangoOrderTrackingDailyReportEvent","CompanyID":44,"RetryCount":0}
            events.Send(new MangoOrderTrackingDailyReportEvent(0, 0, string.Empty, companyID, 0, 0, from, to));


        }

        public void OnUnload()
        {
        }
    }
}
