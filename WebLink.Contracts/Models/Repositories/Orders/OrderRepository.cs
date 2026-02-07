using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.LabelService;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;

namespace WebLink.Contracts.Models
{
    public partial class OrderRepository : GenericRepository<IOrder, Order>, IOrderRepository
    {
        private IDBConnectionManager connManager;
        private IFileStoreManager storeManager;
        private IRemoteFileStore store;
        private ILogService log;
        private IOrderDataRepository dataRepo;
        private ICatalogDataRepository catalogDataRepo;
        private IOrderComparerService orderCompareService;
        private IVariableDataRepository variableDataRepo;
        private IBLabelServiceClient labelService;
        private IPluginManager<IOrderProcessingPlugin> pluginManager;
        private string documentServiceUrl;
        private ICompanyRepository companyRepo;
        private IAutomatedProcessManager apm;
        private ICatalogRepository catalogRepo;

        public OrderRepository(
            IFactory factory,
            IDBConnectionManager connManager,
            IAppConfig config,
            IFileStoreManager storeManager,
            ILogService log,
            IOrderDataRepository dataRepo,
            ICatalogDataRepository catalogDataRepo,
            IOrderComparerService orderCompareService,
            IVariableDataRepository variableDataRepo,
            IBLabelServiceClient labelService,
            IPluginManager<IOrderProcessingPlugin> pluginManager,
            ICompanyRepository companyRepo,
            IAutomatedProcessManager apm,
            ICatalogRepository catalogRepo
            )
            : base(factory, (ctx) => ctx.CompanyOrders)
        {
            this.connManager = connManager;
            this.storeManager = storeManager;
            this.store = storeManager.OpenStore("OrderStore");
            documentServiceUrl = config["WebLink:DocumentService"];
            this.log = log;
            this.dataRepo = dataRepo;
            this.catalogDataRepo = catalogDataRepo;
            this.orderCompareService = orderCompareService;
            this.variableDataRepo = variableDataRepo;
            this.labelService = labelService;
            this.labelService.Url = config["WebLink:LabelService"];
            this.pluginManager = pluginManager;
            this.companyRepo = companyRepo;
            this.apm = apm;
            this.catalogRepo = catalogRepo;
        }


        protected override string TableName { get => "CompanyOrders"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Order actual, IOrder data)
        {
            actual.CompanyID = data.CompanyID;
            actual.ProjectID = data.ProjectID;
            actual.Source = data.Source;
            actual.OrderNumber = data.OrderNumber;
            actual.MDOrderNumber = data.MDOrderNumber;
            actual.OrderStatus = data.OrderStatus;
            actual.ConfirmedByMD = data.ConfirmedByMD;
            actual.PreviewGenerated = data.PreviewGenerated;
            actual.PrintPackageGenerated = data.PrintPackageGenerated;
            actual.ProductionType = data.ProductionType;
            actual.Quantity = data.Quantity;
            actual.BillTo = data.BillTo;
            actual.BillToCompanyID = data.BillToCompanyID;
            actual.SendTo = data.SendTo;
            actual.SendToCompanyID = data.SendToCompanyID;
            actual.LocationID = data.LocationID;
            //actual.SendToLocationID = data.SendToLocationID;
            actual.AssignedPrinterID = data.AssignedPrinterID;
            actual.SendToAddressID = data.SendToAddressID;
            actual.ProjectPrefix = data.ProjectPrefix;
            actual.IsBillable = data.IsBillable;
            actual.IsBilled = data.IsBilled;
            actual.IsInConflict = data.IsInConflict;
            actual.IsStopped = data.IsStopped;

            // TODO: Does this required a Role ?
            actual.SyncWithSage = data.SyncWithSage;
            actual.SageReference = data.SageReference;
            actual.InvoiceStatus = data.InvoiceStatus;
            actual.DeliveryStatus = data.DeliveryStatus;
            actual.CreditStatus = data.CreditStatus;
            actual.SageStatus = data.SageStatus;
            actual.DueDate = data.DueDate;
            actual.HasOrderWorkflow = data.HasOrderWorkflow;
            actual.ItemID = data.ItemID;
        }


        //protected override void AuthorizeOperation(PrintDB ctx, IUserData userData, Order data)
        //{
        //    if (userData.IsIDT || userData.UserName == "SYSTEM") return;  // Do not restrict access to IDT Users/SYSTEM

        //    var isProvider = IsProvider(ctx, data.CompanyID, userData.SelectedCompanyID);

        //    if(data.CompanyID != userData.SelectedCompanyID && !isProvider)
        //        throw new Exception($"Not authorized to get OrderID [{data.ID}] by user [{userData.UserName}]");
        //}


        protected override void AfterInsert(PrintDB ctx, IUserData userData, Order actual)
        {
            var project = ctx.Projects
                .Where(p => p.ID == actual.ProjectID)
                .Include(p => p.Brand)
                .AsNoTracking()
                .FirstOrDefault();

            // Process order inserted plugin if poject don't has Order Workflow Configuration
            if(project != null && !String.IsNullOrWhiteSpace(project.OrderPlugin) && project.OrderWorkflowConfigID == null)
            {
                using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    using(var plugin = pluginManager.GetInstanceByName(project.OrderPlugin))
                    {
                        var orderData = new OrderPluginData()
                        {
                            CompanyID = project.Brand.CompanyID,
                            BrandID = project.BrandID,
                            ProjectID = actual.ProjectID,
                            OrderID = actual.ID,
                            OrderGroupID = actual.OrderGroupID,
                            OrderNumber = actual.OrderNumber
                        };
                        plugin.OrderInserted(orderData);
                    }
                }
            }
        }


        protected override void AfterUpdate(PrintDB ctx, IUserData userData, Order actual)
        {
            var project = ctx.Projects
                .Where(p => p.ID == actual.ProjectID)
                .Include(p => p.Brand)
                .AsNoTracking()
                .FirstOrDefault();

            // Process order plugins if poject don't has Order Workflow Configuration
            if(project != null && !String.IsNullOrWhiteSpace(project.OrderPlugin) && project.OrderWorkflowConfigID == null)
            {
                using(var suppressedScope = new TransactionScope(TransactionScopeOption.Suppress))
                {
                    using(var plugin = pluginManager.GetInstanceByName(project.OrderPlugin))
                    {
                        InvokePluginBasedOfStatus(plugin, actual, project);
                    }
                }
            }
        }


        private void InvokePluginBasedOfStatus(IOrderProcessingPlugin plugin, Order order, Project project)
        {
            var orderData = new OrderPluginData()
            {
                CompanyID = project.Brand.CompanyID,
                BrandID = project.BrandID,
                ProjectID = order.ProjectID,
                OrderID = order.ID,
                OrderGroupID = order.OrderGroupID,
                OrderNumber = order.OrderNumber
            };
            switch(order.OrderStatus)
            {
                case OrderStatus.Received:
                    plugin.OrderReceived(orderData);
                    break;
                case OrderStatus.Processed:
                    plugin.OrderProcessed(orderData);
                    break;
                case OrderStatus.Completed:
                    plugin.OrderCompleted(orderData);
                    break;
                case OrderStatus.Cancelled:
                    plugin.OrderCancelled(orderData);
                    break;
            }
        }


        public void SetOrderFile(int orderid, Guid fileGUID)
        {
            var srcFile = storeManager.GetFile(fileGUID);
            var file = store.GetOrCreateFile(orderid, srcFile.FileName);
            using(var content = srcFile.GetContentAsStream())
                file.SetContent(content);
        }


        public void SetOrderFile(int orderid, Stream fileContent)
        {
            var fileTempService = factory.GetInstance<ITempFileService>();

            var fileName = fileTempService.GetTempFileName(true, ".json");

            var fileData = store.GetOrCreateFile(orderid, fileName);

            fileData.SetContent(fileContent);
        }

        public void SetOrderIdImageFile(int orderid, Stream filedata)
        {
            var fileTempService = factory.GetInstance<ITempFileService>();
            var fileName = fileTempService.GetTempFileName(true, ".jpg");
            var fileData = store.CreateFile(orderid, fileName);
            fileData.SetContent(filedata);
        }

        public IFileData GetOrderFile(int orderid)
        {
            IFileData file;
            if(store.TryGetFile(orderid, out file))
                return file;
            else
                return null;
        }


        public void SetOrderAttachment(int orderid, string attachmentCategory, string attachmentFilePath)
        {
            if(!store.TryGetFile(orderid, out var file))
            {
                file = store.GetOrCreateFile(orderid, "NotAvailable.txt");
                file.SetContent(Encoding.UTF8.GetBytes("This order was not created from a file."));
            }
            var fileName = Path.GetFileName(attachmentFilePath);
            var attachment = file.GetAttachmentCategory(attachmentCategory).CreateAttachment(fileName);
            attachment.SetContent(attachmentFilePath);
        }


        public IAttachmentData GetOrderAttachment(int orderid, string attachmentCategory, string attachmentName)
        {
            log.LogMessage($"GetOrderAttachment(OrderID:{orderid}, Category:{attachmentCategory}, FileName:{attachmentName})");
            IFileData file;
            if(store.TryGetFile(orderid, out file))
            {
                var attachments = file.GetAttachmentCategory(attachmentCategory);
                log.LogMessage($"GetOrderAttachment got attachment count: {attachments.Count}");
                if(attachments.TryGetAttachment(attachmentName, out var attachment))
                {
                    log.LogMessage($"GetOrderAttachment - TryGetAttachment returned attachment: {attachment.FileGUID}");
                    return attachment;
                }
                else
                {
                    log.LogMessage($"GetOrderAttachment - Could not find any attachment named '{attachmentName}'...");
                    return null;
                }
            }
            else
            {
                log.LogMessage($"GetOrderAttachment - Could not find any file with FileID: {orderid}");
                return null;
            }
        }


        public string PackMultiplePrintPackages(int factoryid, IEnumerable<int> orderids)
        {
            Location l;
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                l = ctx.Locations.Where(p => p.ID == factoryid).AsNoTracking().Single();
            }

            var d = DateTime.Now;
            var tempFileService = factory.GetInstance<ITempFileService>();
            string baseFileName = tempFileService.SanitizeFileName($"Orders-{l.FactoryCode}-{d.Year}-{d.Month}-{d.Day}.zip");
            var fileName = tempFileService.GetTempFileName(baseFileName, true);

            using(FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                fs.SetLength(0L);
                using(ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    foreach(var id in orderids)
                    {
                        IAttachmentData file = GetOrderAttachment(id, "PrintPackage", "Order-" + id + ".zip");
                        if(file != null)
                        {
                            using(var srcStream = file.GetContentAsStream())
                            {
                                var entry = archive.CreateEntry(file.FileName);
                                using(var dstStream = entry.Open())
                                {
                                    srcStream.CopyTo(dstStream, 4096);
                                }
                            }
                        }
                    }
                }
            }

            return fileName;
        }


        public string PackMultipleOrdersValidationPreview(IEnumerable<int> orderids)
        {

            IOrderDocumentService docSrv = factory.GetInstance<IOrderDocumentService>();
            var orderdetails = GetOrderArticles(new OrderArticlesFilter { OrderID = orderids.ToList() }, ProductDetails.None);


            var d = DateTime.Now;
            var tempFileService = factory.GetInstance<ITempFileService>();
            string baseFileName = tempFileService.SanitizeFileName($"Order-{orderdetails.First().OrderNumber}-{d.Year}-{d.Month}-{d.Day}.zip");
            var fileName = tempFileService.GetTempFileName(baseFileName, true);



            using(FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                fs.SetLength(0L);
                using(ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    foreach(var id in orderids)
                    {
                        var tmp = docSrv.CreatePreviewDocument(id).GetAwaiter().GetResult();
                        var file = docSrv.GetPreviewDocument(id);
                        var orderinfo = orderdetails.Where(x => x.OrderID == id).First();

                        string pdfbasename = tempFileService.SanitizeFileName(orderinfo.OrderNumber + "-" + orderinfo.ArticleCode + ".pdf");
                        //var pdfFileName = tempFileService.GetTempFileName(pdfbasename, true);

                        if(file != null)
                        {
                            using(var srcStream = file.GetContentAsStream())
                            {
                                var entry = archive.CreateEntry(pdfbasename);
                                using(var dstStream = entry.Open())
                                {
                                    srcStream.CopyTo(dstStream, 4096);
                                }
                            }
                        }
                    }
                }
            }

            return fileName;
        }


        public List<IOrder> GetOrdersByStatus(OrderStatus status)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrdersByStatus(ctx, status);
            }
        }


        public List<IOrder> GetOrdersByStatus(PrintDB ctx, OrderStatus status)
        {
            var userData = factory.GetInstance<IUserData>();

            ProductionType prodType = ProductionType.CustomerLocation;

            if(userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager) || userData.SelectedCompanyID == 1)
                prodType = ProductionType.IDTLocation;

            return new List<IOrder>(All(ctx).Where(p =>
                    p.ProductionType == prodType &&
                    p.OrderStatus == status));
        }


        public List<IOrder> GetOrdersWithoutMDConfirmation()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrdersWithoutMDConfirmation(ctx);
            }
        }


        public List<IOrder> GetOrdersWithoutMDConfirmation(PrintDB ctx)
        {
            return new List<IOrder>(
                ctx.CompanyOrders.Where(p =>
                    p.ProductionType == ProductionType.IDTLocation &&
                    p.ConfirmedByMD == false)
                .AsNoTracking());
        }


        public void SetOrderMDConfirmation(int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                SetOrderMDConfirmation(ctx, orderid);
            }
        }


        public void SetOrderMDConfirmation(PrintDB ctx, int orderid)
        {
            var order = ctx.CompanyOrders.Where(p => p.ID == orderid).SingleOrDefault();
            if(order != null)
            {
                order.ConfirmedByMD = true;
                ctx.SaveChanges();
            }
        }


        public List<CompanyOrderDTO> GetOrderReport(OrderReportFilter filter)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderReport(ctx, filter);
            }
        }


        public List<CompanyOrderDTO> GetOrderReport(PrintDB ctx, OrderReportFilter filter)
        {
            var userData = factory.GetInstance<IUserData>();

            ProductionType prodType = (ProductionType)filter.ProductionType;
            bool orderNumberEmpty = String.IsNullOrWhiteSpace(filter.OrderNumber);


            int maxDaysRange = 90;
            DateTime TopDate = filter.OrderDate.AddDays(maxDaysRange);

            // if IDT maxDayRange = 180 ?

            var qry = from o in ctx.CompanyOrders
                      join p in ctx.Projects on o.ProjectID equals p.ID
                      join b in ctx.Brands on p.BrandID equals b.ID
                      join c in ctx.Companies on b.CompanyID equals c.ID
                      join w in ctx.Companies on o.SendToCompanyID equals w.ID
                      join inv in ctx.Companies on o.BillToCompanyID equals inv.ID // invoice
                      join udpprop in ctx.OrderUpdateProperties on o.ID equals udpprop.OrderID // is active order

                      // left join with locations
                      join locmap in ctx.Locations on o.LocationID equals locmap.ID into LocationsMap
                      from loc in LocationsMap.DefaultIfEmpty()
                          // end left join with locations

                          // left join with Wizard
                      join wzdmap in ctx.Wizards on o.ID equals wzdmap.OrderID into Wizards
                      from wzd in Wizards.DefaultIfEmpty()
                          // end left join wity Wizard

                          // la informacion del taller ya se cargo en la tabla orden al momento de procesarla SendToCompanyID y SendTo
                          //// left join with companies for workshops
                          //join wkmap in ctx.Companies on o.SendToCompanyID equals wkmap.ID into WorkshopMap
                          //from wk in WorkshopMap.DefaultIfEmpty()
                          //// end left join with companies for workshops
                      where
                          udpprop.IsActive.Equals(true) && udpprop.IsRejected.Equals(false)
                          && (filter.CompanyID == 0 || b.CompanyID == filter.CompanyID)
                          && (filter.ProjectID == 0 || o.ProjectID == filter.ProjectID)
                          && (prodType == ProductionType.All || o.ProductionType == prodType)
                          && (filter.OrderStatus == OrderStatus.None || o.OrderStatus == filter.OrderStatus)
                          && (orderNumberEmpty == true || o.OrderNumber.Contains(filter.OrderNumber))
                          && (o.OrderDate >= filter.OrderDate) && o.OrderDate <= filter.OrderDate.AddDays(maxDaysRange)

                          && (filter.InConflict == ConflictFilter.Ignore || o.IsInConflict.Equals(filter.InConflict))
                          && (filter.IsBilled == BilledFilter.Ignore || o.IsBilled.Equals(filter.IsBilled))
                          && (filter.IsStopped == StopFilter.Ignore || o.IsStopped.Equals(filter.IsStopped))
                          && filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID)

                      orderby o.OrderDate descending
                      select
                      new CompanyOrderDTO()
                      {
                          OrderID = o.ID,
                          CompanyID = b.CompanyID,
                          CompanyName = c.Name,             // CUSTOMER - top level
                          OrderNumber = o.OrderNumber,
                          MDOrderNumber = o.MDOrderNumber,
                          OrderDate = o.OrderDate,
                          UserName = o.UserName,
                          Source = o.Source,
                          Quantity = o.Quantity,
                          ProductionType = o.ProductionType,
                          OrderStatus = o.OrderStatus,
                          OrderStatusText = o.OrderStatus.GetText(userData.IsIDT),
                          IsStopped = o.IsStopped,
                          IsBilled = o.IsBilled,
                          IsInConflict = o.IsInConflict,
                          ConfirmedByMD = o.ConfirmedByMD,
                          LocationID = o.LocationID,
                          LocationName = loc != null ? loc.Name : string.Empty,
                          BrandID = b.ID,
                          Brand = b.Name,
                          SendToCompanyID = o.SendToCompanyID,
                          SendToCode = o.SendTo,
                          SendTo = w.Name,
                          BillToCompanyID = o.BillToCompanyID,
                          BillToCode = o.BillTo,
                          BillTo = inv.Name,
                          ProjectID = p.ID,
                          Project = p.Name,
                          Fabric = w.IDTZone,
                          ValidationProgress = wzd != null ? wzd.Progress : 0,
                          NextStates = userData.IsSysAdmin ?
                              OrderUtil.NextStates(o.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst) :
                              OrderUtil.NextManualStates(o.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst)

                      };
            return qry.ToList();
        }



        public IEnumerable<Order> GetOrdersByFilter(OrderFilter filter)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrdersByFilter(ctx, filter);
            }
        }


        public IEnumerable<Order> GetOrdersByFilter(PrintDB ctx, OrderFilter filter)
        {
            return (from o in ctx.CompanyOrders
                    where (filter.CompanyID == null || o.CompanyID == filter.CompanyID) &&
                         (filter.ProjectID == null || o.ProjectID == filter.ProjectID) &&
                         (filter.OrderStatus == null || o.OrderStatus == filter.OrderStatus) &&
                         (filter.ProductionType == null || o.ProductionType == filter.ProductionType) &&
                         (o.CreatedDate > DateTime.Now.AddDays(-30))

                    select o)
            .ToList();
        }



        public IEnumerable<Order> GetOrdersByLabelID(int projectId, int labelId, int count, string orderNumber)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrdersByLabelID(ctx, projectId, labelId, count, orderNumber);
            }
        }


        public IEnumerable<Order> GetOrdersByLabelID(PrintDB ctx, int projectId, int labelId, int count, string orderNumber)
        {
            var articleLabel =  (from o in ctx.CompanyOrders
                    join j in ctx.PrinterJobs on o.ID equals j.CompanyOrderID
                    join a in ctx.Articles on j.ArticleID equals a.ID
                    where o.ProjectID == projectId && a.LabelID == labelId && o.OrderNumber.Contains(orderNumber)
                    select o)
             .OrderByDescending(p => p.OrderDate)
             .Take(count)
             .AsNoTracking()
             .ToList();

            var artifactLabel = (from o in ctx.CompanyOrders
                                 join j in ctx.PrinterJobs on o.ID equals j.CompanyOrderID
                                 join f in ctx.Artifacts on j.ArticleID equals f.ArticleID
                                 where o.ProjectID == projectId && f.LabelID == labelId && o.OrderNumber.Contains(orderNumber)
                                 select o)
            .OrderByDescending(p => p.OrderDate)
            .Take(count)
            .AsNoTracking()
            .ToList();

            return articleLabel.Concat(artifactLabel);
        }


        // ====================================================
        // Order Production Details
        // ====================================================

        public OrderProductionDetail GetOrderProductionDetail(int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderProductionDetail(ctx, orderid);
            }
        }


        public OrderProductionDetail GetOrderProductionDetail(PrintDB ctx, int orderid)
        {
            var order = GetProjectInfo(ctx, orderid);

            if(order == null)
                throw new Exception($"Could not locate Orders {orderid}.");

            var company = (from c in ctx.Companies where c.ID == order.CompanyID select c).Single();
            var catalog = (from c in ctx.Catalogs where c.ProjectID == order.ProjectID && c.Name == "Orders" select c).FirstOrDefault();

            if(catalog == null)
                throw new Exception($"Could not locate Orders catalog for Project {order.ProjectID}");

            var articles = (from a in ctx.Articles where a.ProjectID == order.ProjectID select a).ToList();
            articles.AddRange((from a in ctx.Articles where a.ProjectID == null && a.LabelID != null select a).ToList());// shared labels

            var packs = (from p in ctx.Packs
                         join pa in ctx.PackArticles on p.ID equals pa.PackID
                         join a in ctx.Articles on pa.ArticleID equals a.ID
                         where p.ProjectID.Equals(order.ProjectID) && a.ProjectID.Equals(order.ProjectID)
                         select new PackInfo
                         {
                             PackID = p.ID,
                             PackCode = p.PackCode,
                             ArticleID = a.ID,
                             ArticleCode = a.ArticleCode,
                             BillingCode = a.BillingCode,
                             LabelID = a.LabelID,
                             EncodeRFID = a.Label.EncodeRFID,
                             Quantity = pa.Quantity,
                             Type = pa.Type
                         }).ToList();

            using(var dynamicDB = connManager.CreateDynamicDB())
            {
                var orderDetails = dynamicDB.GetSubset(catalog.CatalogID, order.OrderDataID, "Details");
                var result = new OrderProductionDetail();
                result.OrderID = orderid;
                result.OrderNumber = order.OrderNumber;
                result.CompanyID = order.CompanyID;
                result.ProjectID = order.ProjectID;
                result.SLADays = company.SLADays;
                result.Details = CreateOrderProductionDetail(orderDetails, articles, packs, order.ProjectID);
                return result;
            }
        }


        //public OrderProductionDetail GetOrderProductionDetail(int orderDataID, string orderNumber, int projectID, int companyID)
        //{
        //    using (var ctx = factory.GetInstance<PrintDB>())
        //    {
        //        return GetOrderProductionDetail(ctx, orderDataID, orderNumber, projectID, companyID);
        //    }
        //}


        //public OrderProductionDetail GetOrderProductionDetail(PrintDB ctx, int orderDataID, string orderNumber, int projectID, int companyID)
        //{
        //    var company = (from c in ctx.Companies where c.ID == companyID select c).Single();
        //    var catalog = (from c in ctx.Catalogs where c.ProjectID == projectID && c.Name == "Orders" select c).FirstOrDefault();

        //    if (catalog == null)
        //        throw new Exception($"Could not locate Orders catalog for Project {projectID}");

        //    var articles = (from a in ctx.Articles where a.ProjectID == projectID select a).ToList();
        //    articles.AddRange((from a in ctx.Articles where a.ProjectID == null && a.LabelID != null select a).ToList()); // shared labels

        //    var packs = (from p in ctx.Packs
        //                 join pa in ctx.PackArticles on p.ID equals pa.PackID
        //                 join a in ctx.Articles on pa.ArticleID equals a.ID into paj
        //                 from s in paj.DefaultIfEmpty()
        //                 where p.ProjectID.Equals(projectID) && (s.ProjectID.Equals(projectID) || (s.ProjectID == null && pa.Type == PackArticleType.ByOrderData))
        //                 select new PackInfo
        //                 {
        //                     PackID = p.ID,
        //                     PackCode = p.PackCode,
        //                     ArticleID = pa.Type == PackArticleType.ByOrderData ? 0 : s.ID,
        //                     ArticleCode = s.ArticleCode,
        //                     BillingCode = s.BillingCode,
        //                     LabelID = s.LabelID,
        //                     EncodeRFID = s.Label.EncodeRFID,
        //                     Quantity = pa.Quantity,
        //                     Type = pa.Type,
        //                     CatalogId = pa.CatalogID,
        //                     FieldName = pa.FieldName,
        //                     Mapping = pa.Mapping
        //                 }).ToList();

        //    using (var dynamicDB = connManager.CreateDynamicDB())
        //    {
        //        var orderDetails = dynamicDB.GetSubset(catalog.CatalogID, orderDataID, "Details");
        //        var result = new OrderProductionDetail();
        //        result.OrderID = orderDataID;
        //        result.OrderNumber = orderNumber;
        //        result.CompanyID = companyID;
        //        result.ProjectID = projectID;
        //        result.SLADays = company.SLADays;
        //        result.Details = CreateOrderProductionDetail(orderDetails, articles, packs, projectID);
        //        return result;
        //    }
        //}


        class PackInfo
        {
            public int PackID;
            public string PackCode;
            public int ArticleID;
            public string ArticleCode;
            public string BillingCode;
            public int? LabelID;
            public bool? EncodeRFID;
            public int Quantity;
            public PackArticleType Type;
            public int? CatalogId;
            public string FieldName;
            public string Mapping;
        }


        private List<OrderProductionDetailRow> CreateOrderProductionDetail(JArray array, List<Article> articles, List<PackInfo> packs, int projectId)
        {
            var list = new List<OrderProductionDetailRow>();
            var dataCatalog = new Catalog();

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                dataCatalog = ctx.Catalogs.FirstOrDefault(c => c.ProjectID == projectId && c.IsSystem && c.Name.Equals("VariableData"));
            }

            foreach(var detail in array)
            {
                var item = detail as JObject;
                var row = new OrderProductionDetailRow();
                row.DetailID = item.GetValue<int>("ID");
                row.ArticleCode = item.GetValue<string>("ArticleCode");
                row.Quantity = item.GetValue<int>("Quantity");
                row.Product = item.GetValue<int>("Product");
                row.PackCode = item.GetValue<string>("PackCode");


                #region al clonar no reivsar por packs

                //var article = articles.FirstOrDefault(p => String.Compare(p.ArticleCode, row.ArticleCode, true) == 0);

                // some clientes, put the same name of the articles to the packs
                // XXX: this logic is duplicated  inner Intake -> ExpandPacks Task

                // 1 - Looking inner packs first
                // 2 - Looking inner article list

                //if(packs.Where(pack => pack.PackCode.Equals(row.ArticleCode)).Count() > 0)
                //{

                //    //var packItems = packs.Where(pack => String.Compare(pack.PackCode, row.ArticleCode, true) == 0).ToList();
                //    var packItems = packs.Where(pack => pack.PackCode.Equals(row.ArticleCode) && pack.Type == PackArticleType.ByArticle).ToList();

                //    //get items with orderdata mapping

                //    var packItemsByData = packs.Where(pack => pack.PackCode.Equals(row.ArticleCode) && pack.Type == PackArticleType.ByOrderData).ToList();
                //    var fullMappingSet = new List<MappingDTO>();

                //    if (packItemsByData.Count() > 0)
                //    {
                //        var mappings = packItemsByData.Select(m => m.Mapping).ToList();
                //        foreach (var map in mappings)
                //        {
                //            fullMappingSet.AddRange(JsonConvert.DeserializeObject<List<MappingDTO>>(map));
                //        }

                //        using (var db = connManager.OpenCatalogDB())
                //        {
                //            packItemsByData.ForEach(x =>
                //            {
                //                var articleCodes = JsonConvert.DeserializeObject<List<MappingDTO>>(x.Mapping);

                //                var itemData = db.SelectOne<PackInfo>($@"
                //                                select top 1 vd.{x.FieldName} FieldName 
                //                                from VariableData_{dataCatalog.CatalogID} vd 
                //                                where vd.ID = {row.Product}
                //                            ");

                //                if (itemData != null)
                //                {
                //                    var anyMapping = fullMappingSet.Any(m => m.Key.Equals(itemData.FieldName));
                //                    if (!anyMapping)
                //                    {
                //                        throw new Exception($"Could not find any mapping configuration for FieldName: {x.FieldName} and value: {itemData.FieldName}.");
                //                    }

                //                    foreach (var a in articleCodes)
                //                    {
                //                        if (itemData.FieldName == a.Key)
                //                        {
                //                            var currentArticle = articles.FirstOrDefault(p => p.ArticleCode.Equals(a.Value));

                //                            if (currentArticle != null)
                //                            {
                //                                packItems.Add(new PackInfo()
                //                                {
                //                                    PackCode = row.ArticleCode,
                //                                    ArticleID = currentArticle.ID,
                //                                    ArticleCode = currentArticle.ArticleCode,
                //                                    BillingCode = currentArticle.BillingCode,
                //                                    LabelID = currentArticle.LabelID,
                //                                    Quantity = x.Quantity,
                //                                    FieldName = itemData.FieldName
                //                                });
                //                            }
                //                            else
                //                            {
                //                                throw new ArticleCodeNotFoundException($"Code {row.ArticleCode} does not refer to a valid article. {a.Value}");
                //                            }

                //                        }
                //                    }
                //                }
                //            });
                //        };
                //    }


                //    if (packItems.Count > 0)
                //    {
                //        foreach (var packItem in packItems/*.Where(p => p.LabelID != null)*/)
                //        {
                //            bool addItem = true;

                //            OrderProductionDetailRow itemToAdd = new OrderProductionDetailRow()
                //            {
                //                DetailID = row.DetailID,
                //                ArticleID = packItem.ArticleID,
                //                ArticleCode = packItem.ArticleCode,
                //                Product = row.Product,
                //                Quantity = row.Quantity * packItem.Quantity,
                //                PackID = packItem.PackID,
                //                PackCode = packItem.PackCode,
                //                LabelID = packItem.LabelID
                //            };

                //            //Group items
                //            if (packItem.LabelID == null)
                //            {
                //                var found = list.Where(w => w.ArticleID.Equals(packItem.ArticleID) && !string.IsNullOrEmpty(w.PackCode) && w.PackCode.Equals(packItem.PackCode)).FirstOrDefault();

                //                if (found != null)
                //                {
                //                    itemToAdd = found;
                //                    itemToAdd.Quantity = itemToAdd.Quantity + row.Quantity * packItem.Quantity;
                //                    addItem = false;
                //                }
                //            }

                //            if (addItem)
                //            {
                //                list.Add(itemToAdd);
                //            }
                //        }
                //    }
                //    else throw new ArticleCodeNotFoundException($"The Pack with Code [{row.ArticleCode}] is misconfigured.") { ArticleCode = row.ArticleCode };
                //}else
                #endregion al clonar no revisar por pack
                {
                    var article = articles.FirstOrDefault(p => p.ArticleCode.Equals(row.ArticleCode));
                    if(article != null)
                    {
                        row.ArticleID = article.ID;
                        list.Add(row);
                    }
                    else throw new ArticleCodeNotFoundException($"Code {row.ArticleCode} does not refer to a valid article.") { ArticleCode = row.ArticleCode };
                }
            }
            list.Sort((a, b) => String.Compare(a.ArticleCode, b.ArticleCode, true));
            return list;
        }


        // ====================================================
        // Order Billing Details
        // ====================================================

        public OrderBillingDetail GetOrderBillingDetail(int orderid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderBillingDetail(ctx, orderid);
            }
        }


        public OrderBillingDetail GetOrderBillingDetail(PrintDB ctx, int orderid)
        {
            var order = GetBillingInfo(ctx, orderid);

            if(order == null)
                throw new Exception($"Could not locate Orders {orderid}.");

            var company = (from c in ctx.Companies where c.ID == order.CompanyID select c).Single();
            var catalog = (from c in ctx.Catalogs where c.ProjectID == order.ProjectID && c.Name == "Orders" select c).FirstOrDefault();
            if(catalog == null)
                throw new Exception($"Could not locate Orders catalog for Project {order.ProjectID}");

            var articles = (from a in ctx.Articles where a.ProjectID == order.ProjectID && a.LabelID != null select a).Include(a => a.Label).ToList();
            articles.AddRange((from a in ctx.Articles where a.ProjectID == null && a.LabelID != null select a).Include(a => a.Label).ToList());

            var packs = (from p in ctx.Packs
                         join pa in ctx.PackArticles on p.ID equals pa.PackID
                         join a in ctx.Articles on pa.ArticleID equals a.ID
                         select new PackInfo { PackID = p.ID, PackCode = p.PackCode, ArticleID = a.ID, ArticleCode = a.ArticleCode, BillingCode = a.BillingCode, LabelID = a.LabelID, EncodeRFID = a.Label.EncodeRFID }).ToList();

            var providers = (from p in ctx.CompanyProviders
                             join c in ctx.Companies on p.ProviderCompanyID equals c.ID
                             where p.CompanyID == company.ID
                             select c).ToList();

            using(var dynamicDB = connManager.CreateDynamicDB())
            {
                var orderDetails = dynamicDB.GetSubset(catalog.CatalogID, order.OrderDataID, "Details");
                var result = new OrderBillingDetail();
                result.CompanyCode = company.CompanyCode;
                result.OrderNumber = order.OrderNumber;
                result.CompanyName = company.Name;
                result.GSTCode = company.GSTCode;
                result.GSTID = company.GSTID ?? 0;
                result.BillTo = order.BillTo;
                result.SendTo = order.SendTo;
                InitProviderDetails(result, company, providers);
                InitArticleDetails(result, orderDetails, articles, packs);
                return result;
            }
        }


        private void InitProviderDetails(OrderBillingDetail result, Company company, List<Company> providers)
        {
            result.Providers = new List<MDProvider>();
            AddProviderToResult(company);
            foreach(var provider in providers)
                AddProviderToResult(provider);

            void AddProviderToResult(Company provider)
            {
                result.Providers.Add(new MDProvider()
                {
                    CompanyCode = company.CompanyCode,
                    ProviderCode = provider.CompanyCode,
                    Name = provider.Name,
                    Email = provider.MainContactEmail,
                    GSTCode = provider.GSTCode,
                    GSTID = provider.GSTID ?? 0
                });
            }
        }


        private void InitArticleDetails(OrderBillingDetail bill, JArray orderDetails, List<Article> articles, List<PackInfo> packs)
        {
            var sequence = 1;
            var articleIndex = new Dictionary<string, MDArticle>();
            bill.BillingDetails = new List<MDBillingDetail>();
            foreach(var detail in orderDetails)
            {
                var item = detail as JObject;
                var articleCode = item.GetValue<string>("ArticleCode");
                var quantity = item.GetValue<int>("Quantity");
                var article = articles.FirstOrDefault(p => String.Compare(p.ArticleCode, articleCode, true) == 0);
                if(article != null)
                {
                    AddArticleToBill("PRINT_" + article.ID, article.Label.EncodeRFID, article.BillingCode, quantity);
                }
                else
                {
                    var packItems = packs.Where(pack => String.Compare(pack.PackCode, articleCode, true) == 0).ToList();
                    if(packItems.Count > 0)
                    {
                        foreach(var packItem in packItems.Where(p => p.LabelID != null))
                            AddArticleToBill("PRINT_" + packItem.ArticleID, packItem.EncodeRFID ?? false, packItem.BillingCode, quantity);
                    }
                    else throw new Exception($"Code {articleCode} does not refer to a valid article.");
                }
            }
            bill.Articles = new List<MDArticle>(articleIndex.Values);

            void AddArticleToBill(string articleCode, bool encodeRFID, string billingCode, int quantity)
            {
                bill.BillingDetails.Add(new MDBillingDetail()
                {
                    Sequence = sequence++,
                    Article = articleCode,
                    EncodeRFID = encodeRFID,
                    Quantity = quantity,
                    Date = DateTime.Now
                });
                if(!articleIndex.ContainsKey(articleCode))
                    articleIndex.Add(articleCode, new MDArticle() { ArticleCode = articleCode, EncodeRFID = encodeRFID, BillingCode = billingCode });
            }
        }


        public IEnumerable<Order> GetOrderAffectedByCatalogUpdate(int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderAffectedByCatalogUpdate(ctx, projectID);
            }
        }


        public IEnumerable<Order> GetOrderAffectedByCatalogUpdate(PrintDB ctx, int projectID)
        {
            return _GetOrderAffectedByCatalogUpdate(ctx, projectID).ToList();
        }


        public int TotalOrdersAffectedByCatalogupdate(int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return _GetOrderAffectedByCatalogUpdate(ctx, projectID).Count();
            }
        }


        public int TotalOrdersAffectedByCatalogupdate(PrintDB ctx, int projectID)
        {
            return _GetOrderAffectedByCatalogUpdate(ctx, projectID).Count();
        }


        private IQueryable<Order> _GetOrderAffectedByCatalogUpdate(PrintDB ctx, int projectID)
        {
            // TODO: en que estatus estaran las ordens que pueden verse afectadas
            var notInStatus = new List<OrderStatus>() {
                OrderStatus.Received,
                OrderStatus.Processed,
                OrderStatus.Completed
            };

            // buscar los DataID afectados, no se puede saber, ya que en los mappings se realizan sustituciones
            // y el catalogo de la etiqueta no esta directamente relacionado al catalogo lookup, ejemplo caso de fibras en la CPO

            var query = ctx.CompanyOrders
                .Join(ctx.OrderUpdateProperties,
                o => o.ID,
                p => p.OrderID,
                (ord, prop) => new { Order = ord, Properties = prop }
                )
                .Where(w => w.Order.ProjectID.Equals(projectID))
                .Where(w => !notInStatus.Contains(w.Order.OrderStatus))
                .Where(w => w.Properties.IsActive && !w.Properties.IsRejected);

            return query.Select(s => s.Order);
        }

        #region Clone, Repear, Copy Orders

        public void Clone(int id, bool isBillable, string articleCode, int? providerID, string username, bool withSameData = false, DocumentSource source = DocumentSource.NotSet)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var printJobRepo = factory.GetInstance<IPrinterJobRepository>();
                var propRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();

                // ensure to unmodified original order

                var parentOrder = GetByID(id);
                var sourceOrder = JsonConvert.DeserializeObject<Order>(JsonConvert.SerializeObject((Order)parentOrder));

                var baseOrderNumber = string.Empty;
                var newOrderId = 0;
                var nextId = 0;
                int? articleID = null;

                if(!withSameData)
                {
                    baseOrderNumber = sourceOrder.OrderNumber;
                    var repPattern = $"{Order.REPEAT_PATTERN}|{Order.DERIVATION_PATTERN}";

                    if(Regex.IsMatch(baseOrderNumber, repPattern))
                    {
                        string[] orderNumberParts = baseOrderNumber.Split('-');
                        baseOrderNumber = string.Join('-', orderNumberParts.SkipLast(1));
                    }

                    // include cancelled too to avoid coflicts
                    var found = ctx.CompanyOrders.Where(x => x.OrderGroupID == sourceOrder.OrderGroupID && x.ProjectID == sourceOrder.ProjectID)
                        .OrderByDescending(x => x.CreatedDate)
                        .ToList();

                    var lastDuplicated = found.Where(w => Regex.IsMatch(w.OrderNumber, repPattern) && Regex.Matches(w.OrderNumber, repPattern).Count() == 1).FirstOrDefault();// looking for repetition
                    nextId = lastDuplicated != null ? int.Parse(Regex.Replace(lastDuplicated.OrderNumber.Split("-").LastOrDefault(), "[^0-9]", "")) + 1 : 1;

                    var catalogs = from c in ctx.Catalogs where c.ProjectID == sourceOrder.ProjectID select c;
                    var catalogDefinitionList = new List<CatalogDefinition>();

                    using(var dynamicDB = connManager.CreateDynamicDB())
                    {
                        foreach(var catalog in catalogs/*.Where(w=>w.CatalogType == CatalogType.Inlined)*/)
                        {
                            catalogDefinitionList.Add(dynamicDB.GetCatalog(catalog.CatalogID));
                        }

                        var tables = dataRepo.GetFullOrderData(catalogDefinitionList, sourceOrder.OrderDataID);

                        //get editable catalogs
                        foreach(var table in tables)
                        {
                            var type = catalogs.FirstOrDefault(x => table.Name.Equals(x.TableName));
                            table.IsEditble = type.CatalogType == CatalogType.Inlined ? true : false;
                        }

                        var tableName = tables.FirstOrDefault().Name;

                        dataRepo.GetRelIds(dynamicDB, tables, tableName, articleCode);

                        ProccessData(dynamicDB, "", tables);

                        var orderCatalog = catalogs.FirstOrDefault(x => x.Name.Equals(Catalog.ORDER_CATALOG));
                        newOrderId = tables.FirstOrDefault(x => x.Name.Equals(orderCatalog.TableName)).NewRefIds.FirstOrDefault();
                    }
                }
                else
                {
                    articleID = ctx.Articles.Where(x => x.ArticleCode == articleCode && x.ProjectID == sourceOrder.ProjectID).Select(y => y.ID).FirstOrDefault();

                }

                if(newOrderId < 1) // TODO: this conditon maybe is unnecesary, test required
                    return;

                var orderNumber = withSameData ? sourceOrder.OrderNumber : baseOrderNumber + "-" + nextId + (providerID != null ? "D" : "R");

                if(orderNumber.Length > 16)
                    orderNumber = baseOrderNumber;

                var newOrder = Create();
                newOrder = sourceOrder;
                newOrder.ID = 0;
                newOrder.OrderStatus = OrderStatus.Received;
                newOrder.OrderDataID = withSameData ? sourceOrder.OrderDataID : newOrderId;
                newOrder.MDOrderNumber = null;
                newOrder.SageReference = null;
                newOrder.IsBillable = isBillable;
                newOrder.IsBilled = false;
                newOrder.SageStatus = SageOrderStatus.Unknow;
                newOrder.InvoiceStatus = SageInvoiceStatus.Unknow;
                newOrder.DeliveryStatus = SageDeliveryStatus.Unknow;
                newOrder.SageStatus = SageOrderStatus.Unknow;
                newOrder.CreditStatus = SageCreditStatus.Unknow;
                newOrder.OrderNumber = orderNumber;
                newOrder.UserName = username;
                newOrder.ItemID = null;
                newOrder.DeliveryStatusID = DeliveryStatus.NotSet;   

                if(source != DocumentSource.NotSet)
                {
                    newOrder.Source = source;
                }

                //If it is a derive order, set provider data
                if(providerID != null)
                {
                    var provider = ctx.Companies.Where(w => w.ID.Equals(providerID)).First();

                    newOrder.BillToCompanyID = provider.ID;
                    newOrder.SendToCompanyID = provider.ID;
                    newOrder.BillTo = provider.CompanyCode;
                    newOrder.SendTo = provider.CompanyCode;
                    newOrder.ParentOrderID = id;

                    // XXXX: pendint to update SentToAddressID for the new provider

                    var orderGroup = GetOrderGroup(newOrder.OrderNumber, newOrder.ProjectID, newOrder.BillToCompanyID, newOrder.SendToCompanyID);
                    newOrder.OrderGroupID = orderGroup.ID;
                }

                newOrder = Insert(ctx, newOrder);



                var jobs = printJobRepo.CreateFromOrder(ctx, newOrder.ID, newOrder.AssignedPrinterID, false, articleID);

                var orderProperties = new OrderUpdateProperties();
                orderProperties.OrderID = newOrder.ID;
                orderProperties.IsActive = true;

                propRepo.Insert(ctx, orderProperties);

                //copy order file
                if(store.TryGetFile(id, out var file))
                {
                    if(!store.TryGetFile(newOrder.ID, out var newOrderFile))
                    {
                        var container = store.GetOrCreateFile(newOrder.ID, file.FileName);
                        using(var src = file.GetContentAsStream())
                        {
                            container.SetContent(src);
                        }
                    }
                }
                else
                {
                    // for older orders the file was removed, set dummy file
                    var container = store.GetOrCreateFile(newOrder.ID, "DUMMY_FILE.txt");
                    byte[] dummyFileContent = Encoding.UTF8.GetBytes("OLDER ORDER FILES WAS REMOVED - use dummy file");
                    container.SetContent(new MemoryStream(dummyFileContent));

                }

                //var orderInfo = GetProjectInfo(ctx, newOrder.ID);
                //events.Send(new OrderFileReceivedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                InsertItemOnWorkflow(newOrder).GetAwaiter().GetResult();


                CloneComposition(parentOrder, newOrder, jobs);

            }
        }

        public IOrder Copy(int parentOrder, bool isBillable, string articleCode, int? providerID, string username, DocumentSource source = DocumentSource.NotSet)
        {
            IOrder newOrder = null;

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var printJobRepo = factory.GetInstance<IPrinterJobRepository>();
                var propRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();
                // ensure to unmodified original order
                var order = JsonConvert.DeserializeObject<Order>(JsonConvert.SerializeObject((Order)GetByID(parentOrder)));

                var sourceArticle = GetOrderArticle(ctx, parentOrder, order.ProjectID);

                var baseOrderNumber = order.OrderNumber;

                var catalogs = from c in ctx.Catalogs where c.ProjectID == order.ProjectID select c;
                var catalogDefinitionList = new List<CatalogDefinition>();

                using(var dynamicDB = connManager.CreateDynamicDB())
                {
                    foreach(var catalog in catalogs/*.Where(w=>w.CatalogType == CatalogType.Inlined)*/)
                    {
                        catalogDefinitionList.Add(dynamicDB.GetCatalog(catalog.CatalogID));
                    }

                    var tables = dataRepo.GetFullOrderData(catalogDefinitionList, order.OrderDataID);

                    //get editable catalogs
                    foreach(var table in tables)
                    {
                        var type = catalogs.FirstOrDefault(x => table.Name.Equals(x.TableName));
                        table.IsEditble = type.CatalogType == CatalogType.Inlined ? true : false;
                    }

                    var tableName = tables.FirstOrDefault().Name;

                    dataRepo.GetRelIds(dynamicDB, tables, tableName, sourceArticle.ArticleCode);

                    ProccessData(dynamicDB, "", tables);

                    var orderCatalog = catalogs.FirstOrDefault(x => x.Name.Equals(Catalog.ORDER_CATALOG));
                    var newOrderId = tables.FirstOrDefault(x => x.Name.Equals(orderCatalog.TableName)).NewRefIds.FirstOrDefault();

                    if(newOrderId < 1)
                    {
                        return newOrder; // TODO: maybe is better throw an exception
                    }
                    newOrder = Create();
                    newOrder = order;
                    newOrder.ID = 0;
                    newOrder.OrderStatus = OrderStatus.Received;
                    newOrder.OrderDataID = newOrderId;
                    newOrder.MDOrderNumber = null;
                    newOrder.SageReference = null;
                    newOrder.IsBillable = isBillable;
                    newOrder.IsBilled = false;
                    newOrder.SageStatus = SageOrderStatus.Unknow;
                    newOrder.InvoiceStatus = SageInvoiceStatus.Unknow;
                    newOrder.DeliveryStatus = SageDeliveryStatus.Unknow;
                    newOrder.SageStatus = SageOrderStatus.Unknow;
                    newOrder.CreditStatus = SageCreditStatus.Unknow;
                    newOrder.OrderNumber = baseOrderNumber;
                    newOrder.UserName = username;
                    newOrder.ParentOrderID = parentOrder;
                    newOrder.ItemID = null;
                    newOrder.DeliveryStatusID = DeliveryStatus.NotSet;   

                    if(source != DocumentSource.NotSet)
                    {
                        newOrder.Source = source;
                    }

                    //If it is a derive order, set provider data
                    if(providerID != null)
                    {
                        var provider = ctx.Companies.Where(w => w.ID.Equals(providerID)).First();

                        newOrder.BillToCompanyID = provider.ID;
                        newOrder.SendToCompanyID = provider.ID;
                        newOrder.BillTo = provider.CompanyCode;
                        newOrder.SendTo = provider.CompanyCode;

                        var orderGroup = GetOrderGroup(newOrder.OrderNumber, newOrder.ProjectID, newOrder.BillToCompanyID, newOrder.SendToCompanyID);
                        newOrder.OrderGroupID = orderGroup.ID;
                    }

                    newOrder = Insert(ctx, newOrder);

                    var foundArticle = ctx.Articles.Where(x => x.ArticleCode == articleCode && x.ProjectID == order.ProjectID).Select(y => y.ID).FirstOrDefault();


                    printJobRepo.CreateFromOrder(ctx, newOrder.ID, newOrder.AssignedPrinterID, false, foundArticle);


                    var orderProperties = new OrderUpdateProperties();
                    orderProperties.OrderID = newOrder.ID;
                    orderProperties.IsActive = true;

                    propRepo.Insert(ctx, orderProperties);

                    //copy order file
                    if(store.TryGetFile(parentOrder, out var file))
                    {
                        if(!store.TryGetFile(newOrder.ID, out var newOrderFile))
                        {
                            var container = store.GetOrCreateFile(newOrder.ID, file.FileName);
                            using(var src = file.GetContentAsStream())
                            {
                                container.SetContent(src);
                            }
                        }
                    }

                    // TODO: create validator here, to avoid wait for the OrderFileReceivedEvent finish execution

                    //var orderInfo = GetProjectInfo(ctx, newOrder.ID);
                    //events.Send(new OrderFileReceivedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                    InsertItemOnWorkflow(newOrder).GetAwaiter().GetResult();

                }
            }

            return newOrder;
        }



        public void CloneComposition(IOrder source, IOrder target, List<IPrinterJob> targetJobs)
        {
            var orderUtilService = factory.GetInstance<IOrderUtilService>();
            var printjobRepository = factory.GetInstance<IPrinterJobRepository>();

            var catalogs = GetCompositionCatalogsForProject(source.ProjectID);
            var variableDataCatalog = catalogs.First(w => w.Name == Catalog.VARIABLEDATA_CATALOG);
            var compoDataCatalog = catalogs.First(w => w.Name == Catalog.COMPOSITIONLABEL_CATALOG);
            var detailsCatalog = catalogs.First(w => w.Name == Catalog.ORDERDETAILS_CATALOG);
            var fields = variableDataCatalog.Fields.ToList();

            // TODO: HOW to verify if the current project have the standard project structure
            if(fields.Count(w => w.Name == "HasComposition") < 1)
                return;

            var sourceCompositions = GetUserCompositionForGroup(source.OrderGroupID)
                .Where(w => w.OrderID == source.ID)
                .ToList();

            var sourceProductData = sourceCompositions.Select(s => s.ProductDataID).ToList();
            var targetPrintJobDetails = printjobRepository.JobDetailsByOrder(target.ID).ToList();
            var targetProductData = targetPrintJobDetails.Select(s => s.ProductDataID).ToList();



            if(sourceCompositions.Count < 1)
                return; // don't exist composition data, maybe is an article without compo

            // can ensure the Order of the composition belong to the correct detail USING BARCODE, some BRANDS USE VariableData.TXT3 column like Grouping Column
            // the Order details always are saved in the same sequence order received
            // 2.- Clone CompositionLabel Row
            using(var dynamicDB = connManager.CreateDynamicDB())
            {

                // Can use one query to get barcodes for both order, include in query table Order and Relation between Order and OrderDetails, include in Select OrderID
                var sourceBarcodes = dynamicDB.Select(detailsCatalog.CatalogID, $@"
                        SELECT d.ID as ProductDataID, v.Barcode, v.Size, v.Color, v.TXT3 
                        FROM #TABLE d 
                        INNER JOIN {variableDataCatalog.TableName} v on d.Product = v.ID
                        WHERE d.ID in ({string.Join(',', sourceProductData)})");

                var targetBarcodes = dynamicDB.Select(detailsCatalog.CatalogID, $@"
                        SELECT d.ID as ProductDataID, v.Barcode, v.Size, v.Color, v.TXT3 
                        FROM #TABLE d 
                        INNER JOIN {variableDataCatalog.TableName} v on d.Product = v.ID
                        WHERE d.ID in ({string.Join(',', targetProductData)})");



                for(var rowNumber = 0; rowNumber < sourceCompositions.Count; rowNumber++)
                {

                    // 1.- clone structure
                    var compoToClone = sourceCompositions[rowNumber];

                    var currentBarcode = sourceBarcodes.Where(w => w["ProductDataID"].ToString() == compoToClone.ProductDataID.ToString()).Single();
                    var targetBarcode = targetBarcodes.Where(w => w["Barcode"].ToString() == currentBarcode["Barcode"].ToString()).Single();
                    var targetDetail = targetPrintJobDetails.Single(w => w.ProductDataID == (int)targetBarcode["ProductDataID"]);

                    //var targetDetail = targetDetails[rowNumber];
                    var iDSource = compoToClone.ID;

                    var pj = targetJobs.First(w => w.ID == targetDetail.PrinterJobID);

                    compoToClone.OrderGroupID = target.OrderGroupID;
                    compoToClone.OrderDataID = target.OrderDataID;
                    compoToClone.OrderID = target.ID;
                    compoToClone.ProductDataID = targetDetail.ProductDataID;
                    compoToClone.ArticleID = pj.ArticleID;// XXX: the original composition object some times come with blank ArticleID, this is a bug
                    compoToClone.ID = 0; // set like a new compo (unsaved)

                    orderUtilService.SaveCompositionDefinition(compoToClone);


                    var rowSource = dynamicDB.SelectOne(compoDataCatalog.CatalogID, "SELECT * FROM #TABLE WHERE ID = @idSource ", iDSource);

                    // ???: Copy fields from the existing JObject to a new one, without including Sets and References fields
                    var definition = compoDataCatalog.Fields.ToList();
                    definition.ForEach(f =>
                    {
                        if(f.Type == ColumnType.Set || f.Type == ColumnType.Reference)
                            rowSource.Remove(f.Name);
                    });

                    // update target composition row
                    rowSource["ID"] = compoToClone.ID;

                    dynamicDB.Update(compoDataCatalog.CatalogID, rowSource);

                }

            }



        }
        #endregion

        public void ProccessData(DynamicDB dynamicDB, string tableName, List<TableObject> tables, bool isRefTable = false)
        {
            if(!string.IsNullOrEmpty(tableName))
            {
                var table = tables.FirstOrDefault(x => Equals(x.Name, tableName));

                foreach(var refTable in table.RefTables)
                {
                    //insert parent data
                    ProccessData(dynamicDB, refTable.Name, tables, true);
                }

                //current table rows
                ProcessCurrentTable(table, dynamicDB, isRefTable, tables);

                table.Processed = true;
                #region REL TABLES
                //RelTables - process rel Fields: needed to fill current row
                foreach(var relTable in table.RelTables)
                {
                    foreach(var id in relTable.ParentIds)
                    {
                        var catalogDataList = dynamicDB.GetSubset(table.Id, id, relTable.Field);
                        var currentTable = tables.FirstOrDefault(x => x.Name.Equals(relTable.TargetName + "_" + relTable.TargetId));

                        if(currentTable.RefTables.Count > 0)
                        {
                            foreach(var refTable in currentTable.RefTables)
                            {
                                //insert parent data
                                ProccessData(dynamicDB, refTable.Name, tables);
                            }
                        }

                        foreach(JObject row in catalogDataList.Children<JObject>())
                        {
                            var removeProperties = new List<string>();
                            currentTable.NewRefIds = new List<int>();
                            foreach(JProperty field in row.Properties())
                            {
                                var tableField = currentTable.RefTables.FirstOrDefault(x => x.Field.Equals(field.Name));
                                if(tableField != null)
                                {
                                    removeProperties.Add("_" + field.Name + "_" + "DISP");
                                    var originRefTable = tables.FirstOrDefault(t => Equals(t.Name, tableField.Name));
                                    var index = originRefTable.ParentIds.IndexOf(int.Parse(field.Value.ToString()));
                                    field.Value = originRefTable.NewRefIds.ElementAt(index);
                                }
                            }

                            //remove unknown properties
                            foreach(var i in removeProperties)
                            {
                                row.Property(i).Remove();
                            }

                            var newId = 0;
                            if(currentTable.IsEditble)
                            {
                                newId = dynamicDB.Insert(currentTable.Id, row);
                                currentTable.NewRefIds.Add(newId);
                            }
                            else
                            {
                                newId = int.Parse(row.Property("ID").Value.ToString());
                            }

                            currentTable.Processed = true;
                            //insert data into rel table

                            var newRefIndex = table.ParentIds.IndexOf(id);
                            dynamicDB.InsertRel(relTable.SourceId, relTable.TargetId, table.Fields.FirstOrDefault(x => x.Name.Equals(relTable.Field)).FieldID, table.NewRefIds[newRefIndex], newId);
                        }
                    }
                }
                #endregion REL TABLES
            }
            else
            {
                foreach(var table in tables)
                {
                    if(!table.Processed)
                    {
                        //process ref fields: needed to fill current row
                        if(table.RefTables.Count > 0)
                        {
                            foreach(var refTable in table.RefTables)
                            {
                                //insert parent data
                                ProccessData(dynamicDB, refTable.Name, tables);
                            }
                        }

                        //current table rows
                        ProcessCurrentTable(table, dynamicDB, isRefTable, tables);

                        table.Processed = true;

                        //process rel Fields: needed to fill rel tables
                        foreach(var relTable in table.RelTables)
                        {
                            ProccessData(dynamicDB, relTable.TargetName + "_" + relTable.TargetId, tables);

                            foreach(var ids in table.NewRefIds)
                            {
                                var currentTable = tables.FirstOrDefault(x => x.Name.Equals(relTable.TargetName + "_" + relTable.TargetId));

                                foreach(var id in currentTable.NewRefIds)
                                {
                                    //insert data into rel table
                                    dynamicDB.InsertRel(relTable.SourceId, relTable.TargetId, table.Fields.FirstOrDefault(x => x.Name.Equals(relTable.Field)).FieldID, ids, id);
                                }
                            }
                        }
                    }
                }
            }
        }



        public void ProcessCurrentTable(TableObject table, DynamicDB dynamicDB, bool isRefTable, List<TableObject> tables)
        {
            table.NewRefIds = new List<int>();
            if(table.IsEditble)
            {
                foreach(var id in table.ParentIds)
                {
                    var catalogData = dynamicDB.Select(table.Id, "select * from #TABLE where ID = @id", id);

                    foreach(JObject row in catalogData)
                    {
                        //if (!isRefTable && table.RefTables.Count > 0)
                        // TODO: TRANSLATE - si las tablas de referencias tienen datos ya registrados actualizar el valor de las llaves en cada row
                        if(table.RefTables.Count > 0 && tables.Where(w => table.RefTables.Any(_a => _a.Name.Equals(w.Name)) && w.NewRefIds.Count() > 0).Count() > 0)
                        {
                            foreach(JProperty field in row.Properties())
                            {
                                var refTable = table.RefTables.FirstOrDefault(x => x.Field.Equals(field.Name));
                                if(refTable != null)
                                {
                                    var currentTable = tables.FirstOrDefault(t => Equals(t.Name, refTable.Name));
                                    if(currentTable.IsEditble && !string.IsNullOrEmpty(field.Value.ToString()) && field.Value.Type != JTokenType.Null)
                                    {
                                        var index = currentTable.ParentIds.IndexOf(int.Parse(field.Value.ToString()));
                                        field.Value = currentTable.NewRefIds.ElementAt(index);
                                    }
                                }
                            }
                        }

                        // HARCODE: not copy id of Composition Catalogs reference
                        // if field is a reference and the table is not include, remove reference
                        foreach(var fld in table.Fields.Where(_w => _w.Type == ColumnType.Reference))
                        {
                            var referenceTable = tables.FirstOrDefault(_w => _w.Id == fld.CatalogID);

                            if(referenceTable != null)
                                continue;

                            row[fld.Name] = null; // remove reference with Table
                        }

                        var newId = dynamicDB.Insert(table.Id, row);
                        table.NewRefIds.Add(newId);
                    }
                }
            }
        }



        private IOrderGroup GetOrderGroup(string orderNumber, int projectId, int billtocompanyID, int sendtocompanyID)
        {
            var orderGroupRepo = factory.GetInstance<IOrderGroupRepository>();
            var companyRepo = factory.GetInstance<ICompanyRepository>();

            IOrderGroup og = new OrderGroup()
            {
                OrderNumber = orderNumber,
                ProjectID = projectId,
                BillToCompanyID = billtocompanyID,
                SendToCompanyID = sendtocompanyID,
                IsActive = true,
                IsRejected = false
            };

            return orderGroupRepo.GetGroupFor(og);
        }

        public List<EncodedEntity> GetEncodedByOrder(int id)
        {
            using(var conn = connManager.OpenWebLinkDB())
            {
                //return conn.Select<EncodedEntity>(@"	
                //        	SELECT Barcode, COUNT(*) TotalEncoded
                //            FROM EncodedLabels
                //         INNER JOIN CompanyOrders o ON o.ID = EncodedLabels.OrderID
                //            WHERE OrderID = @id AND OrderID IN (	SELECT o2.ID FROM CompanyOrders o1
                //              JOIN CompanyOrders o2 ON o2.OrderNumber = o1.OrderNumber
                //              INNER JOIN PrinterJobs pj ON o2.ID = pj.CompanyOrderID
                //              INNER JOIN Articles a ON pj.ArticleID = a.ID
                //              WHERE o2.ProjectID = o1.ProjectID 
                //              AND o1.OrderDate >= o2.OrderDate
                //              AND ArticleCode = a.ArticleCode
                //              AND o1.ID = @id)
                //            GROUP BY Barcode
                //                                ", id
                //);
                return conn.Select<EncodedEntity>(@"	
                        	SELECT Barcode, COUNT(*) TotalEncoded
                            FROM EncodedLabels
                            WHERE OrderID = @id
							and SyncState in (2,3)
                            GROUP BY Barcode
                                                ", id
                );
            }
        }


        //================================================== Workflow Refactor ===============================================

        public IOrderGroup GetOrCreateOrderGroup(string orderNumber, int projectid, int billToCompanyID, int sendToCompanyID, string erpReference, string clientCategory, int? ProviderRecordId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrCreateOrderGroup(ctx, orderNumber, projectid, billToCompanyID, sendToCompanyID, erpReference, clientCategory, ProviderRecordId);
            }
        }


        public IOrderGroup GetOrCreateOrderGroup(PrintDB ctx, string orderNumber, int projectid, int billToCompanyID, int sendToCompanyID, string erpReference, string clientCategory, int? ProviderRecordId)
        {
            var orderGroupRepo = factory.GetInstance<IOrderGroupRepository>();
            var companyRepo = factory.GetInstance<ICompanyRepository>();

            IOrderGroup og = new OrderGroup()
            {
                OrderNumber = orderNumber,
                ProjectID = projectid,
                BillToCompanyID = billToCompanyID,
                SendToCompanyID = sendToCompanyID,
                ERPReference = erpReference,
                OrderCategoryClient = clientCategory,
                IsActive = true,
                IsRejected = false
            };

            return orderGroupRepo.GetOrCreateGroup(ctx, og, ProviderRecordId);
        }

        //================================================== Workflow Refactor ===============================================

        public CompanyOrderCountryDTO GetCountryByOrderLocation(int orderGroupID)
        {

            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetCountryByOrderLocation(ctx,orderGroupID);
            }
        }

        public CompanyOrderCountryDTO GetCountryByOrderLocation(PrintDB ctx, int orderGroupID)
        {
            var data = (from co in ctx.CompanyOrders
                        join l in ctx.Locations on co.LocationID equals l.ID
                        join c in ctx.Countries on l.CountryID equals c.ID
                        where co.OrderGroupID == orderGroupID
                        select new CompanyOrderCountryDTO()
                        {
                            OrderID = co.ID,
                            OrderNumber = co.OrderNumber,
                            OrderGroupID = co.OrderGroupID,
                            LocationID = l.ID,
                            CountryName = c.Name
                        }).FirstOrDefault();

            return data;

        }

    }


    public class EncodedEntity
    {
        public string Barcode;
        public int TotalEncoded;
    }

    public class MappingDTO
    {
        public string Key;
        public string Value;
    }

    public class CompanyOrderCountryDTO
    {
        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public int OrderGroupID { get; set; }
        public int LocationID { get; set; }
        public string CountryName { get; set; }
    }
}
