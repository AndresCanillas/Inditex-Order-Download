using Service.Contracts;
using Services.Core;
using System;
using WebLink.Contracts;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services.Automated
{
    public class MonoprixReverseEANsProcess : IAutomatedProcess
    {
        private readonly IAppConfig config;
        private readonly ILogSection log;
        private readonly IEventQueue events;
        private readonly IMonoprixRFIDReportGeneratorService reportService;

        public MonoprixReverseEANsProcess (IAppConfig config, ILogService log, IEventQueue events, IMonoprixRFIDReportGeneratorService reportService)
        {
            this.config = config;
            this.log = log.GetSection("ReverseFlow");
            this.events = events;
            this.reportService = reportService;
        }

        // run every day one time at 01:00:00 am
        public TimeSpan GetIdleTime()
        {

            var scheduledAt = config.GetValue<string>("CustomSettings.Monoprix.ExecutionDailyTime", "01:00:00");
            var now = DateTime.Now;
#if DEBUG
            scheduledAt = now.AddMinutes(1).ToString("HH:mm:ss");
#endif


            var scheduledAtSplit = scheduledAt.Split(':');

            var nextExecutionTime = new DateTime(now.Year, now.Month, now.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));

            if (nextExecutionTime < now)
            {
                var nextDay = now.AddDays(1).Date;
                nextExecutionTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));
            }

            log.LogMessage($"Monoprix time config hour: {scheduledAt}  next: {nextExecutionTime}");


            return nextExecutionTime - now;

        }

        public void OnLoad() { }

        public void OnExecute()
        {
            var Enabled = config.GetValue<bool>("CustomSettings.Monoprix.Enabled", false);
            var companyID = config.GetValue("CustomSettings.Monoprix.CompanyID", 0);
            
            var baseDate = DateTime.Now;
            //var baseDate = new DateTime(2023, 6, 21, 0, 0, 0);
            var from = baseDate.AddDays(-1).Date;
            var to = from.AddDays(1).AddTicks(-1);
            var now = DateTime.Now;

            if (!Enabled) return;

            events.Send(new MonoprixDailyEANReportEvent(0, 0, string.Empty, companyID, 0, 0, from, to));

            var sendHistory = config.GetValue("CustomSettings.Monoprix.SendHistory", false);

            if (!sendHistory) return;
            
            var startHistoryDate = config.GetValue("CustomSettings.Monoprix.HistoryStartDate", now.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
            log.LogMessage($"Monoprix send  history from {DateTime.Parse(startHistoryDate)} to {now.Date.AddTicks(-1)}");
            reportService.SendHistory(companyID, DateTime.Parse(startHistoryDate), now.Date.AddTicks(-1));


        }

        public void OnUnload(){}
    }
}
