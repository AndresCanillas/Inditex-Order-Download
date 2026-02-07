
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public class OrderValidatedEvent : BaseOrderEvent
	{
		public OrderValidatedEvent()
		{
			//NOTE: Empty constructor is required by APM Workflow Constraints
		}

		[Newtonsoft.Json.JsonConstructor]
		public OrderValidatedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		
		public OrderValidatedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
