using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public class SupportFilesValidatedEvent : AbstractValidationFlowEvent
    {
        public SupportFilesValidatedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
            : base(orderID, orderNumber, companyid, brandid, projectid)
        {

        }
    }

    public class SupportFilesStepCompletedEvent : AbstractValidationFlowEvent
    {
        public SupportFilesStepCompletedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
            : base(orderID, orderNumber, companyid, brandid, projectid)
        {

        }
    }
}
