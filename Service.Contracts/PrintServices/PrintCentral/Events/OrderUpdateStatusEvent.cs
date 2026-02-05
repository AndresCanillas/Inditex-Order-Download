
namespace Service.Contracts.PrintCentral
{
	public class OrderUpdateStatusEvent : BaseOrderEvent
	{
        public int OrderStatus { get; set; }

		[Newtonsoft.Json.JsonConstructor]
		public OrderUpdateStatusEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

        public OrderUpdateStatusEvent()
        { }

        public OrderUpdateStatusEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
