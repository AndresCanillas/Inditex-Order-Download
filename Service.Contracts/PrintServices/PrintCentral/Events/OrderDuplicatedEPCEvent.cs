
namespace Service.Contracts.PrintCentral
{
	public class OrderDuplicatedEPCEvent : BaseOrderEvent
	{
		[Newtonsoft.Json.JsonConstructor]
		public OrderDuplicatedEPCEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

		public OrderDuplicatedEPCEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
