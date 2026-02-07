using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public interface IOrderUpdateService : IOrderWorkFlowAction
    {
		void Accept(int orderID, OrderStatus newStatus = OrderStatus.Processed);
		void Accept(PrintDB ctx, int orderID, OrderStatus newStatus = OrderStatus.Processed);

        void Update(ConflictMethod conflictMethod, int acceptedOrderID, params int[] rejectedOrderIDs);
        void Update(PrintDB ctx, ConflictMethod conflictMethod, int acceptedOrderID, params int[] rejectedOrderIDs);
        void Update(ConflictMethod conflictMethod, int acceptedOrderID, IEnumerable<int> rejectedOrderIDs);
        void Update(PrintDB ctx, ConflictMethod conflictMethod, int acceptedOrderID, IEnumerable<int> rejectedOrderIDs);

        void Reject(PrintDB ctx, int orderIDs);
        void Reject(params int[] orderIDs);
		void Reject(PrintDB ctx, params int[] orderID);

        void Reject(IEnumerable<int> orderIDs);
        void Reject(PrintDB ctx, IEnumerable<int> orderID);

        void RejectBySharedData(int orderID);
        void RejectBySharedData(PrintDB ctx, int orderID);

        //void MarkConflict(int oldOrderID);
        //void MarkConflict(PrintDB ctx, int oldOrderID);

        void MarkConflict(ConflictMethod conflictMethod, int oldOrderID);
        void MarkConflict(PrintDB ctx, ConflictMethod conflictMethod, int oldOrderID);




    }
}
