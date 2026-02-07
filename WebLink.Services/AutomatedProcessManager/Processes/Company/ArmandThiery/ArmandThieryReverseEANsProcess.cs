using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services.Automated
{
    public class ArmandThieryReverseEANsProcess : IAutomatedProcess
    {
        private readonly IAppConfig config;
        private readonly IAppLog log;
        private readonly IEventQueue events;
        public ArmandThieryReverseEANsProcess(IAppConfig config, IAppLog log, IEventQueue events)
        {
            this.config = config;
            this.log = log;
            this.events = events;
        }

        public TimeSpan GetIdleTime()
        {
            var scheduledAt = config.GetValue<string>("CustomSettings.ArmandThiery.ExecutionDailyTime", "01:01:00");
            var now = DateTime.Now;
#if DEBUG
            scheduledAt = now.AddSeconds(15).ToString("HH:mm:ss");
#endif


            var scheduledAtSplit = scheduledAt.Split(':');

            var nextExecutionTime = new DateTime(now.Year, now.Month, now.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));

            if(nextExecutionTime < now)
            {
                var nextDay = now.AddDays(1).Date;
                nextExecutionTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));
            }

            log.LogMessage($"Armand Thiery time config hour: {scheduledAt}  next: {nextExecutionTime}");

            return nextExecutionTime - now;
        }

        public void OnExecute()
        {
            var Enabled = config.GetValue<bool>("CustomSettings.ArmandThiery.Enabled", false);
            var companyID = config.GetValue("CustomSettings.ArmandThiery.CompanyID", 0);

            if(!Enabled) return;

            events.Send(new ArmandThieryDailyEANsReportEvent(0, 0, string.Empty, companyID, 0, 0));
            log.LogMessage($"Armand Thiery - Event Report EPCS Send '{Enabled}'");

        }

        public void OnLoad() {}

        public void OnUnload() {}
    }
}
