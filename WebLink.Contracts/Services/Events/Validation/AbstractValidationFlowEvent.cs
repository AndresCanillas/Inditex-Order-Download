
using Service.Contracts;

namespace WebLink.Contracts
{
    public abstract class AbstractValidationFlowEvent : EQEventInfo
    {

        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public int BrandID { get; set; }
        public int ProjectID { get; set; }

        public AbstractValidationFlowEvent(int orderID, string orderNumber, int companyid, int brandid, int projectid)
        {
            OrderID = orderID;
            OrderNumber = orderNumber;
            CompanyID = companyid;
            BrandID = brandid;
            ProjectID = projectid;
        }
    }
}
