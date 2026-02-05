
namespace Service.Contracts.PrintCentral
{
	public class OrderCompletedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderCompletedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderCompletedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
