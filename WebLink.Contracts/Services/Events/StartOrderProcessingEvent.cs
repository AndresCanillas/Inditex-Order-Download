
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public class StartOrderProcessingEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public StartOrderProcessingEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid) 
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public StartOrderProcessingEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
