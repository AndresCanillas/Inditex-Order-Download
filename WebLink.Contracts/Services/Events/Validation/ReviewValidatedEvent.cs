namespace WebLink.Contracts
{
	public class ReviewStepStartedEvent : AbstractValidationFlowEvent
	{
		public ReviewStepStartedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}

	}

	public class ReviewStepCompletedEvent : AbstractValidationFlowEvent
	{
		public ReviewStepCompletedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}
	}
}