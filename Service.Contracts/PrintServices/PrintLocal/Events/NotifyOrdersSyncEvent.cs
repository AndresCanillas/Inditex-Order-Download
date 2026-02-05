using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Service.Contracts.PrintLocal
{
    public class NotifyOrdersSyncEvent : EQEventInfo
    {
        public IEnumerable<int> Orders { get; set; }
        public int FactoryID { get; set; }
        public double DeltaTime { get; set; }

        public NotifyOrdersSyncEvent() { }

        public NotifyOrdersSyncEvent(IEnumerable<int> orders, int factoryID, double deltaTime) 
        {
            Orders = orders;
            FactoryID = factoryID;
            DeltaTime = deltaTime;
        }
    }
}
