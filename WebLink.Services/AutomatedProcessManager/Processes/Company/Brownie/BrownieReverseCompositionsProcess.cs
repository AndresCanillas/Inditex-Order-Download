using Service.Contracts;
using System;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Services
{
    public class BrownieReverseCompositionsProcess : IAutomatedProcess
    {
        private readonly IAppConfig _config;
        private readonly IEventQueue _events;
        public BrownieReverseCompositionsProcess(IAppConfig config, IEventQueue events)
        {
            _config = config;
            _events = events;
        }

        public TimeSpan GetIdleTime()
        {
            var scheduledAt = _config.GetValue<string>("CustomSettings.Brownie.ExecutionDailyTime", "01:00:00");
            var now = DateTime.Now;

#if DEBUG
            scheduledAt = now.AddMinutes(1).ToString("HH:mm:ss");
#endif

            if(_config.GetValue<bool>("WebLink.IsQA", false) == true)
                scheduledAt = now.AddMinutes(3).ToString("HH:mm:ss");

            var scheduledAtSplit = scheduledAt.Split(':');

            var nextExecutionTime = new DateTime(now.Year, now.Month, now.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));

            if(nextExecutionTime < now)
            {
                var nextDay = now.AddDays(1).Date;
                nextExecutionTime = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, int.Parse(scheduledAtSplit[0]), int.Parse(scheduledAtSplit[1]), int.Parse(scheduledAtSplit[2]));
            }

            return nextExecutionTime - now;
        }

        public void OnExecute()
        {
            var Enabled = _config.GetValue<bool>("CustomSettings.Brownie.Enabled", false);
            var companyID = _config.GetValue("CustomSettings.Brownie.CompanyID", 0);

            var baseDate = DateTime.Now;
            var from = baseDate.AddDays(-1).Date;
            var to = from.AddDays(1).AddTicks(-1);
            var now = DateTime.Now;

            if(!Enabled) return;

            _events.Send(new BrownieDailyReportEvent(0, 0, string.Empty, companyID, 0, 0, from, to));
        }

        public void OnLoad()
        {
        }

        public void OnUnload()
        {
        }
    }
}
