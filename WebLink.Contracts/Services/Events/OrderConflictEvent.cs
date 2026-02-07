
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
    public class OrderConflictEvent : BaseOrderEvent
    {
        [Newtonsoft.Json.JsonConstructor]
        public OrderConflictEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
            : base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
        {
        }

        public OrderConflictEvent (BaseOrderEvent e)
            : base(e)
        {

        }
    }


}
