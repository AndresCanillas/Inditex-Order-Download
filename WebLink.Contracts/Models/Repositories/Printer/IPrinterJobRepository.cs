using Service.Contracts.PrintLocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface IPrinterJobRepository
    {
        IQueryable<IPrinterJob> All(PrintDB ctx, bool byPassChecks = false);

        IPrinterJob GetByID(int jobid, bool byPassChecks = false);
        IPrinterJob GetByID(PrintDB ctx, int jobid, bool byPassChecks = false);

        IEnumerable<IPrinterJob> GetByOrderID(int orderid, bool byPassChecks = false);
        IEnumerable<IPrinterJob> GetByOrderID(PrintDB ctx, int orderid, bool byPassChecks = false);

        void Delete(int jobid);
        void Delete(PrintDB ctx, int jobid);

        List<IPrinterJobDetail> JobDetails(int jobid);
        List<IPrinterJobDetail> JobDetails(PrintDB ctx, int jobid);

        Task<List<IPrinterJobDetail>> JobDetailsAsync(int jobid);
        Task<List<IPrinterJobDetail>> JobDetailsAsync(PrintDB ctx, int jobid);

        IEnumerable<IPrinterJobDetail> JobDetailsByOrder(int orderID);
        IEnumerable<IPrinterJobDetail> JobDetailsByOrder(PrintDB ctx, int orderID);

        IEnumerable<JobHeaderDTO> GetPendingJobs();
        IEnumerable<JobHeaderDTO> GetPendingJobs(PrinterJobFilter filter);
        IEnumerable<JobHeaderDTO> GetPrinterJobs(int printerid);

        IEnumerable<PrintJobDetailDTO> GetJobDetails(int jobid, bool applySort);
        IEnumerable<PrintJobDetailDTO> GetJobDetails(PrintDB ctx, int jobid, bool applySort);

        void StartJob(int jobid);
        void PauseJob(int jobid);
        void CancelJob(int jobid);
        void ActivateJob(int jobid);

        void ChangePrinter(int jobid, int printerid);
        void ChangePrinter(PrintDB ctx, int jobid, int printerid);

        IPrinterJobDetail AddExtras(int jobid, int detailid, int quantity);
        IPrinterJobDetail AddExtras(PrintDB ctx, int jobid, int detailid, int quantity);

        IPrinterJob GetNextPrinterJob(int printerid);
        IPrinterJob GetNextPrinterJob(PrintDB ctx, int printerid);

        void SetDueDate(int jobid, DateTime date);
        void SetDueDate(PrintDB ctx, int jobid, DateTime date);

        void SetJobLocation(int jobid, int locationid);
        void SetJobLocation(PrintDB ctx, int jobid, int locationid);

        void SetJobPrinter(int jobid, int locationid, int printerid);
        void SetJobPrinter(PrintDB ctx, int jobid, int locationid, int printerid);

        void UpdateJobState(int jobid, JobStatus status);
        void UpdateJobState(PrintDB ctx, int jobid, JobStatus status);

        void ResetProgress(int jobid);
        void ResetProgress(PrintDB ctx, int jobid);

        void SetDetailProgress(int detailid, int progress);
        void SetDetailProgress(PrintDB ctx, int detailid, int progress);

        void UpdateDetailProgress(PLUnitProgressChangeEvent e);

        void UpdateEncoded(int jobid, int encoded);
        void UpdateEncoded(PrintDB ctx, int jobid, int encoded);

        void UpdateArticle(int jobid, int articleid);
        void UpdateArticle(PrintDB ctx, int jobid, int articleid);

        List<IPrinterJob> CreateFromOrder(int orderid, int? printerid, bool autoStart, int? articleID = null);
        List<IPrinterJob> CreateFromOrder(PrintDB ctx, int orderid, int? printerid, bool autoStart, int? articleID = null);

        int? GetLocationBy(int? printerid);
        int? GetLocationBy(PrintDB ctx, int? printerid);

        IPrinterJob CreateArticleOrder(IOrder order, IEnumerable<OrderProductionDetailRow> details, int? locationid, int? SLADays, int? printerid, bool autoStart = false);
        IPrinterJob CreateArticleOrder(PrintDB ctx, IOrder order, IEnumerable<OrderProductionDetailRow> details, int? locationid, int? SLADays, int? printerid, bool autoStart = false);

        int GetCompanyFromProject(int projectid);
        int GetCompanyFromProject(PrintDB ctx, int projectid);

        void UpdateQuantiesByGroup(List<OrderGroupQuantitiesDTO> updates);
        void UpdateQuantiesByGroup(PrintDB ctx, List<OrderGroupQuantitiesDTO> updates);

        IPrinterJob AddExtraJob(IPrinterJob data);
        IPrinterJob AddExtraJob(PrintDB ctx, IPrinterJob data);

        IPrinterJobDetail AddExtraDetailToJob(IPrinterJobDetail data);
        IPrinterJobDetail AddExtraDetailToJob(PrintDB ctx, IPrinterJobDetail data);

        void RefreshOrderQuantityValue(IEnumerable<int> orders);
        void RefreshOrderQuantityValue(PrintDB ctx, IEnumerable<int> orders);

        IEnumerable<IPrinterJob> GetAllJobsByOrderId(int orderid, bool byPassChecks = false);
        IEnumerable<IPrinterJob> GetAllJobsByOrderId(PrintDB ctx, int orderid, bool byPassChecks = false);

        IEnumerable<PrintJobDetailDTO> GetAllJobDetails(int jobid, bool applySort);
        IEnumerable<PrintJobDetailDTO> GetAllJobDetails(PrintDB ctx, int jobid, bool applySort);

    }
}
