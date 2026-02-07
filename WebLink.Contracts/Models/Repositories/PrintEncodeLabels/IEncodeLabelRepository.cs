using System;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IEncodedLabelRepository
    {
        void AddPrintedLabel(int detailid);
        void AddPrintedLabel(PrintDB ctx, int detailid);

        int AddEncodedLabel(
            int jobid, int detailid, int printerid, int companyid, int projectid,
            string articleCode, string productCode, ProductionType ptype, int prodLocation,
            long serial, string tid, string epc, string accessPwd, string killPwd,
            bool success, string errorCode, bool updatePrinted, bool updateEncoded);

        int AddEncodedLabel(PrintDB ctx,
            int jobid, int detailid, int printerid, int companyid, int projectid,
            string articleCode, string productCode, ProductionType ptype, int prodLocation,
            long serial, string tid, string epc, string accessPwd, string killPwd,
            bool success, string errorCode, bool updatePrinted, bool updateEncoded);


        IEnumerable<IEncodedLabel> GetByOrderId(int orderId);
        IEnumerable<IEncodedLabel> GetByOrderId(PrintDB ctx, int orderId);

        IEnumerable<IEncodedLabel> GetByOrderId(IEnumerable<int> orders, int projectID);
        IEnumerable<IEncodedLabel> GetByOrderId(PrintDB ctx, IEnumerable<int> orders, int projectID);

        IEnumerable<IEncodedLabel> GetForPendingReverseFlow(int orderID);
        IEnumerable<IEncodedLabel> GetForPendingReverseFlow(PrintDB ctx, int orderID);

        IEnumerable<IEncodedLabel> GetForPendingEkoiReverseFlow(int orderID);
        IEnumerable<IEncodedLabel> GetForPendingEkoiReverseFlow(PrintDB ctx, int orderID);

        IEnumerable<IEncodedLabel> GetForPendingReverseFlow(int orderID, int limit);
        IEnumerable<IEncodedLabel> GetForPendingReverseFlow(PrintDB ctx, int orderID, int limit);

        IEnumerable<IEncodedLabel> GetForPendingReverseFlowSortedByID(int orderID, int limit, long lastID = 0);
        IEnumerable<IEncodedLabel> GetForPendingReverseFlowSortedByID(PrintDB ctx, int orderID, int limit, long lastID = 0);

        IEnumerable<int> GetOrderIDEncodeBetweenDates(int companyID, DateTime from, DateTime to);
        IEnumerable<int> GetOrderIDEncodeBetweenDates(PrintDB ctx, int companyID, DateTime from, DateTime to);


        void MarkAsProcessedInReport(int orderID);
        void MarkAsProcessedInReport(PrintDB ctx, int orderID);

        void MarkAsProcessedInEkoiReport(int orderID);
        void MarkAsProcessedInEkoiReport(PrintDB ctx, int orderID);

        void UpdateSyncState(IEnumerable<IEncodedLabel> encodedLabels, SyncState syncState);
    }
}
