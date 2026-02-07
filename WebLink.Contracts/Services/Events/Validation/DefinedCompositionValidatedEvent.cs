using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{

    public class DefinedCompositionStepStaredEvent : AbstractValidationFlowEvent
    {
        public DefinedCompositionStepStaredEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
            : base(orderID, orderNumber, companyid, brandid, projectid)
        {

        }
    }

    public class DefinedCompositionCompletedEvent : AbstractValidationFlowEvent
    {
        public DefinedCompositionCompletedEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
            : base(orderID, orderNumber, companyid, brandid, projectid)
        {

        }
    }
}
