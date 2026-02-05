
namespace Service.Contracts.PrintCentral
{
	public class OrderStoppedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderStoppedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderStoppedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
