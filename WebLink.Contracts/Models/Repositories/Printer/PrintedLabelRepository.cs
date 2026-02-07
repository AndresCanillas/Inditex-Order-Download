using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class EncodedLabelRepository : IEncodedLabelRepository
    {
        private IFactory factory;
        private IEventQueue events;

        public EncodedLabelRepository(IFactory factory, IEventQueue events)
        {
            this.factory = factory;
            this.events = events;
        }

        public void AddPrintedLabel(int detailid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                AddPrintedLabel(ctx, detailid);
            }
        }


        public void AddPrintedLabel(PrintDB ctx, int detailid)
        {
            var detail = ctx.PrinterJobDetails.Where(p => p.ID == detailid).SingleOrDefault();
            if(detail == null) return;
            var job = ctx.PrinterJobs.Where(p => p.ID == detail.PrinterJobID).SingleOrDefault();
            if(job == null) return;
            job.Printed++;
            detail.Printed++;
            ctx.SaveChanges();
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobDetailUpdate, detail));
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobStatusUpdate, job));
        }


        public int AddEncodedLabel(
            int jobid, int detailid, int printerid, int companyid, int projectid,
            string articleCode, string productCode, ProductionType ptype, int prodLocation,
            long serial, string tid, string epc, string accessPwd, string killPwd,
            bool success, string errorCode, bool updatePrinted, bool updateEncoded)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return AddEncodedLabel(ctx, jobid, detailid, printerid, companyid, projectid,
                    articleCode, productCode, ptype, prodLocation,
                    serial, tid, epc, accessPwd, killPwd,
                    success, errorCode, updatePrinted, updateEncoded);
            }
        }


        public int AddEncodedLabel(PrintDB ctx,
            int jobid, int detailid, int printerid, int companyid, int projectid,
            string articleCode, string productCode, ProductionType ptype, int prodLocation,
            long serial, string tid, string epc, string accessPwd, string killPwd,
            bool success, string errorCode, bool updatePrinted, bool updateEncoded)
        {

            IPrinterJob job = ctx.PrinterJobs.Where(p => p.ID == jobid).SingleOrDefault();
            IPrinterJobDetail detail = ctx.PrinterJobDetails.Where(p => p.ID == detailid).SingleOrDefault();
            if(job == null || detail == null)
                return 0;

            var label = new EncodedLabel()
            {
                DeviceID = printerid,
                CompanyID = companyid,
                ProjectID = projectid,
                ArticleCode = articleCode,
                Barcode = productCode,
                ProductionType = (int)ptype,
                ProductionLocationID = prodLocation,
                Serial = serial,
                TID = tid,
                EPC = epc,
                AccessPassword = accessPwd,
                KillPassword = killPwd,
                Success = success,
                ErrorCode = errorCode,
                Date = DateTime.Now
            };

            ctx.EncodedLabels.Add(label);

            if(updatePrinted)
            {
                job.Printed++;
                detail.Printed++;
            }

            if(updateEncoded)
            {
                job.Encoded++;
                detail.Encoded++;
            }

            ctx.Update(job);
            ctx.Update(detail);
            ctx.SaveChanges();

            events.Send(new PrinterJobEvent(companyid, PrinterJobEventType.JobDetailUpdate, detail));
            events.Send(new PrinterJobEvent(companyid, PrinterJobEventType.JobStatusUpdate, job));
            return label.ID;
        }

        public IEnumerable<IEncodedLabel> GetByOrderId(int orderId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrderId(ctx, orderId).ToList();
            }
        }

        public IEnumerable<IEncodedLabel> GetByOrderId(PrintDB ctx, int orderId)
        {
            return ctx.EncodedLabels.Where(e => e.OrderID == orderId);
        }

        public IEnumerable<IEncodedLabel> GetByOrderId(IEnumerable<int> orders, int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrderId(ctx, orders, projectID).ToList();
            }
        }

        public IEnumerable<IEncodedLabel> GetByOrderId(PrintDB ctx, IEnumerable<int> orders, int projectID)
        {
            // TODO: Completed order does not mean that all encoded labels are synchronized
            var byState = new List<SyncState>() { SyncState.Completed, SyncState.Processed };

            return ctx.EncodedLabels
                .AsNoTracking()
                .Where(w => byState.Any(a => a == w.SyncState))
                .Where(e => e.ProjectID == projectID && orders.Any(a => e.OrderID == a));
        }

        public IEnumerable<IEncodedLabel> GetForPendingReverseFlow(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetForPendingReverseFlow(ctx, orderID);
            }

        }

        public IEnumerable<IEncodedLabel> GetForPendingEkoiReverseFlow(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetForPendingEkoiReverseFlow(ctx, orderID);
            }

        }

        public IEnumerable<IEncodedLabel> GetForPendingReverseFlow(PrintDB ctx, int orderID)
        {
            return ctx.EncodedLabels
                .AsNoTracking()
               .Where(l => l.OrderID == orderID && l.SyncState == SyncState.Completed)
               .OrderBy(l => l.Barcode)
               .ToList();
        }

        public IEnumerable<IEncodedLabel> GetForPendingEkoiReverseFlow(PrintDB ctx, int orderID)
        {
            return ctx.EncodedLabels
                .AsNoTracking()
               .Where(l => l.OrderID == orderID && (l.SyncState == SyncState.Completed || l.SyncState == SyncState.Pending))
               .OrderBy(l => l.Barcode)
               .ToList();
        }



        public IEnumerable<IEncodedLabel> GetForPendingReverseFlow(int orderID, int limit)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetForPendingReverseFlow(ctx, orderID, limit);
            }

        }

        public IEnumerable<IEncodedLabel> GetForPendingReverseFlow(PrintDB ctx, int orderID, int limit)
        {
            return ctx.EncodedLabels
                .AsNoTracking()
               .Where(l => l.OrderID == orderID && l.SyncState == SyncState.Completed)
               .OrderBy(l => l.Barcode)
               .Take(limit)
               .ToList();
        }

        public void MarkAsProcessedInReport(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                MarkAsProcessedInReport(ctx, orderID);
            }
        }

        public void MarkAsProcessedInReport(PrintDB ctx, int orderID)
        {
            var encoded = ctx.EncodedLabels
                .Where(w => w.OrderID == orderID)
                .Where(w => w.SyncState == SyncState.Completed)
                .Select(s => s)
                .ToList();

            encoded.ForEach(e => e.SyncState = SyncState.Processed);

            ctx.SaveChanges();

        }

        public void MarkAsProcessedInEkoiReport(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                MarkAsProcessedInEkoiReport(ctx, orderID);
            }
        }

        public void MarkAsProcessedInEkoiReport(PrintDB ctx, int orderID)
        {
            var encoded = ctx.EncodedLabels
                .AsNoTracking()
               .Where(l => l.OrderID == orderID && (l.SyncState == SyncState.Completed || l.SyncState == SyncState.Pending))
               .OrderBy(l => l.Barcode)
               .ToList();

            encoded.ForEach(e => e.SyncState = SyncState.Processed);

            ctx.SaveChanges();

        }

        public void UpdateSyncState(IEnumerable<IEncodedLabel> encodedLabels, SyncState syncState)
        {
            using(var conn = factory.GetInstance<IDBConnectionManager>().OpenWebLinkDB())
            {
                var ids = encodedLabels.Select(label => label.ID).Merge(",");
                var reader = conn.ExecuteNonQuery($@"
                    UPDATE EncodedLabels
                    SET SyncState = @syncState
                    WHERE ID in ({ids})
                ", syncState);
            }
        }


        public IEnumerable<IEncodedLabel> GetForPendingReverseFlowSortedByID(int orderID, int limit, long lastID = 0)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetForPendingReverseFlowSortedByID(ctx, orderID, limit, lastID).ToList();
            }
        }

        public IEnumerable<IEncodedLabel> GetForPendingReverseFlowSortedByID(PrintDB ctx, int orderID, int limit, long lastID = 0)
        {
            return ctx.EncodedLabels
                .AsNoTracking()
               .Where(l => l.OrderID == orderID && l.SyncState == SyncState.Completed)
               .Where(w => w.ID > lastID)
               .OrderBy(l => l.ID)
               .Take(limit)
               .ToList();
        }

        public IEnumerable<int> GetOrderIDEncodeBetweenDates(int companyID, DateTime from, DateTime to)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderIDEncodeBetweenDates(ctx, companyID, from, to);
            }
        }

        public IEnumerable<int> GetOrderIDEncodeBetweenDates(PrintDB ctx, int companyID, DateTime from, DateTime to)
        {
            var q = ctx.EncodedLabels
                .AsNoTracking()
                .Where(w => w.CompanyID == companyID)
                .Where(w => w.Date >= from && w.Date <= to)
                .Select(s => s.OrderID)
                .Distinct()
                .ToList(); // execute query, because return only IDS

            return q;

        }
    }
}
