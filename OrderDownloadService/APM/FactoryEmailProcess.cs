using OrderDonwLoadService.Model;
using OrderDonwLoadService.Services;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrderDonwLoadService
{
    public class FactoryEmailProcess : IAutomatedProcess
    {
        private readonly IOrderStatusStore store;
        private readonly IMailService mail;
        private readonly IAppConfig config;
        private readonly IAppLog log;
        private List<FactoryContact> contacts = new List<FactoryContact>();
        private List<TimeSpan> schedule = new List<TimeSpan>();
        private DateTime nextRun;

        public FactoryEmailProcess(IOrderStatusStore store, IMailService mail, IAppConfig config, IAppLog log)
        {
            this.store = store;
            this.mail = mail;
            this.config = config;
            this.log = log;
        }

        public TimeSpan GetIdleTime()
        {
            var diff = nextRun - DateTime.Now;
            if(diff.TotalSeconds < 10)
                diff = TimeSpan.FromSeconds(10);
            return diff;
        }

        public void OnLoad()
        {
            contacts = OrderDownloadHelper.LoadFactoryContacts(log) ?? new List<FactoryContact>();
            LoadSchedule();
            nextRun = CalculateNextRun(DateTime.Now);
        }

        public void OnUnload() { }

        public void OnExecute()
        {
            if(DateTime.Now >= nextRun)
            {
                try
                {
                    var entries = store.GetAndClear();
                    if(entries.Count > 0)
                    {
                        foreach(var grp in entries.GroupBy(e => e.VendorId))
                        {
                            var contact = contacts.FirstOrDefault(c => c.VendorId == grp.Key);
                            if(contact == null)
                                continue;
                            var sb = new StringBuilder();
                            sb.AppendLine($"Orders report {DateTime.Now:yyyy-MM-dd HH:mm}");
                            foreach(var st in grp)
                            {
                                sb.AppendLine($"Order {st.OrderId} - {(st.SentToPrintCentral ? "Sent" : "Pending")}");
                            }
                            mail.SendMail(contact.SendTo, contact.CopyTo, "Orders Status", sb.ToString());
                        }
                    }
                }
                catch(Exception ex)
                {
                    log.LogException(ex);
                }
                nextRun = CalculateNextRun(DateTime.Now);
            }
        }

        private void LoadSchedule()
        {
            schedule.Clear();
            var times = config.GetValue<string>("FactoryEmail.Times", "09:00");
            foreach(var t in times.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if(TimeSpan.TryParse(t, out var ts))
                    schedule.Add(ts);
            }
            if(schedule.Count == 0)
                schedule.Add(new TimeSpan(9,0,0));
            schedule.Sort();
        }

        private DateTime CalculateNextRun(DateTime from)
        {
            foreach(var ts in schedule)
            {
                var candidate = from.Date + ts;
                if(candidate > from)
                    return candidate;
            }
            return from.Date.AddDays(1) + schedule[0];
        }
    }
}
