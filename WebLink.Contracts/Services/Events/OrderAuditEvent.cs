using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
    public class OrderAuditEvent : BaseOrderEvent
    {
        public int CompositionAuditID { get; set; }        
        [Newtonsoft.Json.JsonConstructor]
        public OrderAuditEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid, int compoAuditID)
            : base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
        {
            CompositionAuditID = compoAuditID; 
        }

        
           

        public OrderAuditEvent(BaseOrderEvent e)
            : base(e)
        {

        }

    }
}
