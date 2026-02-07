using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IOrderActionsService
    {
		void StopOrder(int orderid);
		void StopOrder(PrintDB ctx, int orderid);

		void ResumeOrder(int orderid);
		void ResumeOrder(PrintDB ctx, int orderid);

		void RejectOrder(int orderid);
		void RejectOrder(PrintDB ctx, int orderid);

		void MoveOrder(int orderid, int locationId);
		void MoveOrder(PrintDB ctx, int orderid, int locationId);

        void OrderWithDuplicatedEPC(int orderid);
        void ActiveOrderWithDuplicatedEPC(int orderid);
    }
}
