
namespace Service.Contracts.PrintCentral
{
	public class OrderResumedEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderResumedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderResumedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
