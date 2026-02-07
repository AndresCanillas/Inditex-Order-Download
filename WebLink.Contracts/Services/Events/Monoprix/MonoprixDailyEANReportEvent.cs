using Service.Contracts.PrintCentral;
using System;

namespace WebLink.Contracts.OrderEvents
{
    public class MonoprixDailyEANReportEvent : BaseOrderEvent
    {

        public DateTime StartDate { get; set; } // string tart date YYYY-MM-DD
        public DateTime EndDate { get; set; } // strate end date YYYY-MM-DD


        [Newtonsoft.Json.JsonConstructor]
        public MonoprixDailyEANReportEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid, DateTime from, DateTime to)
            : base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
        {
            StartDate = from;
            EndDate = to;
        }

        public MonoprixDailyEANReportEvent(BaseOrderEvent e)
            : base(e)
        {

        }

        public MonoprixDailyEANReportEvent(MonoprixDailyEANReportEvent e)
            : base(e)
        {
            StartDate = e.StartDate;
            EndDate = e.EndDate;
        }

    }
}
