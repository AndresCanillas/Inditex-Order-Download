using Service.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    [TargetTable("PrinterJobs")]
    public class PrinterJob : IPrinterJob, ICompanyFilter<PrinterJob>, ISortableSet<PrinterJob>
    {
        private object syncObj = new object();
        private volatile int printed;
        private volatile int errors;
        private volatile int extras;
        private volatile int encoded;

        [PK, Identity]
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int CompanyOrderID { get; set; }
        public int ProjectID { get; set; }
        [IgnoreField]
        public Order CompanyOrder { get; set; }
        public int? ProductionLocationID { get; set; }
        public int? AssignedPrinter { get; set; }
        public int ArticleID { get; set; }
        public int Quantity { get; set; }

        public int Printed
        {
            get { lock(syncObj) return printed; }
            set { lock(syncObj) printed = value; }
        }

        public void IncPrinted()
        {
            lock(syncObj) printed += 1;
        }

        public int Errors
        {
            get { lock(syncObj) return errors; }
            set { lock(syncObj) errors = value; }
        }

        public void IncErrors()
        {
            lock(syncObj) errors += 1;
        }

        public int Extras
        {
            get { lock(syncObj) return extras; }
            set { lock(syncObj) extras = value; }
        }

        public int Encoded
        {
            get { lock(syncObj) return encoded; }
            set { lock(syncObj) encoded = value; }
        }

        public void IncEncoded()
        {
            lock(syncObj) encoded += 1;
        }

        public DateTime? DueDate { get; set; }
        public JobStatus Status { get; set; }
        public bool AutoStart { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool PrintPackageGenerated { get; set; }

        public int GetCompanyID(PrintDB db) => CompanyID;

        public Task<int> GetCompanyIDAsync(PrintDB db) => Task.FromResult(CompanyID);

        public IQueryable<PrinterJob> FilterByCompanyID(PrintDB db, int companyid) =>
            from pj in db.PrinterJobs
            where pj.CompanyID == companyid
            select pj;

        public IQueryable<PrinterJob> ApplySort(IQueryable<PrinterJob> qry) => qry.OrderByDescending(p => p.CreatedDate);
    }
}

