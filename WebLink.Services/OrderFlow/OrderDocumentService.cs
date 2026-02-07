using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.LabelService;
using Service.Contracts.PDFDocumentService;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
    public class OrderDocumentService : IOrderDocumentService
    {
        private IFactory factory;
        private IAppConfig configuration;
        private IFileStoreManager storeManager;
        private IFileStore orderStore;
        private ILabelRepository labelRepo;
        private IArticleRepository articleRepo;
        private IPDFDocumentService pdfSrv;
        private IDBConnectionManager db;
        private ITempFileService tempSrv;
        private ILogService log;
        private IProviderRepository providerRepo;
        private IBLabelServiceClient labelService;
        private IArtifactRepository artifactRepo;


        private const int MaxPreviewUnits = 2500;

        public OrderDocumentService(
            IFactory factory,
            IAppConfig configuration,
            IFileStoreManager storeManager,
            ILabelRepository labelRepo,
            IArticleRepository articleRepo,
            IPDFDocumentService pdfSrv,
            IDBConnectionManager db,
            ITempFileService tempSrv,
            ILogService log,
            IProviderRepository providerRepo,
            IBLabelServiceClient labelService,
            IArtifactRepository artifactRepo
        )
        {
            this.factory = factory;
            this.configuration = configuration;
            this.storeManager = storeManager;
            this.labelRepo = labelRepo;
            this.articleRepo = articleRepo;
            this.pdfSrv = pdfSrv;
            this.db = db;
            this.tempSrv = tempSrv;
            this.log = log;
            this.providerRepo = providerRepo;
            this.labelService = labelService;
            orderStore = storeManager.OpenStore("OrderStore");
            pdfSrv.Url = configuration["WebLink.PDFDocumentService"];
            this.artifactRepo = artifactRepo;
        }


        /// <summary>
        /// Checks to see if the preview document for the specified order already exists, if it exists then it also returns the GUID of the document.
        /// </summary>
        public bool PreviewDocumentExists(int orderid, out Guid documentGuid)
        {
            documentGuid = Guid.Empty;
            if (!orderStore.TryGetFile(orderid, out var file))
                return false;
            var category = file.GetAttachmentCategory("Documents");
            if (!category.TryGetAttachment($"OrderPreview_{orderid}.pdf", out var attachment))
                return false;
            documentGuid = attachment.FileGUID;
            return true;
        }


        /// <summary>
        /// Gets the path to the preview document (if available) if the preview is being generated in another process then it waits for up to 
        /// 30 seconds for it to complete before throwing a timeout error.
        /// </summary>
        /// <param name="orderid">The Order ID</param>
        /// <returns>The path to the document</returns>
        public IFSFile GetPreviewDocument(int orderid, bool forceCreate=false)
        {
			if (!orderStore.TryGetFile(orderid, out var file))
                throw new Exception($"Could not find order file in the order file store (OrderID: {orderid})");

            if (!SharedLock.TryAcquire($"OrderPreview_{orderid}", TimeSpan.FromSeconds(30), out var slock))
                throw new Exception("Timeout while waiting for the document to be available.");

            try
            {
                var category = file.GetAttachmentCategory("Documents");
                if (!category.TryGetAttachment($"OrderPreview_{orderid}.pdf", out var attachment))
                {
                    if(forceCreate)
                    {
						slock.Dispose();
						CreatePreviewDocument(orderid).GetAwaiter().GetResult();
						category = file.GetAttachmentCategory("Documents");
						if (!category.TryGetAttachment($"OrderPreview_{orderid}.pdf", out attachment))
							throw new Exception($"The requested document could not be found for order [{orderid}].");
				    }
                    else
                    {
						throw new Exception($"The requested document could not be found for order [{orderid}].");
					}
				}
                return attachment;
            }
            finally
            {
                slock.Dispose();
            }
        }


        /// <summary>
        /// Creates the order preview document and attaches it to the order in the Documents attachment category.
        /// </summary>
        /// <remarks>
        /// If another process is updating the document, then the call terminates immediatelly and returns null.
        /// If you need to wait for the document to be completed and retrieve it, call GetPreviewDocument instead.
        /// </remarks>
        /// <param name="orderid">ID of the order</param>
        /// <returns>Returns the path to the created document</returns>
        public async Task<Guid> CreatePreviewDocument(int orderid)
        {
            if (!orderStore.TryGetFile(orderid, out var file))
                throw new Exception($"Could not find order file in the order file store (OrderID: {orderid})");

            if (!SharedLock.TryAcquire($"OrderPreview_{orderid}", TimeSpan.FromMilliseconds(1), out var slock))
                return Guid.Empty;

            try
            {
                PDFOrderPreview preview = new PDFOrderPreview();
                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    var order = ctx.CompanyOrders.Where(o => o.ID == orderid).Single();
                    var company = ctx.Companies.Where(c => c.ID == order.CompanyID).Single();
                    var companyProvider = order.ProviderRecordID != null ? providerRepo.GetByID(order.ProviderRecordID.Value) : null;
                    var provider = companyProvider != null ? ctx.Companies.Where(c => c.ID == companyProvider.ProviderCompanyID).Single() : null;
                    var project = ctx.Projects.Where(p => p.ID == order.ProjectID).Single();
                    var brand = ctx.Brands.Where(b => b.ID == project.BrandID).Single();
                    var jobs = ctx.PrinterJobs.Where(j => j.CompanyOrderID == orderid).ToList();

                    //validation for supportfiles in project
                    if (project.IncludeFiles)
                    {
                        var article = ctx.Articles.Where(x => jobs.Select(j => j.ArticleID).Any(id => id == x.ID)).Single();
                        var label = ctx.Labels.Where(x => x.ID == (article.LabelID == null ? 0 : article.LabelID)).FirstOrDefault();
                        var categoryName = string.Empty;

                        if (label != null)
                        {
                            switch (label.Type)
                            {
                                case LabelType.Sticker:
                                    categoryName = "Sticker";
                                    break;
                                case LabelType.HangTag:
                                    categoryName = "Hangtag";
                                    break;
                                case LabelType.CareLabel:
                                    categoryName = "Composition";
                                    break;
                                default:
                                    categoryName = "PiggyBack";
                                    break;
                            }
                        }

                        preview.OrderGroupID = order.OrderGroupID;
                        preview.SupportFileCategory = categoryName;
                    }


                    preview.OrderNumber = order.OrderNumber;
                    preview.MDOrderNumber = order.MDOrderNumber;
                    preview.CompanyName = company.Name;
                    preview.ClientReference = companyProvider != null ? companyProvider.ClientReference : null;
                    preview.Provider = provider != null ? provider.Name : null;
                    preview.BrandName = brand.Name;
                    preview.ProjectName = project.Name;
                    preview.OrderDate = order.OrderDate;
                    preview.ValidationDate = order.ValidationDate ?? DateTime.Now;
                    preview.Articles = await InitPDFPreviewArticles(ctx, order, jobs);
                    preview.OutputFile = tempSrv.GetTempFileName(true);
                }

                var result = await pdfSrv.CreateOrderPreviewAsync(preview);
                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var category = file.GetAttachmentCategory("Documents");
                if (!category.TryGetAttachment($"OrderPreview_{orderid}.pdf", out var attachment))
                    attachment = category.CreateAttachment($"OrderPreview_{orderid}.pdf");
                attachment.SetContent(preview.OutputFile);
                return attachment.FileGUID;
            }
            finally
            {
                slock.Dispose();
            }
        }

        /// <summary>
        /// Checks to see if the production sheet document for the specified order already exists, if it exists then it also returns the path to the document.
        /// </summary>
        public bool ProdSheetDocumentExists(int orderid, out Guid documentGUID)
        {
            documentGUID = Guid.Empty;
            if (!orderStore.TryGetFile(orderid, out var file))
                return false;
            var category = file.GetAttachmentCategory("Documents");
            if (!category.TryGetAttachment($"ProdSheet_{orderid}.pdf", out var attachment))
                return false;
            documentGUID = attachment.FileGUID;
            return true;
        }

        /// <summary>
        /// Gets the path to the production sheet document (if available) if the preview is being generated in another process then it waits for up to 
        /// 30 seconds for it to complete before throwing a timeout error.
        /// </summary>
        /// <param name="orderid">The Order ID</param>
        /// <returns>The path to the document</returns>
        public IFSFile GetProdSheetDocument(int orderid)
        {
            if (!orderStore.TryGetFile(orderid, out var file))
                throw new Exception($"Could not find order file in the order file store (OrderID: {orderid})");

            if (!SharedLock.TryAcquire($"ProdSheet_{orderid}", TimeSpan.FromSeconds(30), out var slock))
                throw new Exception("Timeout while waiting for the document to be available.");

            try
            {
                var category = file.GetAttachmentCategory("Documents");
                if (!category.TryGetAttachment($"ProdSheet_{orderid}.pdf", out var attachment))
                    throw new Exception("The requested document could not be found.");
                return attachment;
            }
            finally
            {
                slock.Dispose();
            }
        }

        public IFSFile GetOrderDetailDocument(int orderid)
        {
            if (!orderStore.TryGetFile(orderid, out var file))
                throw new Exception($"Could not find order file in the order file store (OrderID: {orderid})");

            if (!SharedLock.TryAcquire($"ProdSheet_{orderid}", TimeSpan.FromSeconds(30), out var slock))
                throw new Exception("Timeout while waiting for the document to be available.");

            try
            {
                var category = file.GetAttachmentCategory("Documents");
                if (!category.TryGetAttachment($"OrderDetail_{orderid}.pdf", out var attachment))
                    throw new Exception("The requested document could not be found.");
                return attachment;
            }
            finally
            {
                slock.Dispose();
            }
        }

        /// <summary>
        /// Creates the order production sheet document and attaches it to the order in the Documents attachment category.
        /// </summary>
        /// <param name="orderid">ID of the order</param>
        /// <returns>Returns the path to the created document</returns>
        public async Task<Guid> CreateProdSheetDocument(int orderid)
        {
            if (!orderStore.TryGetFile(orderid, out var file))
                throw new Exception($"Could not find order file in the order file store (OrderID: {orderid})");

            if (!SharedLock.TryAcquire($"ProdSheet_{orderid}", TimeSpan.FromMilliseconds(1), out var slock))
                return Guid.Empty;

            try
            {
                PDFProdSheet prodSheet = new PDFProdSheet();
                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    var order = ctx.CompanyOrders.Where(o => o.ID == orderid).Single();
                    if (order.ProductionType == ProductionType.CustomerLocation)
                        return Guid.Empty;
                    var company = ctx.Companies.Where(c => c.ID == order.CompanyID).Single();
                    var companyProvider = order.ProviderRecordID != null ? providerRepo.GetByID(order.ProviderRecordID.Value) : null;
                    var provider = companyProvider != null ? ctx.Companies.Where(c => c.ID == companyProvider.ProviderCompanyID).Single() : null;
                    
                    var project = ctx.Projects.Where(p => p.ID == order.ProjectID).Single();
                    var brand = ctx.Brands.Where(b => b.ID == project.BrandID).Single();
                    var location = ctx.Locations.Where(b => b.ID == order.LocationID).Single();
                    var jobs = ctx.PrinterJobs.Where(j => j.CompanyOrderID == orderid).ToList();
                    var group = ctx.OrderGroups.FirstOrDefault(g => g.ID == order.OrderGroupID);

                    prodSheet.OrderNumber = order.OrderNumber;
                    prodSheet.SageReference = string.Empty;
                    prodSheet.MDOrderNumber = order.MDOrderNumber;
                    prodSheet.CompanyName = company.Name;
                    prodSheet.FileName = file.FileName;
                    prodSheet.ClientReference = companyProvider != null ? companyProvider.ClientReference : null;
                    prodSheet.Provider = provider != null ? provider.Name : null;
                    prodSheet.BrandName = brand.Name;
                    prodSheet.ProjectName = project.Name;
                    prodSheet.Location = location.Name;
                    prodSheet.OrderDate = order.OrderDate;
                    prodSheet.ValidationDate = order.ValidationDate ?? DateTime.Now;
                    prodSheet.OutputFile = tempSrv.GetTempFileName(true);
                    prodSheet.Columns = GetProdSheetColumns(ctx, order, jobs);
                    prodSheet.Rows = GetProdSheetRows(ctx, order, jobs);
                    prodSheet.Previews = GetProdSheetPreviews(ctx, order, jobs);
                    prodSheet.OrderID = order.ID;
                    prodSheet.ShippingInstructions = companyProvider?.Instructions; 
                }

                var result = await pdfSrv.CreateProductionSheetAsync(prodSheet);
                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var category = file.GetAttachmentCategory("Documents");
                if (!category.TryGetAttachment($"ProdSheet_{orderid}.pdf", out var attachment))
                    attachment = category.CreateAttachment($"ProdSheet_{orderid}.pdf");
                attachment.SetContent(prodSheet.OutputFile);
                return attachment.FileGUID;
            }
            finally
            {
                slock.Dispose();
            }
        }



        public async Task<Guid> CreateOrderDetailDocument(int orderid)
        {
            if (!orderStore.TryGetFile(orderid, out var file))
                throw new Exception($"Could not find order file in the order file store (OrderID: {orderid})");

            if (!SharedLock.TryAcquire($"OrderDetail_{orderid}", TimeSpan.FromMilliseconds(1), out var slock))
                return Guid.Empty;

            try
            {
                var prodSheet = new PDFOrderDetail();
                using (var ctx = factory.GetInstance<PrintDB>())
                {
                    var order = ctx.CompanyOrders.Where(o => o.ID == orderid).Single();
                    if (order.ProductionType == ProductionType.CustomerLocation)
                        return Guid.Empty;
                    var company = ctx.Companies.Where(c => c.ID == order.CompanyID).Single();
                    var companyProvider = order.ProviderRecordID != null ? providerRepo.GetByID(order.ProviderRecordID.Value) : null;
                    var provider = companyProvider != null ? ctx.Companies.Where(c => c.ID == companyProvider.ProviderCompanyID).Single() : null;
                    var project = ctx.Projects.Where(p => p.ID == order.ProjectID).Single();
                    var brand = ctx.Brands.Where(b => b.ID == project.BrandID).Single();
                    var location = ctx.Locations.Where(b => b.ID == order.LocationID).Single();
                    var jobs = ctx.PrinterJobs.Where(j => j.CompanyOrderID == orderid).ToList();
                    var group = ctx.OrderGroups.FirstOrDefault(g => g.ID == order.OrderGroupID);

                    prodSheet.OrderNumber = order.OrderNumber;
                    prodSheet.SageReference = string.Empty;
                    prodSheet.MDOrderNumber = order.MDOrderNumber;
                    prodSheet.CompanyName = company.Name;
                    prodSheet.FileName = file.FileName;
                    prodSheet.ClientReference = companyProvider != null ? companyProvider.ClientReference : null;
                    prodSheet.Provider = provider != null ? provider.Name : null;
                    prodSheet.BrandName = brand.Name;
                    prodSheet.ProjectName = project.Name;
                    prodSheet.Location = location.Name;
                    prodSheet.OrderDate = order.OrderDate;
                    prodSheet.ValidationDate = order.ValidationDate ?? DateTime.Now;
                    prodSheet.OutputFile = tempSrv.GetTempFileName(true);
                    prodSheet.Columns = GetOrderDetailColumns(ctx, order, jobs);
                    prodSheet.Rows = GetOrderDetailRows(ctx, order, jobs);
                    prodSheet.Previews = GetOrderDetailPreviews(ctx, order, jobs);
                }

                var result = await pdfSrv.CreateOrderDetailAsync(prodSheet);
                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                var category = file.GetAttachmentCategory("Documents");
                if (!category.TryGetAttachment($"OrderDetail_{orderid}.pdf", out var attachment))
                    attachment = category.CreateAttachment($"OrderDetail_{orderid}.pdf");
                attachment.SetContent(prodSheet.OutputFile);
                return attachment.FileGUID;
            }
            finally
            {
                slock.Dispose();
            }
        }


        /// <summary>
        /// Invalidates any cached images from the specified order
        /// </summary>
        /// <param name="orderid">ID of the order</param>
        public async Task InvalidateCache(int orderid)
        {
            var rq = new LabelCacheInvalidationRequest();
            var labelsIDs = new List<int>();

            using (var ctx = factory.GetInstance<PrintDB>())
            {
                rq.DetailIDs = (from a in ctx.PrinterJobDetails
                                join b in ctx.PrinterJobs on a.PrinterJobID equals b.ID
                                join c in ctx.CompanyOrders on b.CompanyOrderID equals c.ID
                                where c.ID == orderid
                                select a.ProductDataID).ToList();

                labelsIDs = (from a in ctx.PrinterJobs
                             join b in ctx.Articles on a.ArticleID equals b.ID
                             join c in ctx.Labels on b.LabelID equals c.ID
                             where a.CompanyOrderID == orderid
                             select c.ID).ToList();
            }

            foreach (var labelid in labelsIDs)
            {
                rq.LabelID = labelid;
                await labelService.InvalidateCache(rq);
            }
        }


        class UnitInfo
        {
            public int Quantity;
            public int DetailDataID;
            public string GroupingColumnValue;
        }


        private async Task<List<PDFOrderPreviewArticle>> InitPDFPreviewArticles(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<PDFOrderPreviewArticle>();
            var variableDataCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == Catalog.VARIABLEDATA_CATALOG).Single();
            var detailsCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == Catalog.ORDERDETAILS_CATALOG).Single();
            foreach (var job in jobs)
            {
                LabelData label = null;
                string groupingColumn = "";
                int rows = 3;
                int cols = 3;
                var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();

                var artifacts = ctx.Artifacts.Where(a => a.ArticleID == article.ID && a.EnablePreview == true).ToList();

                var articlePreviewSettings = ctx.ArticlePreviewSettings.FirstOrDefault(p => p.ArticleID == article.ID);
                if (articlePreviewSettings != null)
                {
                    rows = articlePreviewSettings.Rows;
                    cols = articlePreviewSettings.Cols;
                }
                if (article.LabelID.HasValue)
                {
                    label = ctx.Labels.Where(l => l.ID == article.LabelID).Single();
                    groupingColumn = GetGroupingColumn(label.GroupingFields);
                }
                var pdfArticle = new PDFOrderPreviewArticle()
                {
                    Name = article.Name,
                    ArticleCode = article.ArticleCode,
                    TotalQuantity = job.Quantity,
                    GroupingField = groupingColumn,
                    Rows = rows,
                    Cols = cols
                };
                if (label != null)
                {
                    using (var conn = db.OpenWebLinkDB())
                    {
                        var units = conn.Select<UnitInfo>($@"
						select Sum(a.Quantity) as Quantity, Max(a.ProductDataID) as DetailDataID, c.{groupingColumn} as GroupingColumnValue
						from PrinterJobDetails a
							join {db.CatalogDB}.dbo.{detailsCatalog.TableName} b on a.ProductDataID = b.ID
							join {db.CatalogDB}.dbo.{variableDataCatalog.TableName} c on b.Product = c.ID
						where a.PrinterJobID = {job.ID}
                        and a.Quantity > 0
						group by c.{groupingColumn}
						");
                        pdfArticle.Units = await InitPDFArticleUnits(label.ID, order.ID, units, artifacts);
                    }
                }
                else
                {
                    pdfArticle.Units = InitStockArticleUnits(article, job.Quantity);
                }
                result.Add(pdfArticle);
            }
            return result;
        }

        private List<PDFArticleUnit> InitStockArticleUnits(Article article, int quantity)
        {
            var result = new List<PDFArticleUnit>();
            result.Add(new PDFArticleUnit()
            {
                Text = article.Name,
                Quantity = quantity,
                PreviewFileGUID = articleRepo.GetArticlePreviewReference(article.ID)
            });
            return result;
        }


        private async Task<List<PDFArticleUnit>> InitPDFArticleUnits(int labelid, int orderid, List<UnitInfo> units, List<Artifact> artifacts)
        {
            var result = new List<PDFArticleUnit>();
            var map = new List<MapPreviewConfig>();

            if (units.Count > MaxPreviewUnits)
            {
                log.LogWarning($"Preview for order {orderid}, Label {labelid} will not include all units because it is over the max limit of units ({MaxPreviewUnits}/{units.Count}). Review the label Display Fields and Grouping Field to ensure it is properly setup.");
            }

            Action<List<MapPreviewConfig>> AddArticleUnitToResult = (currentReadyMap) =>
            {
                try
                {
                    foreach (var taskResult in currentReadyMap.Where(w => w.IsArtifact == false))
                    {
                        if (taskResult.Task.Exception != null)
                            throw taskResult.Task.Exception; // problem to generate main label preview

                        var pdfUnit = new PDFArticleUnit();
                        pdfUnit.PreviewFileGUID = ((Task<Guid>)taskResult.Task).Result;
                        pdfUnit.Quantity = taskResult.Unit.Quantity;
                        pdfUnit.Text = taskResult.Unit.GroupingColumnValue;

                        // add artifacts
                        pdfUnit.Artifacts = new List<PDFOrderPreviewArtifact>();
                        var artifactMaps = currentReadyMap.Where(w => w.IsArtifact == true && w.Unit.DetailDataID == taskResult.Unit.DetailDataID);

                        foreach (var atm in artifactMaps)
                        {
                            if (atm.Task.Exception != null)
                                throw atm.Task.Exception; // problem to generate artifact preview

                            pdfUnit.Artifacts.Add(new PDFOrderPreviewArtifact()
                            {
                                Name = atm.ArtifactName,
                                PreviewFileGUID = ((Task<Guid>)atm.Task).Result
                            });

                        }

                        result.Add(pdfUnit);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    map.Clear();
                }
            };

            foreach (var unit in units.Take(MaxPreviewUnits))
            {

                try
                {
                    var t = GetArticlePreviewFileReference(labelid, orderid, unit.DetailDataID);
                    map.Add(new MapPreviewConfig { Task = t, Unit = unit });

                    // add artifacts
                    foreach (var artifact in artifacts)
                    {
                        if (artifact.LabelID.HasValue && artifact.EnablePreview)
                        {
                            var tArtifact = GetArticlePreviewFileReference(artifact.LabelID.Value, orderid, unit.DetailDataID);
                            //fUnit.Artifacts.Add(new PDFOrderPreviewArtifact() { Name = artifact.Name, PreviewFileGUID = artifactPreviewGUID });
                            map.Add(new MapPreviewConfig { Task = tArtifact, Unit = unit, IsArtifact = true, ArtifactName = artifact.Name });
                        }
                    }

                }
                catch (Exception ex)
                {
                    log.LogException($"Error while creating Unit preview for: OrderID {orderid}, LabelID: {labelid}, DetailID: {unit.DetailDataID}", ex);
                    throw;
                }

                if (map.Count >= 8)
                {
                    await Task.WhenAll(map.Select(m => m.Task).ToArray());
                    AddArticleUnitToResult(map);
                }

            }

            if (map.Count > 0)
            {
                await Task.WhenAll(map.Select(m => m.Task).ToArray());
                AddArticleUnitToResult(map);
            }

            return result;
        }

        private async Task<Guid> GetArticlePreviewFileReference(int labelid, int orderid, int detailDataID)
        {
            int retryCount = 1;
            IFSFile file = null;
            var fileguid = await labelRepo.GetArticlePreviewReferenceAsync(labelid, orderid, detailDataID);

            if (fileguid != Guid.Empty)
                file = await storeManager.GetFileAsync(fileguid);

            while (retryCount < 3 && (file == null || file.FileSize == 0))
            {
                retryCount++;
                await labelService.InvalidateCache(new LabelCacheInvalidationRequest()
                {
                    LabelID = labelid,
                    DetailIDs = new List<int>() { detailDataID }
                });

                fileguid = await labelRepo.GetArticlePreviewReferenceAsync(labelid, orderid, detailDataID);

                if (fileguid != Guid.Empty)
                    file = await storeManager.GetFileAsync(fileguid);
            }
            return fileguid;
        }

        class GroupingColumnInfo
        {
            public string GroupingFields;
            public string DisplayFields;
            public string OrderFields;    
        }


        private string GetGroupingColumn(string groupingFields, bool isItem = false)
        {
            // NOTE: Any error at this point is due to missconfiguration, to fix the problem fix the system configuration, then reprocess the print package. Until that is done, this bit of code will keep throwing and prevent the order from moving to the Pending state...
            if (String.IsNullOrWhiteSpace(groupingFields))
                return isItem ? "PackCode" : "Barcode";
            var grouping = JsonConvert.DeserializeObject<GroupingColumnInfo>(groupingFields);
            if (String.IsNullOrWhiteSpace(grouping.GroupingFields))
                return isItem ? "PackCode" : "Barcode";
            string[] tokens = grouping.GroupingFields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return tokens[0];
        }

        private string GetOrderByColumn(string groupingFields, bool isItem = false)
        {
            // NOTE: Any error at this point is due to missconfiguration, to fix the problem fix the system configuration, then reprocess the print package. Until that is done, this bit of code will keep throwing and prevent the order from moving to the Pending state...
            if(String.IsNullOrWhiteSpace(groupingFields))
                return isItem ? "PackCode" : "Barcode";
            var grouping = JsonConvert.DeserializeObject<GroupingColumnInfo>(groupingFields);
            if(String.IsNullOrWhiteSpace(grouping.OrderFields))
                return isItem ? "PackCode" : "Barcode";
            string[] tokens = grouping.OrderFields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return tokens[0];
        }

        private List<ProdSheetColumn> GetProdSheetColumns(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<ProdSheetColumn>();
            var columns = new List<string>();
            foreach (var job in jobs)
            {
                var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();
                if (article.LabelID.HasValue)
                {
                    var label = ctx.Labels.Where(l => l.ID == article.LabelID).Single();
                    columns = GetProdSheetFields(label.GroupingFields);
                }
                else
                {
                    var jobDetail = ctx.PrinterJobDetails.FirstOrDefault(x => x.PrinterJobID == job.ID);
                    var isPack = !string.IsNullOrWhiteSpace(jobDetail.PackCode);
                    columns = GetProdSheetFields(string.Empty, isPack);
                }


                foreach (var column in columns)
                {
                    result.Add(new ProdSheetColumn(column, column));
                }

                break;
            }

            return result;
        }

        /// <summary>
        /// ijsanchezm
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="order"></param>
        /// <param name="jobs"></param>
        /// <returns></returns>
        private List<OrderDetailColumn> GetOrderDetailColumns(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<OrderDetailColumn>
            {
                new OrderDetailColumn("REF.","REF."),
                new OrderDetailColumn("ARTICLE CODE","ARTICLE CODE"),
                new OrderDetailColumn("DESCRIPTION","DESCRIPTION"),
                new OrderDetailColumn("QUANTITY","QUANTITY"),
                new OrderDetailColumn("SIZE","SIZE"),
                new OrderDetailColumn("COLOR","COLOR")
            };
            //var columns = new List<string>();
            //foreach (var job in jobs)
            //{
            //    var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();
            //    if (article.LabelID.HasValue)
            //    {
            //        var label = ctx.Labels.Where(l => l.ID == article.LabelID).Single();
            //        columns = GetProdSheetFields(label.GroupingFields);
            //    }
            //    else
            //    {
            //        var jobDetail = ctx.PrinterJobDetails.FirstOrDefault(x => x.PrinterJobID == job.ID);
            //        var isPack = !string.IsNullOrWhiteSpace(jobDetail.PackCode);
            //        columns = GetProdSheetFields(string.Empty, isPack);
            //    }


            //    foreach (var column in columns)
            //    {
            //        result.Add(new OrderDetailColumn(column, column));
            //    }

            //    break;
            //}

            return result;
        }

        private string GetDisplayFields(string displayFields, bool isItem = false)
        {
            if (String.IsNullOrWhiteSpace(displayFields))
                return isItem ? "TXT1,TXT2,TXT3" : "Size,TXT1,TXT2";
            var fields = JsonConvert.DeserializeObject<GroupingColumnInfo>(displayFields);
            if (String.IsNullOrWhiteSpace(fields.DisplayFields))
                return isItem ? "TXT1,TXT2,TXT3" : "Size,TXT1,TXT2";
            return fields.DisplayFields;
        }

        private List<ProdSheetArticle> GetProdSheetRows(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<ProdSheetArticle>();
            var variableDataCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "VariableData").Single();
            var detailsCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "OrderDetails").Single();

            foreach (var job in jobs)
            {
                string groupingColumn = "";
                string orderByColumn = "";
                var columns = new List<string>();
                var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();
                var jobDetail = ctx.PrinterJobDetails.FirstOrDefault(x => x.PrinterJobID == job.ID);
                var isPack = !string.IsNullOrWhiteSpace(jobDetail.PackCode);

                var label = ctx.Labels.FirstOrDefault(l => l.ID == article.LabelID);
                if (label != null)
                {
                    columns = GetProdSheetFields(label.GroupingFields);
                    groupingColumn = GetGroupingColumn(label.GroupingFields);
                    orderByColumn =  GetOrderByColumn(label.GroupingFields); 
                }
                else
                {
                    columns = GetProdSheetFields(string.Empty, isPack);
                    groupingColumn = GetGroupingColumn(string.Empty, isPack);
                    orderByColumn = GetOrderByColumn(string.Empty);
                }

                using (var conn = db.OpenWebLinkDB())
                {
                    string query = string.Empty;
                    foreach (var column in columns)
                    {
                        query += label == null && column == "PackCode" ? $"" : $", Max(c.{column}) {column} ";
                    }
                    var groupBy = label != null || !isPack ? $" c.{groupingColumn}" : $" a.PackCode";
                    var orderBy = label != null || !isPack ? $" c.{orderByColumn}" : $" a.PackCode";

                    var rowData = conn.SelectToJson($@"
                    select Max(a.PackCode) PackCode, Max(ar.ArticleCode) ArticleCode, Max(ar.Description) Description, Sum(a.Quantity) as Quantity, Max(l.Rows) Rows, Max(l.Cols) Cols {query}
                    from PrinterJobDetails a
                        join PrinterJobs j on a.PrinterJobID = j.ID
                        join Articles ar on j.ArticleID = ar.ID
                        left join Labels l on l.ID = ar.LabelID
                	    join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
                	    join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
                    where a.PrinterJobID = {job.ID} 
                    and a.PackCode is not null
					group by {groupBy}
                    order by Max({orderBy})
                    ");

                    var articles = conn.SelectToJson($@"
                        select Max(ar.ArticleCode) ArticleCode, Max(ar.Description) Description, Sum(a.Quantity) as Quantity, Max(l.Rows) Rows, Max(l.Cols) Cols {query}
                        from PrinterJobDetails a
                            join PrinterJobs j on a.PrinterJobID = j.ID
                            join Articles ar on j.ArticleID = ar.ID
                            left join Labels l on l.ID = ar.LabelID
                	        join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
                	        join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
                        where a.PrinterJobID = {job.ID} 
                        and a.PackCode is null
						group by {groupBy}
                        order by Max({orderBy})
                    ");

                    if (rowData.Count > 0)
                    {
                        //var prodSheetArticle = new ProdSheetArticle();
                        //prodSheetArticle.IsPack = true;
                        //foreach (JObject row in rowData)
                        //{
                        //    var packCode = row.GetValue<string>("PackCode");
                        //    var description = row.GetValue<string>("Description");
                        //    var quantity = row.GetValue<string>("Quantity");
                        //    string[] rowColumns = new string[columns.Count + 1];
                        //    rowColumns[0] = description;
                        //    for (int index = 1; index < rowColumns.Length; index++)
                        //    {
                        //        rowColumns[index] = row.GetValue<string>(columns[index - 1]);
                        //    }
                        //    prodSheetArticle.RowData = new ProdSheetRow(packCode, int.Parse(quantity), 0, 0, 0, rowColumns);
                        //    //result.Add(prodSheetArticle);
                        //}

                        //if (articles.Count > 0)
                        //{
                        //var packArticles = new List<ProdSheetRow>();
                        //foreach (JObject row in rowData)
                        //{
                        //    packArticles.Add(GetRowData(row, columns));
                        //}
                        //prodSheetArticle.PackArticles = packArticles;
                        //}

                        //result.Add(prodSheetArticle);

                        foreach (JObject row in rowData)
                        {
                            var data = new ProdSheetArticle();
                            data.RowData = GetRowData(row, columns);
                            result.Add(data);
                        }

                        result = result.OrderBy(x => x.RowData.PackCode).ToList();

                    }
                    else
                    {
                        if (articles.Count > 0)
                        {
                            foreach (JObject row in articles)
                            {
                                var data = new ProdSheetArticle();
                                data.RowData = GetRowData(row, columns);
                                result.Add(data);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private List<OrderDetailArticle> GetOrderDetailRows(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<OrderDetailArticle>();
            var variableDataCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "VariableData").Single();
            var detailsCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "OrderDetails").Single();

            foreach (var job in jobs)
            {
                //string groupingColumn = "";
                var columns = "ID,BillingCode,ArticleCode,Description,Quantity,PackCode,Size,Color".Split(",").ToList();
                var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();
                var jobDetail = ctx.PrinterJobDetails.FirstOrDefault(x => x.PrinterJobID == job.ID);
                var isPack = !string.IsNullOrWhiteSpace(jobDetail.PackCode);

                //var label = ctx.Labels.FirstOrDefault(l => l.ID == article.LabelID);
                //if (label != null)
                //{
                //    columns = GetProdSheetFields(label.GroupingFields);
                //    groupingColumn = GetGroupingColumn(label.GroupingFields);
                //}
                //else
                //{
                //    columns = GetProdSheetFields(string.Empty, isPack);
                //    groupingColumn = GetGroupingColumn(string.Empty, isPack);
                //}


                var artifactlst = artifactRepo.GetByArticle(article.ID).Where(artifact => artifact.EnablePreview == true);



                using (var conn = db.OpenWebLinkDB())
                {
                    //string query = string.Empty;
                    //foreach (var column in columns)
                    //{
                    //    query += label == null && column == "PackCode" ? $"" : $", Max(c.{column}) {column} ";
                    //}
                    //var groupBy = label != null || !isPack ? $" c.{groupingColumn}" : $" a.PackCode";

                    this.log.LogMessage("Values of Order Details Document");
                    this.log.LogMessage($"db.CatalogDB: {db.CatalogDB}  -   detailsCatalog.CatalogID {detailsCatalog.CatalogID}   -   variableDataCatalog.CatalogID {variableDataCatalog.CatalogID}   -   job.ID {job.ID}");

     //               var rowData = conn.SelectToJson($@"select Max(ar.ID) ID,Max(ar.BillingCode) BillingCode,  Max(ar.ArticleCode) ArticleCode, 
     //                           MAX(ar.Description) Description, convert (decimal(10,3),convert(decimal, a.Quantity)/1000) as Quantity,Max(a.PackCode) PackCode
     //                           , c.Size, c.Color
     //               from PrinterJobDetails a
     //                   join PrinterJobs j on a.PrinterJobID = j.ID
     //                   join Articles ar on j.ArticleID = ar.ID
     //                   left join Labels l on l.ID = ar.LabelID
     //           	    join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
     //           	    join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
     //               where a.PrinterJobID = {job.ID} 
     //               and a.PackCode is not null
					//group by a.Quantity, a.PackCode, c.Size, c.Color
     //               ");

                    var articles = conn.SelectToJson($@"select Max(ar.ID) ID,Max(ar.BillingCode) BillingCode,  Max(ar.ArticleCode) ArticleCode, MAX(ar.Description) Description, 
                                convert (decimal(10,3),convert(decimal, a.Quantity)/1000) as Quantity,Max(a.PackCode) PackCode
                                , c.Size, c.Color 
                        from PrinterJobDetails a
                            join PrinterJobs j on a.PrinterJobID = j.ID
                            join Articles ar on j.ArticleID = ar.ID
                            left join Labels l on l.ID = ar.LabelID
                	        join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
                	        join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
                        where a.PrinterJobID = {job.ID} 
                    
						group by a.Quantity, a.PackCode, c.Size, c.Color
                        order by a.PackCode desc
                    ");

                    List<OrderDetailArticle> artifactData = new List<OrderDetailArticle>();

                    //if (rowData.Count > 0)
                    //{
                    //    foreach (JObject row in rowData)
                    //    {
                    //        var data = new OrderDetailArticle();
                    //        data.RowData = GetRowDataOrderDetail(row, columns);
                    //        result.Add(data);
                    //    }

                    //    foreach (var artifact in artifactlst)
                    //    {
                    //        var data = new OrderDetailArticle();
                    //        decimal total = 0;

                    //        foreach (JObject row in rowData)
                    //        {
                    //            data.RowData = GetRowDataOrderDetail(row, columns);

                    //            data.RowData.Description = string.IsNullOrWhiteSpace(artifact.Description) ? "Artifact Description is Empty" : artifact.Description;

                    //            data.RowData.ColumnData[1] = artifact.SageRef;
                    //            data.RowData.ColumnData[3] = string.IsNullOrWhiteSpace(artifact.Description) ? "Artifact Description is Empty" : artifact.Description;
                    //            data.RowData.ColumnData[6] = null; 


                    //            total += row.GetValue<decimal>("Quantity");
                    //        }

                    //        data.RowData.ColumnData[4] = total.ToString();
                    //        data.RowData.TotalQuantity = total.ToString();

                    //        result.Add(data);
                    //    }

                    //    result = result.OrderBy(x => x.RowData.PackCode).ToList();
                    //}
                    //else
                    //{
                        if (articles.Count > 0)
                        {
                            foreach (JObject row in articles)
                            {
                                var data = new OrderDetailArticle();
                                data.RowData = GetRowDataOrderDetail(row, columns);
                                result.Add(data);
                            }

                            foreach (var artifact in artifactlst)
                            {
                                var data = new OrderDetailArticle();
                                decimal total = 0;
                                var desc = string.IsNullOrWhiteSpace(artifact.Description) ? "Artifact Description is Empty" : artifact.Description;
                                var billingcode = string.IsNullOrWhiteSpace(artifact.SageRef) ? string.Empty : artifact.SageRef;

                                foreach (JObject row in articles)
                                {
                                    total += row.GetValue<decimal>("Quantity");
                                }

                                JObject artifactrow = ((JObject)articles[0]).DeepClone() as JObject;

                                artifactrow["Description"] = desc;
                                artifactrow["Quantity"]= total;
                                artifactrow["Size"] = string.Empty;
                                artifactrow["BillingCode"] = billingcode;

                                data.RowData = GetRowDataOrderDetail(artifactrow, columns);

                                //data.RowData.ColumnData[1] = artifact.SageRef; //BillingCode
                                //data.RowData.ColumnData[3] = desc; //Description
                                //data.RowData.ColumnData[4] = total.ToString();
                                //data.RowData.ColumnData[6] = null; //Size

                                result.Add(data);
                            }
                        }
                    //}
                }
            }

            return result;
        }


        //   private void RowData(IDBX conn,ICatalog detailsCatalog,ICatalog variableDataCatalog,IPrinterJob job,IArticle article,string groupBy)
        //   {

        //       if (article.LabelID.HasValue)
        //       {
        //           var rowData = conn.SelectToJson($@"
        //               select Max(a.PackCode) PackCode, Max(ar.ArticleCode) ArticleCode, Max(ar.Description) Description, Sum(a.Quantity) as Quantity, Max(l.Rows) Rows, Max(l.Cols) Cols {query}
        //               from PrinterJobDetails a
        //                   join PrinterJobs j on a.PrinterJobID = j.ID
        //                   join Articles ar on j.ArticleID = ar.ID
        //                   left join Labels l on l.ID = ar.LabelID
        //           	    join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
        //           	    join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
        //               where a.PrinterJobID = {job.ID} 
        //               and a.PackCode is not null
        //group by {groupBy}
        //               ");
        //       }
        //       else
        //       {
        //           var rowData = conn.SelectToJson($@"
        //               select Max(a.PackCode) PackCode, Max(ar.ArticleCode) ArticleCode, Max(ar.Description) Description, Sum(a.Quantity) as Quantity, Max(l.Rows) Rows, Max(l.Cols) Cols {query}
        //               from PrinterJobDetails a
        //                   join PrinterJobs j on a.PrinterJobID = j.ID
        //                   join Articles ar on j.ArticleID = ar.ID
        //                   left join Labels l on l.ID = ar.LabelID
        //           	    join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
        //           	    join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
        //               where a.PrinterJobID = {job.ID} 
        //               and a.PackCode is not null
        //group by {groupBy}
        //               ");
        //       }


        //   }

        private ProdSheetRow GetRowData(JObject row, List<string> columns)
        {
            var articleCode = row.GetValue<string>("ArticleCode");
            var packCode = columns.Contains("PackCode") ? string.Empty : row.GetValue<string>("PackCode");
            var description = row.GetValue<string>("Description");
            var quantity = row.GetValue<string>("Quantity");
            var rows = row.GetValue<string>("Rows") != null ? row.GetValue<int>("Rows") : 0;
            var cols = row.GetValue<string>("Cols") != null ? row.GetValue<int>("Cols") : 0;
            var pages = 0;
            if (rows > 0 && cols > 0)
            {
                pages = (int)Math.Ceiling(double.Parse(quantity) / (rows * cols));
            }

            string[] rowColumns = new string[columns.Count + 1];
            rowColumns[0] = description;
            for (int index = 1; index < rowColumns.Length; index++)
            {
                rowColumns[index] = row.GetValue<string>(columns[index - 1]);
            }

            return new ProdSheetRow(packCode, articleCode, int.Parse(quantity), cols, rows, pages, rowColumns.ToList());
        }


        private OrderDetailRow GetRowDataOrderDetail(JObject row, List<string> columns)
        {
            var articleCode = row.GetValue<string>("ArticleCode");
            var billingCode = row.GetValue<string>("BillingCode");
            var description = row.GetValue<string>("Description");
            var quantity = row.GetValue<string>("Quantity");
            var packCode = columns.Contains("PackCode") ? string.Empty : row.GetValue<string>("PackCode");
            var rows = row.GetValue<string>("Rows") != null ? row.GetValue<int>("Rows") : 0;
            var cols = row.GetValue<string>("Cols") != null ? row.GetValue<int>("Cols") : 0;
            var pages = 0;
            if (rows > 0 && cols > 0)
            {
                pages = (int)Math.Ceiling(double.Parse(quantity) / (rows * cols));
            }

            string[] rowColumns = new string[columns.Count];
            //rowColumns[0] = description;
            for (int index = 0; index < rowColumns.Length; index++)
            {
                rowColumns[index] = row.GetValue<string>(columns[index]);
            }

            return new OrderDetailRow(packCode, articleCode, description, 0, billingCode, quantity, rowColumns);
        }


        private List<ProdSheetArticlePreview> GetProdSheetPreviews(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<ProdSheetArticlePreview>();
            var variableDataCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "VariableData").Single();
            var detailsCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "OrderDetails").Single();
            foreach (var job in jobs)
            {
                var label = new LabelData();
                var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();

                label = ctx.Labels.FirstOrDefault(l => l.ID == article.LabelID);

                var labelFields = label != null ? label.GroupingFields : string.Empty;

                string groupingColumn = GetGroupingColumn(labelFields, label == null);
                var groupBy = groupingColumn == "PackCode" ? "a.PackCode" : $"c.{ groupingColumn}";

                var groupColumn = groupingColumn == "PackCode" ? $"Max(a.PackCode)" : $"Max(c.{ groupingColumn})";

                using (var conn = db.OpenWebLinkDB())
                {
                    var articles = conn.SelectToJson($@"
                        select Max(ar.ID) ID, Max(a.PackCode) PackCode, Max(ar.ArticleCode) ArticleCode, Max(ar.Description) Description, Sum(a.Quantity) as Quantity, Max(ar.Instructions) Instructions, 
                            Max(a.ProductDataID) ProductDataID, {groupColumn} as GroupingColumnValue
                        from PrinterJobDetails a
                            join PrinterJobs j on a.PrinterJobID = j.ID
                            join Articles ar on j.ArticleID = ar.ID
                	        join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
                	        join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
                        where a.PrinterJobID = {job.ID} 
						group by {groupBy}
						");


                    if (articles.Count > 0)
                    {
                        foreach (JObject row in articles)
                        {
                            var articleId = row.GetValue<int>("ID");
                            var packCode = row.GetValue<string>("PackCode");
                            var articleCode = row.GetValue<string>("ArticleCode");
                            var description = row.GetValue<string>("Description");
                            var quantity = row.GetValue<string>("Quantity");
                            var instructions = row.GetValue<string>("Instructions");
                            var groupingField = row.GetValue<string>("GroupingColumnValue");
                            var previewPath = articleRepo.GetArticlePreviewReference(articleId);

                            result.Add(new ProdSheetArticlePreview(packCode, articleCode, description, instructions, int.Parse(quantity), 0, previewPath, groupingColumn, groupingField));
                        }
                    }
                }
            }

            return result.OrderBy(x => x.PackCode).ToList();
        }


        private List<OrderDetailArticlePreview> GetOrderDetailPreviews(PrintDB ctx, Order order, List<PrinterJob> jobs)
        {
            var result = new List<OrderDetailArticlePreview>();
            var variableDataCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "VariableData").Single();
            var detailsCatalog = ctx.Catalogs.Where(c => c.ProjectID == order.ProjectID && c.Name == "OrderDetails").Single();
            foreach (var job in jobs)
            {
                var label = new LabelData();
                var article = ctx.Articles.Where(a => a.ID == job.ArticleID).Single();

                label = ctx.Labels.FirstOrDefault(l => l.ID == article.LabelID);

                var labelFields = label != null ? label.GroupingFields : string.Empty;

                string groupingColumn = GetGroupingColumn(labelFields, label == null);
                var groupBy = groupingColumn == "PackCode" ? "a.PackCode" : $"c.{ groupingColumn}";

                var groupColumn = groupingColumn == "PackCode" ? $"Max(a.PackCode)" : $"Max(c.{ groupingColumn})";

                using (var conn = db.OpenWebLinkDB())
                {
                    var articles = conn.SelectToJson($@"
                        select Max(ar.ID) ID,Max(ar.BillingCode) BillingCode,  Max(ar.ArticleCode) ArticleCode, MAX(ar.Description) Description
                                , convert (decimal(10,3),convert(decimal, a.Quantity)/1000) as Quantity  
                        from PrinterJobDetails a
                            join PrinterJobs j on a.PrinterJobID = j.ID
                            join Articles ar on j.ArticleID = ar.ID
                	        join {db.CatalogDB}.dbo.OrderDetails_{detailsCatalog.CatalogID} b on a.ProductDataID = b.ID
                	        join {db.CatalogDB}.dbo.VariableData_{variableDataCatalog.CatalogID} c on b.Product = c.ID
                        where a.PrinterJobID = {job.ID} 
						group by a.Quantity 
						");


                    if (articles.Count > 0)
                    {
                        foreach (JObject row in articles)
                        {
                            var articleId = row.GetValue<int>("ID");
                            //var packCode = row.GetValue<string>("PackCode");
                            var articleCode = row.GetValue<string>("ArticleCode");
                            var description = row.GetValue<string>("Description");
                            var quantity = row.GetValue<string>("Quantity");
                            //var instructions = row.GetValue<string>("Instructions");
                            //var groupingField = row.GetValue<string>("GroupingColumnValue");
                            //var color = row.GetValue<string>("Color");
                            //var size = row.GetValue<string>("Size");
                            var billingcode = row.GetValue<string>("BillingCode");

                            var previewPath = articleRepo.GetArticlePreviewReference(articleId);

                            result.Add(new OrderDetailArticlePreview(articleCode, description,0, billingcode, quantity));
                        }
                    }
                }
            }

            return result.OrderBy(x => x.PackCode).ToList();
        }


        private List<string> GetProdSheetFields(string fields, bool isItem = false)
        {
            string groupingColumn = GetGroupingColumn(fields, isItem);
            string displayColumns = GetDisplayFields(fields, isItem);
            var columns = displayColumns.Split(",").ToList();
            columns.Remove(groupingColumn);
            columns.Add(groupingColumn);
            return columns;
        }


        class MapPreviewConfig
        {
            public Task Task;
            public UnitInfo Unit;
            public bool IsArtifact;
            public List<Task> ArtifactTasks;
            public string ArtifactName;
        }
    }
}
