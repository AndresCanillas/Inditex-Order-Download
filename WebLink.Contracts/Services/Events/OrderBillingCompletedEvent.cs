
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public class OrderBillingCompletedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderBillingCompletedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderBillingCompletedEvent(BaseOrderEvent e)
			:base(e)
		{

		}
	}
}
