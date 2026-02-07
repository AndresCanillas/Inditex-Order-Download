namespace WebLink.Contracts
{
	public class AddressStepStartedEvent : AbstractValidationFlowEvent
	{
		public AddressStepStartedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}
	}

	public class AddressStepCompletedEvent : AbstractValidationFlowEvent
	{
		public AddressStepCompletedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}
	}
}