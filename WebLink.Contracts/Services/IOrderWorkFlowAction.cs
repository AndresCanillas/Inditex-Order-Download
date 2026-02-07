using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public interface IOrderWorkFlowAction {

        int Execute(int orderGroupID, int orderID, string orderNumber, int projectID, int brandID);

    }
}
