
namespace Service.Contracts.PrintCentral
{
	public class OrderMovedEvent : BaseOrderEvent
	{
        public int LocationId { get; set; }

        [Newtonsoft.Json.JsonConstructor]
		public OrderMovedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid, int locationId)
			: base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
		{
            LocationId = locationId;
		}

		public OrderMovedEvent(BaseOrderEvent e)
		   : base(e)
		{

		}
	}
}
