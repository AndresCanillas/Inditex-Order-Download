using LinqKit;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services.Wizards;
using static WebLink.Services.BrownieReportGeneratorService;

namespace WebLink.Services.Wizards
{
    public class ItemAssigmentService : IItemAssigmentService
    {

        private IFactory factory;
        private IDBConnectionManager connManager;
        private IOrderRepository orderRepo;
        private IArticleRepository articleRepo;
        private IOrderGroupRepository groupRepo;
        private IPrinterJobRepository printerJobRepo;
        private IOrderSetValidatorService validatorSetterSrv;
        private ICatalogRepository catalogRepo;
        private IRemoteFileStore orderStore;
        private IUserData userData;

        public ItemAssigmentService(IFactory factory
            , IDBConnectionManager connManager
            , IOrderRepository orderRepo
            , IArticleRepository articleRepo
            , IOrderGroupRepository groupRepo
            , IPrinterJobRepository printerJobRepo
            , ICatalogRepository catalogRepo
            , IFileStoreManager storeManager
            , IOrderSetValidatorService validatorSetterSrv
            , IUserData userData)
        {
            this.factory = factory;
            this.connManager = connManager;
            this.orderRepo = orderRepo;
            this.articleRepo = articleRepo;
            this.groupRepo = groupRepo;
            this.printerJobRepo = printerJobRepo;
            //this.validatorSetterSrv = validatorSetterSrv;
            this.catalogRepo = catalogRepo;
            //this.log = log.GetSection("CommonValidator");
            this.orderStore = storeManager.OpenStore("OrderStore");
            this.validatorSetterSrv = validatorSetterSrv;
            this.userData = userData;


        }

        public CustomDetailSelectionDTO UpdateOrders(CustomDetailSelectionDTO rq)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
                return UpdateOrders(ctx, rq);
        }

        //public void UpdateBaseDataOrders(CustomDetailSelectionDTO rq)
        //{
        //    using (var ctx = factory.GetInstance<PrintDB>())
        //    {
        //        foreach (var selection in rq.Selection)
        //        {
        //            foreach (var id in selection.Orders)
        //            {
        //                IOrder order = ctx.CompanyOrders.FirstOrDefault(o => o.ID == id);
        //                if (order != null)
        //                {
        //                    SetBaseData(ctx, selection.ProductFields, order);
        //                }
        //            }


        //        }
        //    }

        //}

        private CustomDetailSelectionDTO UpdateOrders(PrintDB ctx, CustomDetailSelectionDTO rq)
        {

            var noValidatedStatus = new List<OrderStatus> { OrderStatus.Received, OrderStatus.Processed, OrderStatus.InFlow };

            foreach(var gp in rq.Selection)
            {

                // actualizar todas las ordenes con las mismas cantidades del ArticleCode "0000"
                var articles = new List<OrderDetailDTO>();

                var orderArticles = ctx.CompanyOrders
                   .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { CompanyOrders = o, PrinterJobs = j })
                   .Join(ctx.Articles, join1 => join1.PrinterJobs.ArticleID, art => art.ID, (j1, a) => new { j1.CompanyOrders, j1.PrinterJobs, Articles = a })
                   .Join(ctx.OrderUpdateProperties, join2 => join2.CompanyOrders.ID, pp => pp.OrderID, (j2, props) => new { j2.CompanyOrders, j2.PrinterJobs, j2.Articles, OrderUpdateProperties = props })
                   .Where(w => w.CompanyOrders.OrderGroupID.Equals(gp.OrderGroupID))
                   //  .Where(w => w.Articles.LabelID.HasValue)
                   .Where(w => !w.OrderUpdateProperties.IsRejected)
                   .Where(w => noValidatedStatus.Any(a => a == w.CompanyOrders.OrderStatus))
                   .Select(s => new
                   {
                       ArticleID = s.Articles.ID,
                       Article = s.Articles.Name,
                       ArticleCode = s.Articles.ArticleCode,
                       OrderID = s.CompanyOrders.ID,
                       OrderGroupID = s.CompanyOrders.OrderGroupID,
                       PrinterJobID = s.PrinterJobs.ID,
                       ProviderRecordID = s.CompanyOrders.ProviderRecordID,
                       OrderDataID = s.CompanyOrders.OrderDataID,
                       OrderStatus = s.CompanyOrders.OrderStatus,
                       IsItem = !s.Articles.LabelID.HasValue

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
                        OrderStatus = s.OrderStatus,
                        IsItem = s.IsItem

                    }));

                var groupInfo = groupRepo.GetBillingInfo(gp.OrderGroupID); //se obtiene el ordergroup actual

                var notForBaseOrder = ctx.Articles.FirstOrDefault(f => f.ProjectID == groupInfo.ProjectID && f.ArticleCode == "GMSTICKER"); // TODO: HARCODE FOR GrainDeMalice

                var found = orderArticles
                    .Where(w => !w.IsItem)
                    .Where(w => notForBaseOrder == null || w.ArticleCode != notForBaseOrder.ArticleCode) // TODO: HARCODE FOR GrainDeMalice
                    .FirstOrDefault();

                if(found == null)
                {
                    found = orderArticles
                    .Where(w => notForBaseOrder == null || w.ArticleCode != notForBaseOrder.ArticleCode) // TODO: HARCODE FOR GrainDeMalice
                    .FirstOrDefault();
                }

                var orderID = found.OrderID;



                var allArticles = articleRepo.GetArticlesInfo(groupInfo.ProjectID); //se obtienen todos los articulos por Proyecto

                // order selected from UI
                var uiOrder = orderRepo.GetByID(orderID);

                //check for ParentOrder
                var baseOrder = uiOrder.ParentOrderID == null ? uiOrder : orderRepo.GetByID(uiOrder.ParentOrderID.Value);

                // always get Order for the Article.EMPTY_ARTICLE_CODE to get FILE_GUID
                //var initialOrder = orderArticles.Find(f => f.ArticleCode == Article.EMPTY_ARTICLE_CODE);

                //  orders always are created with Article.EMPTY_ARTICLE_CODE
                //baseOrder = orderRepo.GetByID(initialOrder.OrderID);

                IRemoteFile file = orderStore.TryGetFileAsync(baseOrder.ID).GetAwaiter().GetResult();

                // var hasLog = AddArticleByCategory(ctx, LOGISTIC_CATEGORY_CODE, gp.Logistic, gp, articles, baseOrder, allArticles, file.FileGUID);

                var containsNewArticles = 0;

                foreach(var selectedArticle in gp.SelectedArticles)
                {
                    var Order = AddArticle(ctx, selectedArticle.ArticleID, gp, articles, baseOrder, allArticles);

                    SetVariableData(ctx, gp.ProductFields, selectedArticle, Order);

                    if(gp?.CustomDetails.Count > 0)
                        SetVariableDataCustomDetails(ctx, gp.CustomDetails, Order);

                    if(!string.IsNullOrEmpty(selectedArticle.PackCode))
                        UpdatePackCode(ctx, Order.ID, selectedArticle.PackCode);

                    if(notForBaseOrder == null || selectedArticle.ArticleID != notForBaseOrder.ID) // TODO: HARCODE FOR GrainDeMalice
                        containsNewArticles++;

                }

                RemoveArticles(ctx, gp, rq.GetAllArticles);

                // Set OrderData -

                AddOrRemoveEmptyArticle(ctx, baseOrder, articles, containsNewArticles > 0);

                // XXX: this ordergroup always keep active, 
                // cancelations sometimes deactivate odergroup record for the nature of how to this script add or remove CompanyOrders to the current OrderGroup
                var orderGroup = groupRepo.GetByID(groupInfo.OrderGroupID);
                orderGroup.IsActive = true;
                groupRepo.Update(orderGroup);

                //SetFactory(ctx, groupInfo, gp);
            }

            return rq;
        }

        private void UpdatePackCode(PrintDB ctx, int orderID, string packCode)
        {
            var printerDetails = printerJobRepo.JobDetailsByOrder(ctx, orderID);
            foreach(var pd in printerDetails)
            {
                var details = ctx.PrinterJobDetails.FirstOrDefault(p => p.ID == pd.ID);
                if(details != null)
                {
                    details.PackCode = packCode;
                    ctx.SaveChanges();
                }
            }

        }

        public List<OrderGroupSelectionDTO> GetOrderData(List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter = null)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
                return GetOrderData(ctx, selection, filter);
        }

        public List<OrderGroupSelectionDTO> GetOrderData(PrintDB ctx, List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter = null)
        {

            // default only active  orders type LABEL, 
            if(filter == null)
                filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Label, ActiveFilter = OrderActiveFilter.All, OrderStatus = OrderStatusFilter.InFlow };

            // get all articles by OrderGroupID
            var cloneSelection = new List<OrderGroupSelectionDTO>();

            selection.ForEach(e =>
            {
                var orders = new List<int>() { };// leave empty to get all orders for this group
                var itemSel = new OrderGroupSelectionDTO(e);
                itemSel.Details = new List<OrderDetailDTO>();
                itemSel.Orders = orders.ToArray();
                cloneSelection.Add(itemSel);
            });

            var result = orderRepo.GetArticleDetailSelection(ctx, cloneSelection, filter);

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

            });

            return result;
        }


        private IOrder AddArticle(PrintDB ctx, int articleID, OrderGroupSelectionItemAssigmentDTO gp, IEnumerable<OrderDetailDTO> articlesAdded, IOrder baseOrder, IEnumerable<ArticleInfoDTO> projectArticles)
        {
            IOrder Order = null;

            // order with article
            var foundOrder = articlesAdded.Where(w => w.ArticleID == articleID).FirstOrDefault();

            var projectID = baseOrder.ProjectID;

            // order was found in articles 
            if(foundOrder == null)
            {
                var groupInfo = groupRepo.GetBillingInfo(baseOrder.OrderGroupID);
                groupInfo.OrderNumber = baseOrder.OrderNumber;

                //Create a new order partial
                //Order = orderRepo.CreateCustomPartialOrder(ctx, groupInfo, baseOrder.Quantity, baseOrder.OrderDataID,true, fileGuid);

                var foundArticle = projectArticles.Where(x => x.ArticleID == articleID).FirstOrDefault();

                Order = orderRepo.Copy(baseOrder.ID, true, foundArticle.ArticleCode, null, userData.UserName, DocumentSource.Validation);

                //ClonePrinterJob(ctx, projectArticles.First(f => f.ArticleID.Equals(articleID)) , baseOrder, Order);

                var partialOrderInfo = orderRepo.GetProjectInfo(Order.ID);

                validatorSetterSrv.Execute(Order.OrderGroupID, Order.ID, Order.OrderNumber, Order.ProjectID, partialOrderInfo.BrandID);
            }
            else
            {
                Order = orderRepo.GetByID(foundOrder.OrderID);
            }

            if(foundOrder != null && foundOrder.OrderStatus == OrderStatus.Cancelled)
            {
                orderRepo.ChangeStatus(foundOrder.OrderID, OrderStatus.InFlow);

            }

            return Order;
        }

        private void RemoveArticles(PrintDB ctx, OrderGroupSelectionItemAssigmentDTO gp, bool getAllArticles)
        {
            var orderArticles = ctx.CompanyOrders
                  .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { CompanyOrders = o, PrinterJobs = j })
                  .Join(ctx.Articles, join1 => join1.PrinterJobs.ArticleID, art => art.ID, (j1, a) => new { j1.CompanyOrders, j1.PrinterJobs, Articles = a })
                  .Join(ctx.OrderUpdateProperties, join2 => join2.CompanyOrders.ID, pp => pp.OrderID, (j2, props) => new { j2.CompanyOrders, j2.PrinterJobs, j2.Articles, OrderUpdateProperties = props })
                  .Where(w => w.CompanyOrders.OrderGroupID.Equals(gp.OrderGroupID))
                  .Where(w => getAllArticles || w.Articles.LabelID.HasValue)
                  .Where(w => !w.OrderUpdateProperties.IsRejected)
                  .Where(w => w.CompanyOrders.OrderStatus == OrderStatus.InFlow)
                  .Select(s => new
                  {
                      ArticleID = s.Articles.ID,
                      OrderID = s.CompanyOrders.ID
                  }

                  ).ToList();

            var toremove = orderArticles.Select(art => art.ArticleID).Except(gp.SelectedArticles.Select(art => art.ArticleID).ToList());

            toremove.ToList().ForEach(articleid =>
            {
                var orderID = orderArticles.Find(f => f.ArticleID == articleid).OrderID;
                orderRepo.ChangeStatus(orderID, OrderStatus.Cancelled);

            });
        }

        private void AddOrRemoveEmptyArticle(PrintDB ctx, IOrder baseOrder, IList<OrderDetailDTO> articlesInOrder, bool containsArticles)
        {

            if(containsArticles)
            {
                //var o = found.First();
                articlesInOrder
                    .Where(article => article.ArticleCode == Article.EMPTY_ARTICLE_CODE && baseOrder.ID == article.OrderID).ToList()
                    .ForEach(article => orderRepo.ChangeStatus(baseOrder.ID, OrderStatus.Cancelled));
            }
            else
                orderRepo.ChangeStatus(baseOrder.ID, OrderStatus.InFlow);

        }

        private void ClonePrinterJob(PrintDB ctx, ArticleInfoDTO article, IOrder baseOrder, /*IOrder emptyOrderInfoXYZ,*/ IOrder targetOrder)
        {

            var jobs = printerJobRepo.GetAllJobsByOrderId(baseOrder.ID);

            foreach(var job in jobs)
            {
                var jobdetails = printerJobRepo.GetAllJobDetails(job.ID, true).Select(s => new OrderProductionDetailRow
                {
                    DetailID = s.ProductDataID,
                    ArticleID = article.ArticleID,
                    ArticleCode = article.ArticleCode,
                    Quantity = s.Quantity,
                    PackCode = "USER_ADD" //s.PackCode
                });

                printerJobRepo.CreateArticleOrder(targetOrder, jobdetails, baseOrder.LocationID, null, null, false);

            }
        }

        //public void SetBaseData(IEnumerable<ProductField> basefields, IOrder order)
        //{
        //    using (var ctx = factory.GetInstance<PrintDB>())
        //    {
        //        SetBaseData(ctx, basefields, order);
        //    }
        //}

        //public void SetBaseData(PrintDB ctx, IEnumerable<ProductField> updateColumns, IEnumerable<ProductField> filterColumns, int projectId)
        //{
        //    var catalogs = catalogRepo.GetByProjectID(projectId);
        //    var baseDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.BASEDATA_CATALOG));
        //    var sb = new StringBuilder();
        //    using (DynamicDB dynamicDB = connManager.CreateDynamicDB())
        //    {
        //        var parametersSet = new List<string>();
        //        var parametersVal = new List<object>();

        //        updateColumns.ToList().ForEach(p =>
        //        {
        //            parametersSet.Add($"b.{p.Name} = @p{p.Name} ");
        //            parametersVal.Add(p.Value);
        //        });

        //        bool isFirstCondition = true;
        //        foreach (var item in filterColumns.ToList())
        //        {
        //            if (isFirstCondition)
        //            {
        //                sb.Append($"b.{item.Name}  {item.Value}");
        //                isFirstCondition = false;
        //                continue;
        //            }
        //            sb.Append($" AND b.{item.Name}  {item.Value}");

        //        }
        //        var whereCondition = sb.Length > 0 ? "WHERE " + sb.ToString() : string.Empty;
        //        var query = $@"UPDATE b
        //            SET 
        //                {string.Join(",", parametersSet)} 
        //            FROM {baseDataCatalog.TableName} b 
        //            {whereCondition}
        //        ";

        //        dynamicDB.Conn.ExecuteNonQuery(query, parametersVal.ToArray());
        //    }
        //}

        public void SetBaseData(PrintDB ctx, IEnumerable<ProductField> commonFields, IOrder order)
        {
            var catalogs = catalogRepo.GetByProjectID(ctx, order.ProjectID, true);
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var baseDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.BASEDATA_CATALOG));

            var printerDetails = printerJobRepo.JobDetailsByOrder(ctx, order.ID);

            using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                var allIds = printerDetails.Select(s => s.ProductDataID);

                if(allIds.Count() > 0)
                {

                    var parametersSet = new List<string>();
                    var parametersVal = new List<object>();

                    commonFields.ToList().ForEach(p =>
                    {
                        parametersSet.Add($"b.{p.Name} = @p{p.Name} ");
                        parametersVal.Add(p.Value);
                    });
                    var orderNumberList = order.OrderNumber.Split(',');


                    foreach(var orderNumber in orderNumberList)
                    {
                        dynamicDB.Conn.ExecuteNonQuery(
                                 $@"UPDATE b
                                 SET
                                         {string.Join(",", parametersSet)}
                                 FROM {baseDataCatalog.TableName} b
                                 WHERE b.OrderNumber LIKE '%{orderNumber.Trim()}%'", parametersVal.ToArray());
                    }
                }
            }

        }

        public void SetVariableData(IEnumerable<ProductField> commonFields, CustomArticle selectedarticle, IOrder order)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                SetVariableData(ctx, commonFields, selectedarticle, order);
            }
        }

        public void SetVariableData(PrintDB ctx, IEnumerable<ProductField> commonFields, CustomArticle selectedarticle, IOrder order)
        {

            if(commonFields.ToList().Count == 0)
                return;

            var catalogs = catalogRepo.GetByProjectID(ctx, order.ProjectID, true);
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));

            var printerDetails = printerJobRepo.JobDetailsByOrder(ctx, order.ID);

            using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                // bulk update
                var allIds = printerDetails.Select(s => s.ProductDataID);

                if(allIds.Count() > 0)
                {
                    var found = ctx.Articles.Where(x => x.ID == selectedarticle.ArticleID).FirstOrDefault();
                    var parametersSet = new List<string>();
                    var parametersVal = new List<object>();

                    commonFields.ToList().ForEach(p =>
                    {
                        parametersSet.Add($"v.{p.Name} = @p{p.Name} ");
                        parametersVal.Add(p.Value);
                    });

                    dynamicDB.Conn.ExecuteNonQuery(
                   $@"UPDATE v 
                   SET
                   {string.Join(",", parametersSet)}
                   FROM {variableDataCatalog.TableName} v
                   INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                   WHERE d.ID in  ({string.Join(',', allIds)})", parametersVal.ToArray());

                    //ensure the data containt the value of the article code created after cloned
                    dynamicDB.Conn.ExecuteNonQuery(
                    $@"UPDATE d
                    SET
                    d.ArticleCode = @ArticleCode
                    FROM {variableDataCatalog.TableName} v
                    INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                    WHERE d.ID in  ({string.Join(',', allIds)})", found.ArticleCode);

                }
            }

        }

        //public void SetBaseData(IEnumerable<ProductField> updateColumns, IEnumerable<ProductField> filterColumns, int projectId)
        //{
        //    using (var ctx = factory.GetInstance<PrintDB>())
        //    {
        //        SetBaseData(ctx, updateColumns, filterColumns, projectId);
        //    }
        //}

        /// <summary>
        /// Update Size ID value always on VaraibleData catalog
        /// TODO: this Method can be use to save any field on table VariableData
        /// XXX: warning, can be allow for SQL INJECTION
        /// </summary>
        /// <param name="rq"></param>
        public void UpdateSizes(List<FieldsToUpdateDTO> rq)
        {
            int projectID = -1;
            List<ICatalog> catalogs = new List<ICatalog>();
            ICatalog detailCatalog;
            ICatalog variableDataCatalog;
            IList<FieldDefinition> vdFields = new List<FieldDefinition>();

            using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                rq.ForEach((dto) =>
                {

                    if(dto.ProjectID != projectID)
                    {
                        projectID = dto.ProjectID;
                        catalogs = catalogRepo.GetByProjectID(dto.ProjectID, true);
                    }

                    detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
                    variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
                    vdFields = variableDataCatalog.Fields;

                    var parametersSet = new List<string>();
                    var parametersVal = new List<object>();


                    dto.ProductFields.ToList().ForEach(p =>
                    {
                        // avoid SQL injection
                        if(vdFields.FirstOrDefault(r => r.Name == p.Name) == null)
                            return;// TODO: log ignore field

                        parametersSet.Add($"v.[{p.Name}] = @p{p.Name} ");
                        parametersVal.Add(p.Value);
                    });

                    parametersVal.Add(dto.ProductDataID);

                    dynamicDB.Conn.ExecuteNonQuery(
                    $@"UPDATE v 
                    SET
                    {string.Join(",", parametersSet)}
                    FROM {variableDataCatalog.TableName} v
                    INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                    WHERE d.ID = @productDataID", parametersVal.ToArray());

                });

            }
        }

        public void UpdateTagtypes(List<FieldsToUpdateDTO> rq)
        {
            int projectID = -1;
            List<ICatalog> catalogs = new List<ICatalog>();
            ICatalog detailCatalog;
            ICatalog variableDataCatalog;
            IList<FieldDefinition> vdFields = new List<FieldDefinition>();

            using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                rq.ForEach((dto) =>
                {

                    if(dto.ProjectID != projectID)
                    {
                        projectID = dto.ProjectID;
                        catalogs = catalogRepo.GetByProjectID(dto.ProjectID, true);
                    }

                    detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
                    variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
                    vdFields = variableDataCatalog.Fields;

                    var parametersSet = new List<string>();
                    var parametersVal = new List<object>();


                    dto.ProductFields.ToList().ForEach(p =>
                    {
                        // avoid SQL injection
                        if(vdFields.FirstOrDefault(r => r.Name == p.Name) == null)
                            return;// TODO: log ignore field

                        parametersSet.Add($"v.[{p.Name}] = @p{p.Name} ");
                        parametersVal.Add(p.Value);
                    });

                    parametersVal.Add(dto.ProductDataID);

                    dynamicDB.Conn.ExecuteNonQuery(
                    $@"UPDATE v 
                    SET
                    {string.Join(",", parametersSet)}
                    FROM {variableDataCatalog.TableName} v
                    INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                    WHERE d.ID = @productDataID", parametersVal.ToArray());

                });

            }
        }

        /// <summary>
        /// FOR INDITEX: update in variable data TrackingCode Field
        /// </summary>
        /// <param name="rq"></param>
        public void UpdateCustomTrackinCode(List<int> orderIds)
        {
            var trackingCodeCalculator = new Inditex.HelperLib.InditexTrackingCodeMaskCalculator();
            var connectionManager = factory.GetInstance<IConnectionManager>();
            var log = factory.GetInstance<ILogService>();
            var maskConfigCatalogName = "TrackingCodeConfigLookup";

            var currentProjectID = 0;
            var currentArticleCode = string.Empty;
            List<ICatalog> catalogs = null;
            ICatalog orderCatalog = null;
            ICatalog detailCatalog = null;
            ICatalog variableDataCatalog = null;
            ICatalog trackinCodeConfigLookupCatalog = null;
            int detailFieldID = 0;
            string mask = string.Empty;

            // validate if project has a Custon Tracking Code Configured looking Mask Configuration


            using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {

                orderIds.ForEach(o =>
                {
                    // only need projectID and ArticleCode
                    var orderInfo = orderRepo.GetOrderArticle(o);

                    if(orderInfo.HasRFID == false) return; // continue with next order

                    // cache: try con reuse catalogs for the same project within consecutive ids
                    if(currentProjectID != orderInfo.ProjectID)
                    {
                        catalogs = catalogRepo.GetByProjectID(orderInfo.ProjectID, true);
                        orderCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDER_CATALOG));
                        detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
                        variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
                        detailFieldID = orderCatalog.Fields.First(f => f.Name == "Details").FieldID;
                        
                    }

                    // cache: try to reuse the mask for the same article
                    if(currentProjectID != orderInfo.ProjectID && currentArticleCode != orderInfo.ArticleCode)
                    {
                        mask = trackingCodeCalculator.GetMask(orderInfo.ProjectID, orderInfo.ArticleCode, connectionManager);
                    }

                    var orderData = dynamicDB.Select(orderCatalog.CatalogID,
                    $@"SELECT CTKC_VariableData.*
                    FROM {orderCatalog.TableName} CTKC_Order
                    INNER JOIN REL_{orderCatalog.CatalogID}_{detailCatalog.CatalogID}_{detailFieldID} r1 ON CTKC_Order.ID = r1.SourceID
                    INNER JOIN {detailCatalog.TableName} CTKC_OrderDetail ON CTKC_OrderDetail.ID = r1.TargetID
                    INNER JOIN {variableDataCatalog.TableName} CTKC_VariableData ON CTKC_VariableData.ID = CTKC_OrderDetail.Product
                    WHERE CTKC_Order.ID = {orderInfo.OrderDataID}
                    ");

                    orderData.ForEach(d =>
                    {

                        var trackingCode = trackingCodeCalculator.ProcessMask(mask, d as JObject);

                        // it would be nice to have a configurable field name -> get targefield from configuration
                        d["TrackingCode"] = trackingCode;

                        dynamicDB.Update(variableDataCatalog.CatalogID, d as JObject);// warning: update one by one
                    });

                });

            }
            
        }

        private void SetVariableDataCustomDetails(PrintDB ctx, IEnumerable<CustomDetails> customDetails, IOrder order)
        {
            var catalogs = catalogRepo.GetByProjectID(ctx, order.ProjectID, true);
            var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
            var variableDataCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));

            using(DynamicDB dynamicDB = connManager.CreateDynamicDB())
            {
                if(customDetails.Count() > 0)
                {
                    var parametersSet = new List<string>();
                    var parametersVal = new List<object>();
                    var units = customDetails.Select(s => s.Value).ToList();

                    customDetails.ToList().ForEach(customDetail =>
                    {
                        dynamicDB.Conn.ExecuteNonQuery(
                                                       $@"UPDATE v
                                                        SET v.{customDetail.Name} = {customDetail.Value} 
                                                        FROM {variableDataCatalog.TableName} v
                                                        INNER JOIN {detailCatalog.TableName} d ON v.ID = d.Product
                                                        WHERE v.Barcode = '{customDetail.Barcode}'");
                    });

                }
            }
        }
    }
}
