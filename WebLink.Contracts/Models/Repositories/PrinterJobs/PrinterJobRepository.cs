using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.PrintLocal;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public class PrinterJobRepository : IPrinterJobRepository
    {
        private IFactory factory;
        private IEventQueue events;
        private IAppConfig config;
        private IZPrinterManager printerManager;
        private IPrinterRepository printerRepo;
        private IDBConnectionManager connManager;
        private ILogService log;

        public PrinterJobRepository(
            IFactory factory,
            IEventQueue events,
            IAppConfig config,
            IZPrinterManager printerManager,
            IPrinterRepository printerRepo,
            IDBConnectionManager connManager,
            ILogService log)
        {
            this.factory = factory;
            this.events = events;
            this.config = config;
            this.printerManager = printerManager;
            this.printerRepo = printerRepo;
            this.connManager = connManager;
            this.log = log;
        }


        public IQueryable<IPrinterJob> All(PrintDB ctx, bool byPassChecks = false)
        {
            var userData = factory.GetInstance<IUserData>();

            if (byPassChecks)
            {
                return ctx.PrinterJobs;
            }
            else
            {
                ProductionType prodType;
                int companyid, locationid;
                GetFilters(userData, out prodType, out companyid, out locationid);
                return from pj in ctx.PrinterJobs
                       join o in ctx.CompanyOrders on pj.CompanyOrderID equals o.ID
                       where (prodType == 0 || o.ProductionType == prodType) &&
                             (companyid == 0 || pj.CompanyID == companyid || o.SendToCompanyID == companyid) &&
                             (locationid == 0 || pj.ProductionLocationID == null || pj.ProductionLocationID == locationid)
                       select pj;
            }
        }


        // ProductionType: 0 = All Types, 1 = IDT, 2 = Costumer. Production Type 0 should be used when user is from smart dots (companyid = 1), for users from other companies always use Production Type 2
        // CompanyID:	0 = All companies. Value 0 should be used exclusively when user is from smart dots (companyid = 1). 
        //				ID = See orders for that specified company, should be used whenever the user is from another company other than smart dots
        // LocationID:	0 = All locations, should only be used if the user has the SysAdmin or ProductionControl roles
        //				ID = See only jobs for the specified location, this should be set if the user has the PrinterOperator role
        private void GetFilters(IUserData userData, out ProductionType prodType, out int companyid, out int locationid)
        {
            prodType = ProductionType.CustomerLocation;
            companyid = userData.SelectedCompanyID;
            locationid = userData.LocationID;
            if (companyid == 1 || userData.IsIDT)
            {
                // Shows IDT production type, for all companies and potentially all locations
                prodType = ProductionType.All;
                companyid = 0;
                if (userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTProdManager))
                    locationid = 0; // See all locations, only for SysAdmin & IDTProdManager users
            }
            else
            {
                if (userData.Principal.IsAnyRole(Roles.CompanyAdmin, Roles.ProdManager))
                    locationid = 0; // show all locations, only for ProductionControl
            }

            //if (!userData.IsIDT)
            //{
            //	filter.CompanyID = userData.SelectedCompanyID;
            //}
            //else
            //{
            //	prodType = filter.CompanyID == 1 ? prodType : ProductionType.IDTLocation;
            //}
        }


        public IPrinterJob GetByID(int jobid, bool byPassChecks = false)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByID(ctx, jobid, byPassChecks);
            }
        }


        public IPrinterJob GetByID(PrintDB ctx, int jobid, bool byPassChecks = false)
        {
            return All(ctx, byPassChecks)
                .Where(p => p.ID == jobid)
                .AsNoTracking()
                .FirstOrDefault();
        }


        public IEnumerable<IPrinterJob> GetByOrderID(int orderid, bool byPassChecks = false)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByOrderID(ctx, orderid, byPassChecks);
            }
        }


        public IEnumerable<IPrinterJob> GetByOrderID(PrintDB ctx, int orderid, bool byPassChecks = false)
        {
            return All(ctx, byPassChecks)
                .Where(p => p.CompanyOrderID == orderid)
                .AsNoTracking()
                .ToList();
        }


        public void Delete(int jobid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                Delete(ctx, jobid);
            }
        }


        public void Delete(PrintDB ctx, int jobid)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            ctx.PrinterJobs.Remove(job);
            ctx.SaveChanges();
        }


        public List<IPrinterJobDetail> JobDetails(int jobid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return JobDetails(ctx, jobid);
            }
        }


        public List<IPrinterJobDetail> JobDetails(PrintDB ctx, int jobid)
        {
            return new List<IPrinterJobDetail>(
                ctx.PrinterJobDetails
                .Where(p => p.PrinterJobID == jobid)
            );
        }


        public async Task<List<IPrinterJobDetail>> JobDetailsAsync(int jobid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return await JobDetailsAsync(ctx, jobid);
            }
        }


        public async Task<List<IPrinterJobDetail>> JobDetailsAsync(PrintDB ctx, int jobid)
        {
            return new List<IPrinterJobDetail>(
                await ctx.PrinterJobDetails
                .Where(p => p.PrinterJobID == jobid)
                .ToListAsync()
            );
        }

        public IEnumerable<IPrinterJobDetail> JobDetailsByOrder(int orderID)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return JobDetailsByOrder(ctx, orderID);
            }
        }

        public IEnumerable<IPrinterJobDetail> JobDetailsByOrder(PrintDB ctx, int orderID)
        {
            var q = ctx.PrinterJobDetails
            .Join(ctx.PrinterJobs, ptjd => ptjd.PrinterJobID, ptj => ptj.ID, (pjd, pj) => new { PrinterJobDetail = pjd, PrinterJob = pj })
            .Where(w => w.PrinterJob.CompanyOrderID == orderID)
            .Select(s => s.PrinterJobDetail)
            .OrderBy(or => or.PrinterJobID)// Sort the details exactly as they were received
            .ThenBy(or => or.ProductDataID);

            return q.ToList();
        }


        private const string PrinterJobsQuery = @"
				select o.ID as OrderID, pj.CompanyID, pj.ProjectID, c.Name as CompanyName, o.OrderNumber, o.OrderDate, pj.DueDate, o.ProductionType,
					pj.ID as JobID, pj.ProductionLocationID, l.[Name] as LocationName, pj.ArticleID, a.[Name] as ArticleName,
					a.ArticleCode, m.ID as MaterialID, m.Name as MaterialName, lbl.ID as LabelID, lbl.Name as LabelName,
					pj.Quantity, pj.Printed, pj.Errors, pj.Extras, pj.Status, pj.AutoStart, pj.AssignedPrinter, p.Name as PrinterName,
					c2.[Name] as SendToCompany, (select top 1 ProductDataID from PrinterJobDetails where PrinterJobID = pj.ID) as ProductDataID, 
                    case when lbl.EncodeRFID = 1 then ISNULL((pj.Encoded * 100) / NULLIF(pj.Quantity,0), 0) else -1 end as Encoded, lbl.EncodeRFID
				from PrinterJobs pj
					join Companies c on pj.CompanyID = c.ID
					join CompanyOrders o on pj.CompanyOrderID = o.ID
					
					left outer join Companies c2 on o.SendToCompanyID = c2.ID
					left outer join Locations l on pj.ProductionLocationID = l.ID
					join Articles a on pj.ArticleID = a.ID
					join Labels lbl on a.LabelID = lbl.ID
					join Materials m on lbl.MaterialID = m.ID
					left outer join Printers p on pj.AssignedPrinter = p.ID
				where ((@prodType = 0 and o.ProductionType = 2 and pj.CompanyID = 1) or (@prodType = 0 and o.ProductionType = 1) or o.ProductionType = @prodType) and
				   (@companyid = 0 or pj.CompanyID = @companyid or o.SendToCompanyID = @companyid) and
				   (@locationid = 0 or pj.ProductionLocationID = @locationid) and
					(o.OrderStatus in (50, 3, 6))
				
		";


        private string PrinterJobsQueryWithFilters
        {
            get
            {
                return PrinterJobsQuery +
                @"
                    and (@status = 0 or (@status < 5 and (pj.Status <= @status or pj.Status = 7)) or (@status >= 5 and pj.Status = @status))
                    and pj.CreatedDate >= @date and o.OrderNumber like @ordernumber
                ";
            }
        }

        public IEnumerable<JobHeaderDTO> GetPendingJobs()
        {
            var userData = factory.GetInstance<IUserData>();
            ProductionType prodType;
            int companyid, locationid;
            GetFilters(userData, out prodType, out companyid, out locationid);
            using (var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<JobHeaderDTO>(PrinterJobsQuery, prodType, companyid, locationid);
            }
        }

        public IEnumerable<JobHeaderDTO> GetPendingJobs(PrinterJobFilter filter)
        {
            var userData = factory.GetInstance<IUserData>();
            ProductionType prodType;
            int companyid, locationid;
            GetFilters(userData, out prodType, out companyid, out locationid);
            using (var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<JobHeaderDTO>(PrinterJobsQueryWithFilters, prodType, filter.CompanyID, filter.LocationID, filter.Status, filter.Date, "%" + filter.OrderNumber + "%");
            }
        }


        public IEnumerable<JobHeaderDTO> GetPrinterJobs(int printerid)
        {
            var userData = factory.GetInstance<IUserData>();
            ProductionType prodType;
            int companyid, locationid;
            GetFilters(userData, out prodType, out companyid, out locationid);
            using (var conn = connManager.OpenWebLinkDB())
            {
                var result = conn.Select<JobHeaderDTO>(PrinterJobsQuery + @"
					and (pj.[Status] is null or pj.[Status] <= 4 or pj.[Status] = 7)
					and pj.AssignedPrinter = @printerid",
                    prodType, companyid, locationid, printerid);
                return result;
            }
        }


        public IEnumerable<PrintJobDetailDTO> GetJobDetails(int jobid, bool applySort)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetJobDetails(ctx, jobid, applySort);
            }
        }


        public IEnumerable<PrintJobDetailDTO> GetJobDetails(PrintDB ctx, int jobid, bool applySort)
        {
            string orderby;
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).AsNoTracking().FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");

            var company = ctx.Companies.Where(p => p.ID == job.CompanyID).AsNoTracking().FirstOrDefault();
            var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == job.ProjectID && c.Name == Catalog.ORDERDETAILS_CATALOG select c).FirstOrDefault();
            var varDataCatalog = (from c in ctx.Catalogs where c.ProjectID == job.ProjectID && c.Name == Catalog.VARIABLEDATA_CATALOG select c).FirstOrDefault();
            if (detailCatalog == null || varDataCatalog == null)
                throw new Exception($"Could not locate OrderDetails catalog for the Project.");

            var fields = JsonConvert.DeserializeObject<FieldDefinition[]>(detailCatalog.Definition).ToList();
            var vdFields = JsonConvert.DeserializeObject<FieldDefinition[]>(varDataCatalog.Definition).ToList();
            fields.AddRange(vdFields);

            if (!String.IsNullOrWhiteSpace(company.OrderSort) && applySort)
                orderby = "order by " + getOrderBy(fields.ToArray(), company.OrderSort);
            else
                orderby = "order by b.ID";

            using (var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);
                var variableData = dynamicDB.Select(detailCatalog.CatalogID, $@"
						select prod.*, det.ID as DetailID, b.ID as PrinterJobDetailID, b.Quantity, b.Printed, b.Encoded, b.Errors, b.Extras, b.PackCode
						from #TABLE det
							join [VariableData_{varDataCatalog.CatalogID}] prod on det.Product = prod.ID
							join [{connManager.WebLinkDB}].dbo.PrinterJobDetails b on b.ProductDataID = det.ID and b.PrinterJobID = @printerjob
						{orderby}", jobid);

                var result = new List<PrintJobDetailDTO>();
                JObject item;
                foreach (var row in variableData)
                {
                    item = row as JObject;
                    result.Add(new PrintJobDetailDTO()
                    {
                        ID = item.GetValue<int>("PrinterJobDetailID"),
                        PrinterJobID = jobid,
                        ProductDataID = item.GetValue<int>("DetailID"),
                        Quantity = item.GetValue<int>("Quantity"),
                        Printed = item.GetValue<int>("Printed"),
                        Errors = item.GetValue<int>("Errors"),
                        Extras = item.GetValue<int>("Extras"),
                        ProductData = item.ToString(),
                        PackCode = item.GetValue<string>("PackCode", string.Empty),
                    });
                }
                return result;
            }
        }


        private string getOrderBy(FieldDefinition[] fieldList, string sort)
        {
            StringBuilder sb = new StringBuilder(100);
            foreach (var f in fieldList)
                sb.Append(f.Name + ",");
            var fields = sb.ToString().ToLower();
            StringBuilder orderby = new StringBuilder(100);
            var sortspec = JsonConvert.DeserializeObject<SortSpec[]>(sort);
            foreach (var e in sortspec)
            {
                var field = e.field.Trim().ToLower();
                if (fields.Contains(field))
                {
                    if (e.dir.ToLower().Trim() == "desc")
                        orderby.Append($"{field} DESC, ");
                    else
                        orderby.Append($"{field}, ");
                }
            }
            if (orderby.Length > 0)
            {
                orderby.Remove(orderby.Length - 2, 2);
                return orderby.ToString();
            }
            else
            {
                return "b.ID";
            }
        }


        public void StartJob(int jobid)
        {
            var job = GetByID(jobid);
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            switch (job.Status)
            {
                case JobStatus.Executing:
                    return;
                case JobStatus.Paused:
                case JobStatus.Pending:
                case JobStatus.Error:
                case JobStatus.Printed:
                    if (!job.AssignedPrinter.HasValue)
                        throw new Exception("Job is not assigned to any printer, it cannot be started.");
                    var printer = printerManager.GetPrinter(job.AssignedPrinter.Value);
                    if (printer != null)
                    {
                        //if (printer.IsPrinting() && printer.CurrentJob.ID != jobid)
                        //	printer.StopJob(printer.CurrentJob);
                        //UpdateJobState(jobid, JobStatus.Executing);
                        printer.StartJob(job);
                    }
                    else throw new Exception("Cannot start job because the printer is not online");
                    break;
                case JobStatus.Completed:
                    throw new Exception("Cannot start job because it has already been completed");
                case JobStatus.Cancelled:
                default:
                    throw new Exception("Cannot start job because it has been cancelled");
            }
        }


        public void PauseJob(int jobid)
        {
            var job = GetByID(jobid);
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            switch (job.Status)
            {
                case JobStatus.Paused:
                case JobStatus.Pending:
                case JobStatus.Error:
                case JobStatus.Executing:
                    if (job.AssignedPrinter.HasValue)
                    {
                        var printer = printerManager.GetPrinter(job.AssignedPrinter.Value);
                        if (printer != null)
                            printer.PauseJob(job);
                    }
                    break;
                case JobStatus.Printed:
                    throw new Exception("Cannot pause job because it is not running");
                case JobStatus.Completed:
                    throw new Exception("Cannot pause job because it has already been completed");
                case JobStatus.Cancelled:
                default:
                    throw new Exception("Cannot pause job because it has been cancelled");
            }
        }


        public void CancelJob(int jobid)
        {
            var job = GetByID(jobid);
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            if (job.AssignedPrinter.HasValue)
            {
                var printer = printerManager.GetPrinter(job.AssignedPrinter.Value);
                if (printer != null && printer.CurrentJobID == jobid)
                    printer.PauseJob(job);
            }
            UpdateJobState(jobid, JobStatus.Cancelled);
        }


        public void ActivateJob(int jobid)
        {
            var job = GetByID(jobid);
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            UpdateJobState(jobid, JobStatus.Pending);
        }


        public void ChangePrinter(int jobid, int printerid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                ChangePrinter(ctx, jobid, printerid);
            }
        }


        public void ChangePrinter(PrintDB ctx, int jobid, int printerid)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault(); // Ensures that the user can see the requested job
            var printer = printerRepo.GetByID(printerid);   // Ensures that the user can see the requested printer
            if (job.AssignedPrinter.HasValue)
            {
                var actualPrinter = printerManager.GetPrinter(job.AssignedPrinter.Value);
                if (actualPrinter != null && actualPrinter.CurrentJobID == job.ID)
                {
                    actualPrinter.PauseJob(job);
                }
            }
            job.AssignedPrinter = printerid;
            job.Status = JobStatus.Paused;
            ctx.PrinterJobs.Update(job);
            ctx.SaveChanges();
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobStatusUpdate, job));
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.PrinterChanged, job));
        }


        public IPrinterJobDetail AddExtras(int jobid, int detailid, int quantity)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return AddExtras(ctx, jobid, detailid, quantity);
            }
        }


        public IPrinterJobDetail AddExtras(PrintDB ctx, int jobid, int detailid, int quantity)
        {
            var job = All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            var detail = ctx.PrinterJobDetails.Where(p => p.PrinterJobID == jobid && p.ID == detailid).FirstOrDefault();
            if (detail == null)
                throw new Exception($"Job Detail {detailid} does not exist.");
            detail.Extras += quantity;
            job.Extras += quantity;
            if ((int)job.Status >= (int)JobStatus.Completed)
            {
                job.Status = JobStatus.Pending;
                job.AutoStart = false;
                events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobStatusUpdate, job));
            }
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.ExtrasAdded, detail));
            ctx.SaveChanges();
            return detail;
        }


        public IPrinterJob GetNextPrinterJob(int printerid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetNextPrinterJob(ctx, printerid);
            }
        }


        public IPrinterJob GetNextPrinterJob(PrintDB ctx, int printerid)
        {
            var job = ctx.PrinterJobs.Where(p => p.AssignedPrinter == printerid && p.Status == JobStatus.Executing).AsNoTracking().FirstOrDefault();
            if (job == null)
                job = ctx.PrinterJobs.Where(p => p.AssignedPrinter == printerid && p.AutoStart == true && (p.Status <= JobStatus.Executing && p.Status != JobStatus.Paused)).AsNoTracking().FirstOrDefault();
            return job;
        }


        public void SetDueDate(int jobid, DateTime date)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                SetDueDate(ctx, jobid, date);
            }
        }


        public void SetDueDate(PrintDB ctx, int jobid, DateTime date)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            job.DueDate = date;
            ctx.SaveChanges();
        }


        public void SetJobLocation(int jobid, int locationid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                SetJobLocation(ctx, jobid, locationid);
            }
        }


        public void SetJobLocation(PrintDB ctx, int jobid, int locationid)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            if (job.ProductionLocationID != locationid)
            {
                job.ProductionLocationID = locationid;
                job.AssignedPrinter = null;
                ctx.SaveChanges();
                events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.LocationChanged, job));
            }
        }


        public void SetJobPrinter(int jobid, int locationid, int printerid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                SetJobPrinter(ctx, jobid, locationid, printerid);
            }
        }


        public void SetJobPrinter(PrintDB ctx, int jobid, int locationid, int printerid)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");

            var location = ctx.Locations.Where(l => l.ID == locationid).AsNoTracking().FirstOrDefault();
            if (location == null)
                throw new Exception($"Location {locationid} could not be found.");

            var printer = ctx.Printers.Where(p => p.ID == printerid && p.LocationID == locationid).AsNoTracking().FirstOrDefault();
            if (printer == null)
                throw new Exception($"Printer {printerid} could not be found or does not belong in location {locationid}.");

            job.AssignedPrinter = printerid;
            ctx.SaveChanges();
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.PrinterChanged, job));
        }


        public void UpdateJobState(int jobid, JobStatus status)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateJobState(ctx, jobid, status);
            }
        }


        public void UpdateJobState(PrintDB ctx, int jobid, JobStatus status)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            job.Status = status;
            if ((int)job.Status >= (int)JobStatus.Completed)
                job.CompletedDate = DateTime.Now;
            ctx.SaveChanges();
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobStatusUpdate, job));
        }


        public void ResetProgress(int jobid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                ResetProgress(ctx, jobid);
            }
        }


        public void ResetProgress(PrintDB ctx, int jobid)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            if (job.Status == JobStatus.Cancelled || job.Status == JobStatus.Completed)
                job.Status = JobStatus.Pending;
            job.Printed = 0;
            job.Errors = 0;
            job.Extras = 0;
            job.Encoded = 0;
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobStatusUpdate, job));
            var details = ctx.PrinterJobDetails.Where(p => p.PrinterJobID == jobid);
            foreach (var detail in details)
            {
                detail.Printed = 0;
                detail.Errors = 0;
                detail.Extras = 0;
                detail.Encoded = 0;
                events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobDetailUpdate, detail));
            }
            ctx.SaveChanges();
        }


        public void SetDetailProgress(int detailid, int progress)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                SetDetailProgress(ctx, detailid, progress);
            }
        }


        public void SetDetailProgress(PrintDB ctx, int detailid, int progress)
        {
            var detail = ctx.PrinterJobDetails.Where(p => p.ID == detailid).FirstOrDefault();
            if (detail == null)
                throw new Exception($"JobDetail {detailid} does not exist.");
            var job = (PrinterJob)All(ctx).Where(p => p.ID == detail.PrinterJobID).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {detail.PrinterJobID} does not exist or user does not have permissions to access it.");
            detail.Printed = progress;
            detail.Errors = 0;
            detail.Extras = 0;
            ctx.SaveChanges();
            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobDetailUpdate, detail));
        }


        public void UpdateDetailProgress(PLUnitProgressChangeEvent e)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateDetailProgress(ctx, e);
            }
        }

        public void UpdateDetailProgress(PrintDB ctx, PLUnitProgressChangeEvent e)
        {
            var detail = ctx.PrinterJobDetails
                            .Where(p => p.ID == e.PrintJobDetailID)
                            .FirstOrDefault();

            if(detail == null)
                throw new Exception($"JobDetail {e.PrintJobDetailID} does not exist.");

            var job = (PrinterJob)All(ctx)
                            .Where(p => p.ID == detail.PrinterJobID)
                            .FirstOrDefault();

            if(job == null)
                throw new Exception($"Job {detail.PrinterJobID} does not exist or user does not have permissions to access it.");

            detail.Printed = e.Progress;
            detail.Encoded = e.EncodeProgress;
            detail.TransferProgress = e.TransferProgress;
            detail.ExportProgress = e.ExportProgress;
            detail.VerifyProgress = e.VerifyProgress;
            detail.Extras = e.Extras;
            detail.LastEncodeDate = e.LastEncodeDate;
            detail.LastPrintDate = e.LastPrintDate;
            detail.LastVerifyDate = e.LastVerifyDate;

            detail.UpdatedDate = DateTime.UtcNow;

            ctx.SaveChanges();

            events.Send(new PrinterJobEvent(job.CompanyID, PrinterJobEventType.JobDetailUpdate, detail));
        }


        public void UpdateEncoded(int jobid, int encoded)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateEncoded(ctx, jobid, encoded);
            }
        }


        public void UpdateEncoded(PrintDB ctx, int jobid, int encoded)
        {
            var job = (PrinterJob)All(ctx).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            job.Encoded = encoded;
            ctx.SaveChanges();
        }

        public void UpdateArticle(int jobid, int articleid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateArticle(ctx, jobid, articleid);
            }
        }


        public void UpdateArticle(PrintDB ctx, int jobid, int articleid)
        {
            var job = (PrinterJob)All(ctx, true).Where(p => p.ID == jobid).FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");
            job.ArticleID = articleid;
            ctx.SaveChanges();
        }


        public List<IPrinterJob> CreateFromOrder(int orderid, int? printerid, bool autoStart = false, int? articleID = null)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return CreateFromOrder(ctx, orderid, printerid, autoStart, articleID);
            }
        }


        public List<IPrinterJob> CreateFromOrder(PrintDB ctx, int orderid, int? printerid, bool autoStart = false, int? articleID = null)
        {
            var userData = factory.GetInstance<IUserData>();
            int companyidFilter;
            GetFilters(userData, out _, out companyidFilter, out _);
            int? locationid = null;
            if (printerid != null)
            {
                var printer = ctx.Printers.Include(p => p.Location).Where(p => p.ID == printerid && (companyidFilter == 0 || p.Location.CompanyID == companyidFilter)).AsNoTracking().FirstOrDefault();
                if (printer == null) throw new Exception($"Printer {printerid} could not be found.");
                locationid = printer.LocationID;
            }
            var result = new List<IPrinterJob>();
            var orderRepo = factory.GetInstance<IOrderRepository>();
            var order = orderRepo.GetOrderProductionDetail(ctx, orderid);



            PrinterJob job = null;
            string lastArticleCode = null;
            foreach (var detail in order.Details)
            {
                if (lastArticleCode != detail.ArticleCode)
                {
                    lastArticleCode = detail.ArticleCode;
                    job = new PrinterJob();
                    job.CompanyID = order.CompanyID;
                    job.CompanyOrderID = order.OrderID;
                    job.ProjectID = order.ProjectID;
                    job.ProductionLocationID = locationid;
                    job.AssignedPrinter = printerid;
                    job.ArticleID = articleID.HasValue ? articleID.Value : detail.ArticleID;
                    job.Quantity = 0;
                    job.Printed = 0;
                    job.Errors = 0;
                    job.Extras = 0;
                    job.DueDate = DateTime.Now.AddDays(order.SLADays ?? 7);
                    job.Status = JobStatus.Pending;
                    job.AutoStart = autoStart;
                    job.CreatedDate = DateTime.Now;
                    job.CompletedDate = null;
                    ctx.PrinterJobs.Add(job);
                    ctx.SaveChanges();
                    result.Add(job);
                }
                PrinterJobDetail jobdetail = new PrinterJobDetail();
                jobdetail.PrinterJobID = job.ID;
                jobdetail.ProductDataID = detail.DetailID; // ???: OrderDetail or VariableData?
                                                           //jobdetail.ProductDataID = detail.Product;
                jobdetail.Quantity = detail.Quantity;
                jobdetail.QuantityRequested = detail.Quantity;
                jobdetail.PackCode = detail.PackCode;
                job.Quantity += jobdetail.Quantity;

                ctx.PrinterJobDetails.Add(jobdetail);
            }
            ctx.SaveChanges();
            return result;
        }


        public int? GetLocationBy(int? printerid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetLocationBy(ctx, printerid);
            }
        }


        public int? GetLocationBy(PrintDB ctx, int? printerid)
        {
            int companyidFilter;
            var userData = factory.GetInstance<IUserData>();

            GetFilters(userData, out _, out companyidFilter, out _);

            int? locationid = null;

            if (printerid != null)
            {
                var printer = ctx.Printers.Include(p => p.Location).Where(p => p.ID == printerid && (companyidFilter == 0 || p.Location.CompanyID == companyidFilter)).AsNoTracking().FirstOrDefault();
                if (printer == null) throw new Exception($"Printer {printerid} could not be found.");
                locationid = printer.LocationID;
            }

            return locationid;
        }


        public IPrinterJob CreateArticleOrder(IOrder order, IEnumerable<OrderProductionDetailRow> details, int? locationid, int? SLADays, int? printerid, bool autoStart = false)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return CreateArticleOrder(ctx, order, details, locationid, SLADays, printerid, autoStart);
            }
        }


        public IPrinterJob CreateArticleOrder(PrintDB ctx, IOrder order, IEnumerable<OrderProductionDetailRow> details, int? locationid, int? SLADays, int? printerid, bool autoStart = false)
        {
            if (details.Count() < 1)
                return null;

            PrinterJob job = new PrinterJob();
            var jobDetails = new List<PrinterJobDetail>();

            job.CompanyID = order.CompanyID;
            job.CompanyOrderID = order.ID;
            job.ProjectID = order.ProjectID;
            job.ProductionLocationID = locationid;
            job.AssignedPrinter = printerid;
            // se supone que todos los detalles que estoy recibiendo son del mismo codigo
            job.ArticleID = details.First().ArticleID;
            job.Quantity = 0;
            job.Printed = 0;
            job.Errors = 0;
            job.Extras = 0;
            job.DueDate = DateTime.Now.AddDays(SLADays ?? 7);
            job.Status = JobStatus.Pending;
            job.AutoStart = autoStart;
            job.CreatedDate = DateTime.Now;
            job.CompletedDate = null;

            foreach (var detail in details)
            {
                PrinterJobDetail jobdetail = new PrinterJobDetail();
                //jobdetail.PrinterJobID = job.ID;
                jobdetail.ProductDataID = detail.DetailID; // is VariableDataID
                jobdetail.Quantity = detail.Quantity;
                jobdetail.QuantityRequested = detail.Quantity;
                jobdetail.PackCode = detail.PackCode;
                job.Quantity += jobdetail.Quantity;

                jobDetails.Add(jobdetail);
            }

            ctx.PrinterJobs.Add(job);
            ctx.SaveChanges();

            jobDetails.ForEach(d => d.PrinterJobID = job.ID);
            ctx.PrinterJobDetails.AddRange(jobDetails);
            ctx.SaveChanges();

            return job;
        }


        public int GetCompanyFromProject(int projectid)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetCompanyFromProject(ctx, projectid);
            }
        }


        public int GetCompanyFromProject(PrintDB ctx, int projectid)
        {
            return (
                from p in ctx.Projects
                join b in ctx.Brands on p.BrandID equals b.ID
                where p.ID == projectid
                select b.CompanyID
            )
            .Single();
        }


        public void UpdateQuantiesByGroup(List<OrderGroupQuantitiesDTO> updates)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateQuantiesByGroup(ctx, updates);
            }
        }


        public void UpdateQuantiesByGroup(PrintDB ctx, List<OrderGroupQuantitiesDTO> updates)
        {
            //var db = sp.GetRequiredService<IDBConnectionManager>();
            string query = string.Empty;
            var quantitiesStates = new List<QuantityState>();
            var orderIDs = new List<int>();

            updates.ForEach(e =>
            {
                // if items are selected, they not have an assigned value at this point
                if (e.Quantities != null)
                {
                    quantitiesStates.AddRange(e.Quantities);
                    orderIDs.AddRange(e.Quantities.Select(s => s.OrderID).Distinct());
                }
            });

            foreach (var q in quantitiesStates.Where(w => w.PrinterJobDetailID > 0))
            {
                var pjd = ctx.PrinterJobDetails.Where(w => w.ID.Equals(q.PrinterJobDetailID)).First();
                pjd.Quantity = q.Value;
                pjd.Extras = 0;

                ctx.Entry(pjd).State = EntityState.Modified;

                // If entity event updates is required, use GenericRepository.Update methdos
            }

            ctx.SaveChanges();

            RefreshOrderQuantityValue(ctx, orderIDs.Distinct().ToList());
        }

        public void RefreshOrderQuantityValue(IEnumerable<int> orders)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
                RefreshOrderQuantityValue(ctx, orders);
        }

        public void RefreshOrderQuantityValue(PrintDB ctx, IEnumerable<int> orders)
        {
            if (orders.Count() < 1) return;

            var includeOrders = string.Join(',', orders.ToArray());

            var q1 = $@"  
					UPDATE j
					  SET Quantity = gpjd.TotalQuantity, Extras = TotalExtras
					  FROM [dbo].[PrinterJobs] j
					  INNER JOIN [dbo].[CompanyOrders] o ON j.CompanyOrderID = o.ID
					  INNER JOIN(
						  SELECT SUM(d.Quantity) AS TotalQuantity, SUM(d.Extras) AS TotalExtras, d.PrinterJobID
						  FROM [dbo].[PrinterJobDetails] d
					      INNER JOIN [dbo].[PrinterJobs] pj2 ON d.PrinterJobID = pj2.ID
						  WHERE pj2.CompanyOrderID IN ({includeOrders})
						  GROUP BY d.PrinterJobID

					  ) AS gpjd ON gpjd.PrinterJobID = j.ID
					WHERE j.CompanyOrderID IN ({includeOrders})
					";

            var q2 = $@"  
					UPDATE o
					  SET Quantity = pj.Quantity
					  FROM [dbo].[CompanyOrders] o
                      INNER JOIN [dbo].[PrinterJobs] pj ON o.ID = pj.CompanyOrderID
					WHERE o.ID IN({includeOrders})
					";
            // NOTE: There is no danger of SQL injection because we are not concatenating any user supplied data
#pragma warning disable
            ctx.Database.ExecuteSqlCommand(q1);
            ctx.Database.ExecuteSqlCommand(q2);
#pragma warning restore
        }


        public IPrinterJob AddExtraJob(IPrinterJob data)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return AddExtraJob(ctx, data);
            }
        }


        public IPrinterJob AddExtraJob(PrintDB ctx, IPrinterJob data)
        {
            ctx.PrinterJobs.Add((PrinterJob)data);
            ctx.SaveChanges();
            return data;
        }


        public IPrinterJobDetail AddExtraDetailToJob(IPrinterJobDetail data)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return AddExtraDetailToJob(ctx, data);
            }
        }


        public IPrinterJobDetail AddExtraDetailToJob(PrintDB ctx, IPrinterJobDetail data)
        {
            ctx.PrinterJobDetails.Add((PrinterJobDetail)data);
            ctx.SaveChanges();
            return data;
        }




        public IEnumerable<IPrinterJob> GetAllJobsByOrderId(int orderid, bool byPassChecks = false)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetAllJobsByOrderId(ctx, orderid, byPassChecks);
            }
        }


        public IEnumerable<IPrinterJob> GetAllJobsByOrderId(PrintDB ctx, int orderid, bool byPassChecks = false)
        {
            return ctx.PrinterJobs
                .Where(p => p.CompanyOrderID == orderid)
                .AsNoTracking()
                .ToList();
        }



        public IEnumerable<PrintJobDetailDTO> GetAllJobDetails(int jobid, bool applySort)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                return GetAllJobDetails(ctx, jobid, applySort);
            }
        }


        public IEnumerable<PrintJobDetailDTO> GetAllJobDetails(PrintDB ctx, int jobid, bool applySort)
        {
            string orderby;
            var job = ctx.PrinterJobs.Where(p => p.ID == jobid).AsNoTracking().FirstOrDefault();
            if (job == null)
                throw new Exception($"Job {jobid} does not exist or user does not have permissions to access it.");

            var company = ctx.Companies.Where(p => p.ID == job.CompanyID).AsNoTracking().FirstOrDefault();
            var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == job.ProjectID && c.Name == Catalog.ORDERDETAILS_CATALOG select c).FirstOrDefault();
            var varDataCatalog = (from c in ctx.Catalogs where c.ProjectID == job.ProjectID && c.Name == Catalog.VARIABLEDATA_CATALOG select c).FirstOrDefault();
            if (detailCatalog == null || varDataCatalog == null)
                throw new Exception($"Could not locate OrderDetails catalog for the Project.");

            var fields = JsonConvert.DeserializeObject<FieldDefinition[]>(detailCatalog.Definition);
            if (!String.IsNullOrWhiteSpace(company.OrderSort) && applySort)
                orderby = "order by " + getOrderBy(fields, company.OrderSort);
            else
                orderby = "order by b.ID";

            using (var dynamicDB = factory.GetInstance<DynamicDB>())
            {
                dynamicDB.Open(config["Databases.CatalogDB.ConnStr"]);
                var variableData = dynamicDB.Select(detailCatalog.CatalogID, $@"
						select prod.*, det.ID as DetailID, b.ID as PrinterJobDetailID, b.Quantity, b.Printed, b.Encoded, b.Errors, b.Extras, b.PackCode
						from #TABLE det
							join [VariableData_{varDataCatalog.CatalogID}] prod on det.Product = prod.ID
							join [{connManager.WebLinkDB}].dbo.PrinterJobDetails b on b.ProductDataID = det.ID and b.PrinterJobID = @printerjob
						{orderby}", jobid);

                var result = new List<PrintJobDetailDTO>();
                JObject item;
                foreach (var row in variableData)
                {
                    item = row as JObject;
                    result.Add(new PrintJobDetailDTO()
                    {
                        ID = item.GetValue<int>("PrinterJobDetailID"),
                        PrinterJobID = jobid,
                        ProductDataID = item.GetValue<int>("DetailID"),
                        Quantity = item.GetValue<int>("Quantity"),
                        Printed = item.GetValue<int>("Printed"),
                        Errors = item.GetValue<int>("Errors"),
                        Extras = item.GetValue<int>("Extras"),
                        ProductData = item.ToString(),
                        PackCode = item.GetValue<string>("PackCode", string.Empty),
                    });
                }
                return result;
            }
        }
    }




    public class SortSpec
    {
        public string field { get; set; }
        public string dir { get; set; }
    }





}
