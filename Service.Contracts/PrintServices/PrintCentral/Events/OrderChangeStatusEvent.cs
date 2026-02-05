
namespace Service.Contracts.PrintCentral
{
	public class OrderChangeStatusEvent : BaseOrderEvent
	{
        public int OrderStatus { get; set; }

		[Newtonsoft.Json.JsonConstructor]
		public OrderChangeStatusEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
		}

        public OrderChangeStatusEvent()
        { }

        public OrderChangeStatusEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
