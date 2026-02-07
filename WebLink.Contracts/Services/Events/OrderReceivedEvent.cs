
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public class OrderFileReceivedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderFileReceivedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid) 
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderFileReceivedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
