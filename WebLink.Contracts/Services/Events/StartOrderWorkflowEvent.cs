using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public class StartOrderWorkflowEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public StartOrderWorkflowEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public StartOrderWorkflowEvent(BaseOrderEvent e)
		   : base(e)
		{
		}
	}
}
