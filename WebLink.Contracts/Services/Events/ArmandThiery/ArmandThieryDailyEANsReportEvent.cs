using Service.Contracts.PrintCentral;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.OrderEvents
{

    public class ArmandThieryDailyEANsReportEvent : BaseOrderEvent
    {
        [Newtonsoft.Json.JsonConstructor]
        public ArmandThieryDailyEANsReportEvent(int orderGroupID, int orderID, string orderNumber,int companyID, int brandID, int projectID) : 
            base(orderGroupID, orderID, orderNumber,companyID, brandID, projectID)
        {
        }

        public ArmandThieryDailyEANsReportEvent(BaseOrderEvent e)
            : base(e)
        {

        }
    }
}
