
using Service.Contracts.PrintCentral;
using System.Security.Cryptography.X509Certificates;

namespace WebLink.Contracts
{
    public class OrderExistVerifiedEvent : BaseOrderEvent
    {
        public bool ContinueProcessing { get; set; }

		public OrderExistVerifiedEvent()
		{
			//NOTE: Empty constructor is required by APM Workflow Constraints
		}

		[Newtonsoft.Json.JsonConstructor]
        public OrderExistVerifiedEvent(int orderGroupID, int orderID, string orderNumber, int companyid, int brandid, int projectid, bool continueProcessing)
            : base(orderGroupID, orderID, orderNumber, companyid, brandid, projectid)
        {
            ContinueProcessing = continueProcessing;
        }

        public OrderExistVerifiedEvent(BaseOrderEvent e)
            : base(e)
        {
            ContinueProcessing = true;
        }
	}
}
