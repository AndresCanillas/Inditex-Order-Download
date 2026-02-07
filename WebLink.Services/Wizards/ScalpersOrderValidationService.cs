using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using Newtonsoft.Json;
using Services.Core;

namespace WebLink.Services.Wizards
{
    public class ScalpersOrderValidationService : IScalpersOrderValidationService
    {
        private IFactory factory;
        private IDBConnectionManager connManager;
        private IOrderRepository orderRepo;
        private IArticleRepository articleRepo;
        private IOrderGroupRepository groupRepo;
        private IPrinterJobRepository printerJobRepo;
        private IOrderSetValidatorService validatorSetterSrv;
        private ICatalogRepository catalogRepo;
        private ILogSection log;
        private IRemoteFileStore orderStore;

        // HARDCODE to Custom Article Categories, user can modify from UI, but will brake this script
        private const string LOGISTIC_CATEGORY_CODE = "LOG";
        private const string ADHESIVE_CATEGORY_CODE = "ADH";
        private const string CARDBOARD_CATEGORY_CODE = "CAR";
        private const string COMPOSITION_CATEGORY_CODE = "POL";

        public ScalpersOrderValidationService(
            IFactory factory,
            IDBConnectionManager connManager,
            IOrderRepository orderRepo,
            IArticleRepository articleRepo,
            IOrderGroupRepository groupRepo,
            IPrinterJobRepository printerJobRepo,
            IOrderSetValidatorService validatorSetterSrv,
            ICatalogRepository catalogRepo,
            ILogService log,
            IFileStoreManager storeManager
            )
        {
            this.factory = factory;
            this.connManager = connManager;
            this.orderRepo = orderRepo;
            this.articleRepo = articleRepo;
            this.groupRepo = groupRepo;
            this.printerJobRepo = printerJobRepo;
            this.validatorSetterSrv = validatorSetterSrv;
            this.catalogRepo = catalogRepo;
            this.log = log.GetSection("ScalpersValidator");
            this.orderStore = storeManager.OpenStore("OrderStore");
        }


        // looking for first article detail or EmptyArticle Detail
        public List<OrderGroupSelectionDTO> GetOrderData(PrintDB ctx, List<OrderGroupSelectionDTO> selection)
        {
            // only active orders, all articles
            var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Label, ActiveFilter = OrderActiveFilter.All, OrderStatus = OrderStatusFilter.InFlow };

            // for scalpers all articles use the same OrderData, the wizard only need details from one article
            //var cloneSelection = new List<OrderGroupSelectionDTO>();

            //selection.ForEach(e =>
            //{
                
            //    var orders = new List<int>() {  };// leave empty to get all orders for this group
            //    var itemSel = new OrderGroupSelectionDTO(e);
            //    itemSel.Details = new List<OrderDetailDTO>();
            //    itemSel.Orders = orders.ToArray();  
            //    cloneSelection.Add(itemSel);
            //});

            var result = orderRepo.GetArticleDetailSelection(ctx, selection, filter, true, new List<string> {
                "CodeGama", // fields only for scalpers
                "CodeSection",
                "MadeIn",
                "Season",
                "FullMadeIn",
                "ArticleLine"
            });

            // All orders inner selection must be contains the same OrderDataID


            // update order IDS from details in result

            result.ForEach(r =>
            {
                if(r.Details == null || r.Details.Count() < 1)
                {
                    return;
                }

                r.Orders = r.Details.OrderBy(a => a.ArticleID).ThenBy(b => b.PrinterJobDetailID).Select(s => s.OrderID).Distinct().ToArray();
                r.Details = r.Details.OrderBy(a => a.ArticleID).ThenBy(b => b.PrinterJobDetailID).ToList();

                r.Details.ForEach(d =>
                {
                    if (d.ProductData == null) return; // continue

                    if ( string.IsNullOrEmpty(d.ProductData.GetValue<string>("ArticleLine")))  {

                        var desArt = d.ProductData.GetValue<string>("TXT1");
                        // ADN wahs disabled at 202212
                        d.ProductData["ArticleLine"] = desArt.Contains("ROTO") ? "ROTO" : (/*desArt.StartsWith("ADN ") ? "SCALPERS" :*/ "SCALPERS");
                    }

                });



            });
            
            return result;
        }

        public List<OrderGroupSelectionDTO> GetOrderData(List<OrderGroupSelectionDTO> selection)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
                return GetOrderData(ctx, selection);
        }

        public bool IsEmptyOrder(int orderGroupID)
        {
            //var orderInfo = orderRepo.GetProjectInfo(orderID);

            var articleFilter = new OrderArticlesFilter() {
                ActiveFilter = OrderActiveFilter.All,
                OrderGroupID = orderGroupID
            };

            var articles = orderRepo.GetOrderArticles(articleFilter, ProductDetails.None).ToList();

            return IsEmptyOrder(articles);

        }

        public bool IsEmptyOrder(IEnumerable<OrderDetailDTO> articles)
        {
            // no articles or contain EMPTY_ARTICLE is a empty order
            if (articles.Count() == 0 || articles.Count(w => w.ArticleCode.Equals(Article.EMPTY_ARTICLE_CODE)) > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return new order un SelecteGroup to update wizard
        /// </summary>
        /// <param name="rq"></param>
        /// <returns></returns>
        public List<ScalpersOrderGroupQuantitiesDTO> UpdateOrders(List<ScalpersOrderGroupQuantitiesDTO> rq)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
                return UpdateOrders(ctx, rq);
        }

        private List<ScalpersOrderGroupQuantitiesDTO> UpdateOrders(PrintDB ctx, List<ScalpersOrderGroupQuantitiesDTO> rq)
        {
           
            foreach (var gp in rq)
            {
                //var articles = orderRepo.GetOrderArticles(ctx, new OrderArticlesFilter() {
                //    ActiveFilter = OrderActiveFilter.All,
                //    OrderNumber = string.Empty,
                //    OrderGroupID = gp.OrderGroupID,
                //    ArticleType = ArticleTypeFilter.Label
                //}).ToList();

                // actualizar todas las ordenes con las mismas cantidades del ArticleCode "0000"
                var articles = new List<OrderDetailDTO>();

                var orderArticles = ctx.CompanyOrders
                   .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { CompanyOrders = o, PrinterJobs = j })
                   .Join(ctx.Articles, join1 => join1.PrinterJobs.ArticleID, art => art.ID, (j1, a) => new { j1.CompanyOrders, j1.PrinterJobs, Articles = a })
                   .Join(ctx.OrderUpdateProperties, join2 => join2.CompanyOrders.ID, pp => pp.OrderID, (j2, props) => new { j2.CompanyOrders, j2.PrinterJobs, j2.Articles, OrderUpdateProperties = props })
                   .Where(w => w.CompanyOrders.OrderGroupID.Equals(gp.OrderGroupID))
                   .Where(w => w.Articles.LabelID.HasValue)
                   .Where(w => !w.OrderUpdateProperties.IsRejected)
                   .Select(s => new {
                       ArticleID = s.Articles.ID,
                       Article = s.Articles.Name,
                       ArticleCode = s.Articles.ArticleCode,
                       OrderID = s.CompanyOrders.ID,
                       OrderGroupID = s.CompanyOrders.OrderGroupID,
                       PrinterJobID = s.PrinterJobs.ID,
                       ProviderRecordID = s.CompanyOrders.ProviderRecordID,
                       OrderDataID = s.CompanyOrders.OrderDataID,
                       OrderStatus = s.CompanyOrders.OrderStatus
                       
                   }).ToList();


                orderArticles.ForEach(s => articles.Add(
                    new OrderDetailDTO
                    {
                        ArticleID = s.ArticleID,
                        Article = s.Article,
                        ArticleCode = s.ArticleCode,
                        OrderID = s.OrderID,
                        OrderGroupID = s.OrderGroupID,
                        PrinterJobID = s.PrinterJobID,
                        OrderDataID = s.OrderDataID,
                        OrderStatus = s.OrderStatus
                    }));

                var orderID = gp.Quantities.First().OrderID;// same order use from UI

                var groupInfo = groupRepo.GetBillingInfo(gp.OrderGroupID);

                var allArticles = articleRepo.GetArticlesInfo(groupInfo.ProjectID);

                // order selected from UI
                var uiOrder = orderRepo.GetByID(orderID);

                // always get Order for the Article.EMPTY_ARTICLE_CODE to get FILE_GUID
                var initialOrder = orderArticles.Find(f => f.ArticleCode == Article.EMPTY_ARTICLE_CODE && f.OrderDataID == uiOrder.OrderDataID);

                // Scalpers orders always are created with Article.EMPTY_ARTICLE_CODE
                var baseOrder = orderRepo.GetByID(initialOrder.OrderID);

                IRemoteFile file = orderStore.TryGetFileAsync(initialOrder.OrderID).GetAwaiter().GetResult();

                //Clonar AddArticleByCategory
                var hasLog = AddArticleByCategory(ctx, LOGISTIC_CATEGORY_CODE, gp.Logistic, gp, articles, baseOrder, allArticles, file.FileGUID);
                var hasAdh = AddArticleByCategory(ctx, ADHESIVE_CATEGORY_CODE, gp.Adhesive, gp, articles, baseOrder, allArticles, file.FileGUID);
                var hasCar = AddArticleByCategory(ctx, CARDBOARD_CATEGORY_CODE, gp.Cardboard, gp, articles, baseOrder, allArticles, file.FileGUID);
                var hasCompo = AddArticleByCategory(ctx, COMPOSITION_CATEGORY_CODE, gp.Composition, gp, articles, baseOrder, allArticles, file.FileGUID);
  
                // Set OrderData - Gamma, MadeIn
                SetVariableData(ctx, gp, baseOrder);
                AddOrRemoveEmptyArticle(ctx, baseOrder/*, articles*/, hasLog || hasAdh || hasCar || hasCompo);


                // XXX: this ordergroup always keep active, 
                // cancelations sometimes deactivate odergroup record for the nature of how to this script add or remove CompanyOrders to the current OrderGroup
                var orderGroup = groupRepo.GetByID(groupInfo.OrderGroupID);
                orderGroup.IsActive = true;
                groupRepo.Update(orderGroup);

                SetFactory(ctx, groupInfo, gp);
            }

            return rq;
        }

        private void SetFactory(PrintDB ctx, OrderInfoDTO groupInfo, ScalpersOrderGroupQuantitiesDTO sel)
        {
            /**
             * scalpers rules:
             * Section Woman -> send to Spain Factory -> Castellar [SDS01]
             * ROTO Collection -> send to Spain Factory -> Castellar [SDS01]
             * other case use default Provider location config
             */

            //var womanSections = new List<string>() { "woman", "mujer" };
            var articleCodes = new List<string>() {
                !string.IsNullOrEmpty(sel.Logistic) ? sel.Logistic : "no_selected",
                !string.IsNullOrEmpty(sel.Adhesive) ? sel.Adhesive : "no_selected",
                !string.IsNullOrEmpty(sel.Cardboard) ? sel.Cardboard : "no_selected",
                !string.IsNullOrEmpty(sel.Composition) ? sel.Composition : "no_selected",
            };

            if (/*!womanSections.Contains(sel.CodeSection.ToLower()) &&*/ articleCodes.Count(w => w.Contains("ROTO")) < 1)
            {
                return;// no woman , no roto collection
            }

            ILocationRepository locationRepo = factory.GetInstance<ILocationRepository>();
            IOrderActionsService actionsService = factory.GetInstance<IOrderActionsService>();

            var orders = orderRepo.GetOrdersByGroupID(groupInfo.OrderGroupID);
            // factories that can produce WOMAN section: spain (SDS01), china (SDS07), bangladesh (SDS04), turquia (SDS05)
            var allowLocations = locationRepo.GetIDTFactories().Where(w => w.FactoryCode == "SDS01" || w.FactoryCode == "SDS07" || w.FactoryCode == "SDS04" || w.FactoryCode == "SDS05" || w.FactoryCode == "POR01");

            // no roto collection and woman order not in china, bangladesh or spain, move to spain
            //if (womanSections.Contains(sel.CodeSection.ToLower()) && articleCodes.Count(w => w.Contains("ROTO")) < 1)
            //{
            //    foreach (var o in orders)
            //    {
            //        if (!allowLocations.Any(a => a.ID == o.LocationID)) 
            //            actionsService.MoveOrder(o.ID, allowLocations.First(f => f.FactoryCode == "SDS01").ID);
            //    }
            //}

            // ROTO collection always move to spain
            if (articleCodes.Count(w => w.Contains("ROTO")) >= 1)
            {
                foreach (var o in orders)
                {
                    actionsService.MoveOrder(o.ID, allowLocations.First(f => f.FactoryCode == "SDS01").ID);
                }
            }
        }


        // return true if article was added or updated, false if article was removed
        private bool AddArticleByCategory(PrintDB ctx, string categoryCode, string articleCode, ScalpersOrderGroupQuantitiesDTO gp, IEnumerable<OrderDetailDTO> articles, IOrder baseOrder, IEnumerable<ArticleInfoDTO> projectArticles, Guid fileGuid)
        {
            var articlesInCategory = projectArticles.Where(w => w.CategoryID != null &&  w.CategoryName.Equals(categoryCode)).ToList();
            // order with article with the same category
            var foundOrder = articles.Where(w => w.OrderDataID == baseOrder.OrderDataID && w.OrderStatus == OrderStatus.InFlow && articlesInCategory.Any(a => a.ArticleID.Equals(w.ArticleID))).ToList();

            var projectID = baseOrder.ProjectID;
            var ret = false;
            // order was found for articles in category

            var removeArticles = foundOrder.Where(w => w.ArticleCode != articleCode).ToList();
            var toUpdateArticle = foundOrder.Where(w => w.ArticleCode == articleCode).ToList();

            // remove article if exist in list and articleCode is null -> user leave empty
            if (string.IsNullOrEmpty(articleCode))
            {
                // remove order 
                foundOrder.ForEach(o =>
                {
                    orderRepo.ChangeStatus(o.OrderID, OrderStatus.Cancelled);
                });
                
                return ret;
            }

            // remove articles belong to the same category
            removeArticles.ForEach(o =>
            {
                orderRepo.ChangeStatus(o.OrderID, OrderStatus.Cancelled);
            });

            ret = true;

            if (toUpdateArticle.Count() > 0)
            {
                // update if articleCode already exist inner order from the same OrderDataID
                toUpdateArticle.ToList().ForEach(o =>
                {
                    orderRepo.ChangeStatus(o.OrderID, OrderStatus.InFlow);
                    var targetOrder = orderRepo.GetByID(o.OrderID);

                    UpdatePrinterJob(ctx, /*null,*/ gp, /*baseOrder,*/ targetOrder);
                });
            }
            else
            {
               
                // add new if articleCode not found inner order
                
                var groupInfo = groupRepo.GetBillingInfo(baseOrder.OrderGroupID);

                // use the ordernumber from baseOrder, to kee repetitions marks
                groupInfo.OrderNumber = baseOrder.OrderNumber;

                var totalQuantity = gp.Quantities.Sum(s => s.Value);
                // add new
                // TODO: how to get File GUID to set original file
                // dont show oders jet, wait for validation

                var partialOrder = orderRepo.CreateCustomPartialOrder(ctx, groupInfo, totalQuantity, baseOrder.OrderDataID, true, fileGuid);

                #region remove
                //// Clone printerjob from emptyOrder - only clone for article that no require composition Info
                //if (categoryCode != COMPOSITION_CATEGORY_CODE)
                //{
                //    ClonePrinterJobForComposition(ctx, articlesInCategory.First(f => f.ArticleCode.Equals(articleCode)), gp, baseOrder, partialOrder);
                //}
                //else
                //{
                //    ClonePrinterJob(ctx, articlesInCategory.First(f => f.ArticleCode.Equals(articleCode)), gp, baseOrder, partialOrder);
                //}
                #endregion remove
                //clonar substituir por ItemAssigmentService orderRepo.Copy
                ClonePrinterJob(ctx, articlesInCategory.First(f => f.ArticleCode.Equals(articleCode)), gp, /*baseOrder,*/ partialOrder);

                var partialOrderInfo = orderRepo.GetProjectInfo(partialOrder.ID);// ????: BrandID is not used to set validator
                    
                validatorSetterSrv.Execute(partialOrder.OrderGroupID, partialOrder.ID, partialOrder.OrderNumber, partialOrder.ProjectID, partialOrderInfo.BrandID);

            }

            return ret;

        }
        
        private void ClonePrinterJob(PrintDB ctx, ArticleInfoDTO article, ScalpersOrderGroupQuantitiesDTO gp, /*IOrder emptyOrderInfoXYZ,*/ IOrder targetOrder)
        {
            // details from UI, with user quantities asgined by size/color
            var printerJobDetailsIds = gp.Quantities.Select(s => s.PrinterJobDetailID);

            // OPT A: clone only PrintJobs for PrinterJobDetailsID Received
            var printJobs = ctx.PrinterJobs
                .Join(ctx.PrinterJobDetails, j => j.ID, jd => jd.PrinterJobID, (job, detail) => new { PrinterJob = job, PrinterJobDetail = detail})
                .Where(w => printerJobDetailsIds.Any(a => a.Equals(w.PrinterJobDetail.ID)))
                .Select(s => s.PrinterJob)
                .Distinct()
                .ToList();

            // OPT B: clone all printJobs with the same details, maybe is not the best options
            //var printJobs = ctx.PrinterJobs.Where(w => w.CompanyOrderID == emptyOrderInfo.ID).ToList();

            // clone printjobs process
            printJobs.ForEach((job) =>
            {
                var pj = new PrinterJob()
                {
                    CompanyID = job.CompanyID,
                    CompanyOrderID = targetOrder.ID,
                    ProjectID = job.ProjectID,
                    ProductionLocationID = job.ProductionLocationID,
                    AssignedPrinter = job.AssignedPrinter,
                    ArticleID = article.ArticleID,
                    Quantity = gp.Quantities.Sum(s => s.Value), 
                    Printed = 0,
                    Encoded = 0,
                    Extras = 0,
                    Status = JobStatus.Pending,
                    AutoStart = false,
                    PrintPackageGenerated = false

                };

                var inserted = printerJobRepo.AddExtraJob(ctx, pj);

                var printJobDetails = ctx.PrinterJobDetails.Where(w => w.PrinterJobID.Equals(job.ID)).ToList();

                
                // create details
                gp.Quantities.ForEach((det) => {

                    var pjd = printJobDetails.First(f => f.ID.Equals(det.PrinterJobDetailID));
                    
                    var jobDetail = new PrinterJobDetail()
                    {
                        PrinterJobID = inserted.ID,
                        ProductDataID = pjd.ProductDataID, // reuse the same DATA in Print_Data Database
                        Quantity = det.Value,
                        QuantityRequested = pjd.QuantityRequested,
                        Extras = 0,
                        PackCode = "USER_ADD",
                        UpdatedDate = DateTime.Now
                    };

                    printerJobRepo.AddExtraDetailToJob(ctx, jobDetail);

                });
                
            });

        }

        private void ClonePrinterJobForComposition(PrintDB ctx, ArticleInfoDTO article, ScalpersOrderGroupQuantitiesDTO gp, IOrder emptyOrderInfo, IOrder targetOrder)
        {
            var printerJobDetailsIds = gp.Quantities.Select(s => s.PrinterJobDetailID);

            var printJobs = ctx.PrinterJobs
                .Join(ctx.PrinterJobDetails, j => j.ID, jd => jd.PrinterJobID, (job, detail) => new { PrinterJob = job, PrinterJobDetail = detail })
                .Where(w => printerJobDetailsIds.Any(a => a.Equals(w.PrinterJobDetail.ID)))
                .Select(s => s.PrinterJob)
                .Distinct()
                .ToList();
            // clone printjobs
            printJobs.ForEach((job) =>
            {
                var pj = new PrinterJob()
                {
                    CompanyID = job.CompanyID,
                    CompanyOrderID = targetOrder.ID,
                    ProjectID = job.ProjectID,
                    ProductionLocationID = job.ProductionLocationID,
                    AssignedPrinter = job.AssignedPrinter,
                    ArticleID = article.ArticleID,
                    Quantity = gp.Quantities.Sum(s => s.Value),
                    Printed = 0,
                    Encoded = 0,
                    Extras = 0,
                    Status = JobStatus.Pending,
                    AutoStart = false,
                    PrintPackageGenerated = false

                };

                var inserted = printerJobRepo.AddExtraJob(ctx, pj);

                var printJobDetails = ctx.PrinterJobDetails.Where(w => w.PrinterJobID.Equals(job.ID)).ToList();

                // group by color

                // create details
                gp.Quantities.ForEach((det) => {

                    var pjd = printJobDetails.First(f => f.ID.Equals(det.PrinterJobDetailID));

                    var jobDetail = new PrinterJobDetail()
                    {
                        PrinterJobID = inserted.ID,
                        ProductDataID = pjd.ProductDataID,
                        Quantity = det.Value,
                        QuantityRequested = pjd.QuantityRequested,
                        Extras = 0,
                        PackCode = "USER_ADD",
                        UpdatedDate = DateTime.Now
                    };

                    printerJobRepo.AddExtraDetailToJob(ctx, jobDetail);

                });

            });

        }

        private void UpdatePrinterJob(PrintDB ctx, /*ArticleInfoDTO article,*/ ScalpersOrderGroupQuantitiesDTO gp, /*IOrder baseOrderXYZ,*/ IOrder targetOrder)
        {

            var printerJobDetailsIds = gp.Quantities.Select(s => s.PrinterJobDetailID);

            var baseData = ctx.PrinterJobs
                .Join(ctx.PrinterJobDetails, j => j.ID, jd => jd.PrinterJobID, (job, detail) => new { PrinterJob = job, PrinterJobDetail = detail })
                .Where(w => printerJobDetailsIds.Any(a => a.Equals(w.PrinterJobDetail.ID)))
                .Select(s => s)
                .ToList();

            var basePrinterJobs = baseData.Select(s => s.PrinterJob).Distinct().ToList(); ;
            var baseJobDetails = baseData.Select(s => s.PrinterJobDetail);

            var targetData = ctx.PrinterJobs
                .Join(ctx.PrinterJobDetails, j => j.ID, jd => jd.PrinterJobID, (job, detail) => new { PrinterJob = job, PrinterJobDetail = detail })
                .Where(w => w.PrinterJob.CompanyOrderID.Equals(targetOrder.ID))
                .Select(s => s)
                .ToList();

            var targetPrinterJobs = targetData.Select(s => s.PrinterJob).Distinct().ToList();
            var targetJobDetails = targetData.Select(s => s.PrinterJobDetail).ToList();

            // orders for scalper always contain the same details, later composition order will be diferent
            if (targetJobDetails.Count() != baseJobDetails.Count())
            {
                throw new Exception("Printer Jobs Details Error, contact your support team");
            }

            targetJobDetails.ForEach(tjd => {

                var bjd = baseJobDetails.First(f => f.ProductDataID.Equals(tjd.ProductDataID));
                var q = gp.Quantities.First(f => f.PrinterJobDetailID.Equals(bjd.ID));

                tjd.Quantity = q.Value;
                bjd.Quantity = q.Value;

                ctx.Entry(tjd).State = EntityState.Modified;
                ctx.Entry(bjd).State = EntityState.Modified;

            });

            targetPrinterJobs.ForEach(tpj => {
                tpj.Quantity = targetJobDetails.Where(w => w.PrinterJobID.Equals(tpj.ID)).Sum(s => s.Quantity);
                ctx.Entry(tpj).State = EntityState.Modified;
            });

            basePrinterJobs.ForEach(bpj => {
                bpj.Quantity = baseJobDetails.Where(w => w.PrinterJobID.Equals(bpj.ID)).Sum(s => s.Quantity);
                ctx.Entry(bpj).State = EntityState.Modified;
            });
            
            // save
            ctx.SaveChanges();
            
        }

        private void SetVariableData(PrintDB ctx, ScalpersOrderGroupQuantitiesDTO gp, IOrder baseOrder)
        {
            var catalogs = catalogRepo.GetByProjectID(ctx, baseOrder.ProjectID, true);
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
            var madeInCatalog = catalogs.First(f => f.Name.Equals("MadeIn"));

            var printerJobs = printerJobRepo.GetByOrderID(baseOrder.ID, true).ToList();


            // XXX: esta consulta esta tardando 7 segundos
            //var printerDetails = ctx.PrinterJobDetails.Where(w => printerJobs.Any(a => w.PrinterJobID.Equals(a.ID)))
            //    .Select(s => s).ToList();

            var printerDetails = ctx.PrinterJobDetails
                                .Join(ctx.PrinterJobs, ptjd => ptjd.PrinterJobID, ptj =>ptj.ID, (pjd, pj) => new { PrinterJobDetail = pjd, PrinterJob = pj } )
                                .Where(w => w.PrinterJob.CompanyOrderID == baseOrder.ID)
                                .Select(s => s.PrinterJobDetail)
                                .ToList();


            using (DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                ////update one by one
                //printerDetails.ForEach(pjdt => {

                //    sw.Restart();
                //    // get row
                //    var detail = dynamicDB.SelectOne(detailCatalog.CatalogID, pjdt.ProductDataID);
                //    var product = dynamicDB.SelectOne(variableDataCatalog.CatalogID, detail.GetValue<int>("Product"));
                //    product["CodeGama"] = gp.CodeGama;
                //    product["MadeIn"] = gp.MadeInCode;
                //    dynamicDB.Update(variableDataCatalog.CatalogID, product);
                //});


                var listfield = JsonConvert.DeserializeObject<List<FieldDefinition>>(madeInCatalog.Definition).Where(x => x.Name != "ID" && x.Name != "IsActive");

                // bulk update
                var allIds = printerDetails.Select(s => s.ProductDataID);

                if (allIds.Count() > 0)
                {
                   
                    var jsonMadeIn = dynamicDB.SelectOne(madeInCatalog.CatalogID, int.Parse(gp.MadeInCode));
                    var madeInConcat = "";
                    var concatChar = Environment.NewLine;

                    foreach (var madein in listfield)
                    {
                       madeInConcat += jsonMadeIn.GetValue<string>(madein.ToString()) + concatChar;

                    }

                    dynamicDB.Conn.ExecuteNonQuery(
                       $@"UPDATE v
                    SET
                        CodeGama = @CodeGama,
                        MadeIn = @MadeInCode,
                        FullMadeIn = @madeInConcat,
                        Importer = @importer
                    FROM {variableDataCatalog.TableName} v
                    INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                    WHERE d.ID in  ({string.Join(',', allIds)})",gp.CodeGama,gp.MadeInCode, madeInConcat.TrimEnd('\r', '\n'),gp.Importer);

                }
            }
            
        }

        private void AddOrRemoveEmptyArticle(PrintDB ctx, IOrder baseOrder/*, IList<OrderDetailDTO> artilcesInOrder*/, bool containsArticles)
        {

            // if contain an article remove EMPTY_ARTICLE_CODE, else keep EMPTY_ARTICLE_CODE
            //var found = artilcesInOrder.Where(w => w.ArticleCode.Equals(Article.EMPTY_ARTICLE_CODE)).ToList();
            //if (found.Count() > 0)
            //{
            //    var o = found.First();
            //    orderRepo.ChangeStatus(o.OrderID, OrderStatus.Cancelled);
            //}

            //// always return a record for Scalpers
            //var found = ctx.CompanyOrders
            //        .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { CompanyOrders = o, PrinterJobs = j })
            //        .Join(ctx.Articles, join1 => join1.PrinterJobs.ArticleID, art => art.ID, (j1, a) => new { j1.CompanyOrders, j1.PrinterJobs, Articles = a })
            //        .Join(ctx.OrderUpdateProperties, join2 => join2.CompanyOrders.ID, props => props.OrderID, (j2,p) => new { j2.CompanyOrders, j2.PrinterJobs, j2.Articles, OrderUpdateProperties = p })
            //        .Where(w => w.CompanyOrders.OrderGroupID.Equals(baseOrder.OrderGroupID) && w.Articles.ArticleCode.Equals(Article.EMPTY_ARTICLE_CODE))
            //        .Where(w => w.OrderUpdateProperties.IsRejected != true)
            //        .Select(s => s.CompanyOrders.ID).ToList();

            //log.LogMessage($"Buscando articulo default se encontraron : [{found.Count()}], Se han agregado Articulos ?: [{containsArticles}]");
            
            if (containsArticles)
            {
                //var o = found.First();
                orderRepo.ChangeStatus(baseOrder.ID, OrderStatus.Cancelled);
            }else
            {
                //var o = found.First();
                orderRepo.ChangeStatus(baseOrder.ID, OrderStatus.InFlow);
            }

        }

        public IEnumerable<IOrder> GetOrdersInValidation(ScalpersOrderGroupQuantitiesDTO gp)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
                return GetOrdersInValidation(ctx, gp).ToList();
        }

        public IEnumerable<IOrder> GetOrdersInValidation(PrintDB ctx, ScalpersOrderGroupQuantitiesDTO gp)
        {

            var groupInfo = groupRepo.GetBillingInfo(gp.OrderGroupID);

            var orders = ctx.CompanyOrders
                .Join(ctx.Wizards, co => co.ID, wz => wz.OrderID, (ord, wzd) => new { Order = ord, Wizard = wzd })
                .Join(ctx.OrderUpdateProperties, join1 => join1.Order.ID, props => props.OrderID, (j1, pp) => new { j1.Order, j1.Wizard, OrderUpdateProperty = pp })
                .Where(w => w.Order.OrderGroupID.Equals(gp.OrderGroupID))
                .Where(w => w.Order.OrderStatus != OrderStatus.Cancelled)
                .Where(w => w.OrderUpdateProperty.IsRejected == false)
                .Where(w => w.Wizard.IsCompleted.Equals(false))
                .Select(s => s.Order);

            return orders;
        }
    }

    
}
