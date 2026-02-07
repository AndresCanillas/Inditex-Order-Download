using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.OrderEvents;

namespace WebLink.Contracts.OrderEvents
{
    public class BrownieDailyReportEvent : BaseOrderEvent
    {
        public DateTime StartDate { get; set; } // string tart date YYYY-MM-DD
        public DateTime EndDate { get; set; } // strate end date YYYY-MM-DD


        [Newtonsoft.Json.JsonConstructor]
        public BrownieDailyReportEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid, DateTime from, DateTime to)
            : base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
        {
            StartDate = from;
            EndDate = to;
        }

        public BrownieDailyReportEvent(BaseOrderEvent e)
            : base(e)
        {

        }

        public BrownieDailyReportEvent(BrownieDailyReportEvent e)
            : base(e)
        {
            StartDate = e.StartDate;
            EndDate = e.EndDate;
        }
    }
}
