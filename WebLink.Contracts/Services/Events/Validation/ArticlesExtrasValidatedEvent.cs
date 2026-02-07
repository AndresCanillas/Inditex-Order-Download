namespace WebLink.Contracts
{
	public class ArticlesExtraStepStartedEvent : AbstractValidationFlowEvent
	{
		public ArticlesExtraStepStartedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}

	}

	public class ArticlesExtraStepCompletedEvent : AbstractValidationFlowEvent
	{
		public ArticlesExtraStepCompletedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}
	}
}