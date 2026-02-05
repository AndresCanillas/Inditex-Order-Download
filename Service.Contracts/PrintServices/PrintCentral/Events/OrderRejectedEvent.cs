
namespace Service.Contracts.PrintCentral
{
	public class OrderRejectedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderRejectedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderRejectedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
