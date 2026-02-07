
using Service.Contracts;

namespace WebLink.Contracts
{
	public class QuantitiesStepStartedEvent : AbstractValidationFlowEvent
	{
		public QuantitiesStepStartedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			: base(orderID, orderNumber, companyid, brandid, projectid)
		{

		}
	}

	public class QuantitiesStepCompletedEvent : AbstractValidationFlowEvent
	{
		public QuantitiesStepCompletedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
			:base(orderID, orderNumber, companyid, brandid, projectid)
		{
			
		}
	}
}