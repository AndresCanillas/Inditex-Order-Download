
using Service.Contracts.PrintCentral;

namespace WebLink.Contracts
{
	public class OrderDocumentsCompletedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderDocumentsCompletedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderDocumentsCompletedEvent(BaseOrderEvent e)
			: base(e)
		{

		}
	}
}
