using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services.Automated
{
    public class BandFReverseEANsProcess : IAutomatedProcess
    {
        private readonly IAppConfig config;
        private readonly ILogSection log;
        private readonly IEventQueue events;
        private readonly IBandFRFIDReportGeneratorService reportService;

        public BandFReverseEANsProcess(IAppConfig config, ILogService log, IEventQueue events, IBandFRFIDReportGeneratorService reportService)
        {
            this.config = config;
            this.log = log.GetSection("ReverseFlow");
            this.events = events;
            this.reportService = reportService;

        }

        public TimeSpan GetIdleTime()
        {

            var scheduledAt = config.GetValue<string>("CustomSettings.BandF.ExecutionDailyTime", "01:01:00");
            var now = DateTime.Now;
#if DEBUG
            scheduledAt = now.AddMinutes(1).ToString("hh:mm:ss");
#endif


            var scheduledAtSplit = scheduledAt.Split(':');

            var nextExecutionTime = new DateTime(now.Year, now.Month, now.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));

            if(nextExecutionTime < now)
            {
                var nextDay = now.AddDays(1).Date;
                nextExecutionTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));
            }

            log.LogMessage($"B&F time config hour: {scheduledAt}  next: {nextExecutionTime}");


            return nextExecutionTime - now;

        }
        public void OnLoad() { }

        public void OnExecute()
        {
            var Enabled = config.GetValue<bool>("CustomSettings.BandF.Enabled", false);
            var companyID = config.GetValue("CustomSettings.BandF.CompanyID", 0);

            var baseDate = DateTime.Now;
            //var baseDate = new DateTime(2023, 6, 21, 0, 0, 0);
            var from = baseDate.AddDays(-1).Date;
            var to = from.AddDays(1).AddTicks(-1);
            var now = DateTime.Now;

            //log.LogMessage($"B&F - executando proceso '{Enabled}'");

            if(!Enabled) return;

            
            events.Send(new BandFDailyEANReportEvent(0, 0, string.Empty, companyID, 0, 0, from, to));
            //log.LogMessage($"B&F - Evento Reporte Envidado '{Enabled}'");

            var sendHistory = config.GetValue("CustomSettings.BandF.SendHistory", false);

            if(!sendHistory) return;

            //var startHistoryDate = config.GetValue("CustomSettings.BandF.HistoryStartDate", now.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
            //log.LogMessage($"B&F history will be send from {DateTime.Parse(startHistoryDate)} to {now.Date.AddTicks(-1)}");
            //reportService.SendHistory(companyID, DateTime.Parse(startHistoryDate), now.Date.AddTicks(-1));


        }

        public void OnUnload() { }

    }
}
