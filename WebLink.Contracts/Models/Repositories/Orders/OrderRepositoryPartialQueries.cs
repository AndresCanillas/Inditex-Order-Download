using LinqKit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Infrastructure.Encoding.Tempe;
using Service.Contracts.PrintCentral;
using Service.Contracts.WF;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts.Workflows;

namespace WebLink.Contracts.Models
{
    public enum ProductDetails
    {
        None = 0,
        Custom,
        Label,
        All
    }

    public partial class OrderRepository : GenericRepository<IOrder, Order>, IOrderRepository
    {


        public IEnumerable<OrderToUpdateDTO> GetOrdersToUpdate(int orderGroupID, int orderID, string orderNumber, int projectID, ConflictMethod method, bool categorizeArticle, int? factoryID, int? providerRecordId, bool onlyActive = true)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrdersToUpdate(ctx, orderGroupID, orderID, orderNumber, projectID, method, categorizeArticle, factoryID, providerRecordId, onlyActive).ToList();
            }
        }


        public IEnumerable<OrderToUpdateDTO> GetOrdersToUpdate(PrintDB ctx, int orderGroupID, int orderID, string orderNumber, int projectID, ConflictMethod method, bool categorizeArticle, int? factoryID, int? providerRecordId, bool onlyActive = true)
        {
            if(method.Equals(ConflictMethod.Article))
                return GetOrdersToUpdateByArticle(ctx, orderGroupID, orderID, factoryID, orderNumber, projectID, categorizeArticle, providerRecordId, onlyActive);
            // defuatl or shareddata use the same query
            return GetOrdersToUpdatByDefult(ctx, orderGroupID, orderID, factoryID, orderNumber, projectID, onlyActive);
        }


        private IEnumerable<OrderToUpdateDTO> GetOrdersToUpdatByDefult(PrintDB ctx, int orderGroupID, int orderID, int? factoryID, string orderNumber, int projectID, bool onlyActive = true)
        {
            // how to do joins
            // https://stackoverflow.com/a/3217679/2062838
            // https://stackoverflow.com/a/10884757/2062838

            // can use orderfilehistory instace, will be add later

            var activeOrders = new List<OrderStatus>() {
                    OrderStatus.Cancelled,
                    OrderStatus.Completed
                };

            var qq = (from GOTUBD_Order in ctx.CompanyOrders
                      join GOTUBD_PrinterJob in ctx.PrinterJobs on GOTUBD_Order.ID equals GOTUBD_PrinterJob.CompanyOrderID
                      join GOTUBD_Article in ctx.Articles on GOTUBD_PrinterJob.ArticleID equals GOTUBD_Article.ID
                      join GOTUBD_OrderUpdateProperty in ctx.OrderUpdateProperties on GOTUBD_Order.ID equals GOTUBD_OrderUpdateProperty.OrderID

                      where GOTUBD_Order.OrderGroupID == orderGroupID
                      && GOTUBD_Order.OrderNumber == orderNumber
                      && GOTUBD_Order.ProjectID == projectID
                      && GOTUBD_OrderUpdateProperty.IsRejected == false
                      && (factoryID.HasValue || GOTUBD_Order.LocationID == factoryID.Value)
                      && (onlyActive == false || !activeOrders.Contains(GOTUBD_Order.OrderStatus))
                      orderby GOTUBD_Order.CreatedDate
                      select new OrderToUpdateDTO()
                      {
                          OrderID = GOTUBD_Order.ID,
                          OrderNumber = GOTUBD_Order.OrderNumber,
                          OrderStatus = GOTUBD_Order.OrderStatus,
                          ProductionType = GOTUBD_Order.ProductionType,
                          IsStopped = GOTUBD_Order.IsStopped,
                          OrderCreatedAt = GOTUBD_Order.CreatedDate,
                          UpdatePropertiesID = GOTUBD_OrderUpdateProperty.ID,
                          IsActive = GOTUBD_OrderUpdateProperty.IsActive,
                          IsReject = GOTUBD_OrderUpdateProperty.IsRejected,
                          UpdatePropertiesCreatedAt = GOTUBD_OrderUpdateProperty.CreatedDate,
                          ArticleHasEnableConflicts = GOTUBD_Article.EnableConflicts

                      }).AsNoTracking();

            return qq.ToList();
        }


        private IEnumerable<OrderToUpdateDTO> GetOrdersToUpdateByArticle(PrintDB ctx, int orderGroupID, int orderID, int? factoryID, string orderNumber, int projectID, bool categorizeArticle, int? providerRecordId, bool onlyActive = true)
        {
            // how to do joins
            // https://stackoverflow.com/a/3217679/2062838
            // https://stackoverflow.com/a/10884757/2062838

            // can use orderfilehistory instace, will be add later

            var article = GetOrderArticle(ctx, orderID, projectID);

            if(article == null)
            {
                throw new InvalidArticleException($"The order [{orderID}] does not contain a valid article.");
            }

            var details = GetOrderArticles(new OrderArticlesFilter
            {
                OrderID = new List<int>() { orderID },
                ActiveFilter = OrderActiveFilter.All
            }, ProductDetails.None);

            var packCode = details.First().PackCode;

            using(var conn = connManager.OpenWebLinkDB())
            {
                var result = conn.Select<OrderToUpdateDTO>(@"
                                SELECT 
                                    o.ID as OrderID
                                    , o.OrderNumber
                                    , o.OrderStatus
                                    , op.ID as UpdatePropertiesID
                                    , op.IsActive
                                    , op.IsRejected as IsReject
                                    , op.CreatedDate as UpdatePropertiesCreatedAt
                                    , o.Productiontype
                                    , a.LabelID
                                    , a.EnableConflicts as ArticleHasEnableConflicts
                                    , o.IsStopped
                                    , d.PackCode
                                from CompanyOrders o
                                    join OrderUpdateProperties op on op.OrderID = o.ID
                                    join PrinterJobs j on j.CompanyOrderID = o.ID
                                    join Articles a on a.ID = j.ArticleID

	                                LEFT JOIN (
	                                    SELECT DISTINCT tt.PackCode, tt.PrinterJobID
	                                    FROM PrinterJobDetails tt
	                                ) d 	ON d.PrinterJobID = j.ID
                                where 
                                    o.OrderGroupID = @orderGroupID
                                    and o.OrderNumber = @orderNumber
                                    and o.ProjectID = @projectID
                                    and op.IsRejected = 0   
                                    and o.OrderStatus <> 6
                                    and o.OrderStatus <> 7
                                    and ((@categorizeArticle = 1 and a.CategoryID = @articleCategory and a.CategoryID is not null)
                                        or a.ArticleCode = @articleCode)
                                    and (@factoryID < 1 OR o.LocationID = @factoryID)
                                    and o.ProviderRecordID = @providerRecordId
                                    and (LEN(@packCode) < 1 OR d.PackCode = @packCode)
                                order by o.CreatedDate",
                    orderGroupID, orderNumber, projectID, /*orderID,*/ categorizeArticle, article.CategoryID ?? 0, article.ArticleCode, factoryID ?? 0, providerRecordId, packCode ?? "");
                return result;
            }
        }


        private IArticle GetOrderArticle(PrintDB ctx, int orderId, int projectId)
        {
            var job = ctx.PrinterJobs.FirstOrDefault(x => x.CompanyOrderID.Equals(orderId) && x.ProjectID.Equals(projectId));

            if(job != null)
            {
                var article = ctx.Articles.FirstOrDefault(x => x.ID.Equals(job.ArticleID) && x.ProjectID.Equals(projectId));
                if(article != null)
                    return article;
                else
                {
                    var defaultAricle = ctx.Articles
                        .AsNoTracking()
                        .Where(a => a.ProjectID == null && a.ArticleCode == Article.EMPTY_ARTICLE_CODE)
                        .FirstOrDefault();
                    if(defaultAricle != null && job.ArticleID == defaultAricle.ID)
                        return defaultAricle;
                }

            }

            return null;
        }


        public OrderToUpdateDTO GetConflictedOrderFor(int currentID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetConflictedOrderFor(ctx, currentID);
            }
        }


        public OrderToUpdateDTO GetConflictedOrderFor(PrintDB ctx, int currentID)
        {
            var orderInfo = GetProjectInfo(ctx, currentID);
            var comparerType = GetComparerType(ctx, currentID);
            var comparerMethod = comparerType != null && comparerType.ID != 0 && !string.IsNullOrEmpty(comparerType.Method.ToString()) ? comparerType.Method : ConflictMethod.Default;
            var categorizeArticle = comparerType != null ? comparerType.CategorizeArticle : false;

            var orderUpdateHistory = GetOrdersToUpdate(ctx, orderInfo.OrderGroupID, currentID, orderInfo.OrderNumber, orderInfo.ProjectID, comparerMethod, categorizeArticle, orderInfo.LocationID, orderInfo.ProviderRecordID, false);

            var conflictOrder = orderUpdateHistory
                .Where(o => o.IsActive.Equals(false) && o.IsReject.Equals(false))
                .OrderBy(o => o.OrderCreatedAt)
                .FirstOrDefault();

            if(conflictOrder == null) throw new NoNullAllowedException("Order in conflict not found");

            var job = ctx.PrinterJobs.FirstOrDefault(x => x.CompanyOrderID.Equals(conflictOrder.OrderID));
            var article = ctx.Articles.FirstOrDefault(x => x.ID.Equals(job.ArticleID));

            if(article.LabelID.HasValue)
            {
                conflictOrder.LabelID = article.LabelID.Value;
            }
            else
            {
                conflictOrder.LabelID = 0;
            }

            return conflictOrder;
        }


        public IEnumerable<OrderDetailDTO> GetOrderArticles(OrderArticlesFilter filter, ProductDetails addProductDetails = ProductDetails.None, List<string> productFields = null)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderArticles(ctx, filter, addProductDetails, productFields);
            }
        }


        public IEnumerable<OrderDetailDTO> GetOrderArticles(PrintDB ctx, OrderArticlesFilter filter, ProductDetails addProductDetails = ProductDetails.None, List<string> customProductFields = null)
        {

            if(!string.IsNullOrEmpty(filter.OrderNumber) && (filter.OrderGroupID > 0 || filter.OrderID.Count > 0))
            {
                log.LogWarning($"Esta consulta puede retornar detalles de diferentes Ordernes   OrderNumber [{filter.OrderNumber}], OrderGroupID [{filter.OrderGroupID}], OrderIds {string.Join(',', filter.OrderID)} ");

                throw new Exception("Invalid Filter to get OrderDetails");
            }

            var userData = factory.GetInstance<IUserData>();
            var packRepo = factory.GetInstance<IPackRepository>();
            var orderGroupRepo = factory.GetInstance<IOrderGroupRepository>();

            var project = orderGroupRepo.GetProjectBy(ctx, filter);

            var orderStatus = filter.OrderStatus != OrderStatusFilter.All ? (int)filter.OrderStatus : (int)OrderStatus.None;
            var excludeFromAllStatusOption = new List<OrderStatus>()
            {
                OrderStatus.Cancelled
            };



            using(var dynamicDB = connManager.CreateDynamicDB())
            {
                var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == project.ID && c.Name == Catalog.ORDERDETAILS_CATALOG select c).FirstOrDefault();

                var productCatalog = (from c in ctx.Catalogs where c.ProjectID == project.ID && c.Name == Catalog.VARIABLEDATA_CATALOG select c).FirstOrDefault();

                var OrderDetailDTOList = (
                    from DT_Order in ctx.CompanyOrders
                    join DT_PrinterJob in ctx.PrinterJobs on DT_Order.ID equals DT_PrinterJob.CompanyOrderID
                    join DT_Article in ctx.Articles on DT_PrinterJob.ArticleID equals DT_Article.ID
                    join DT_JobDetails in ctx.PrinterJobDetails on DT_PrinterJob.ID equals DT_JobDetails.PrinterJobID
                    join DT_Projects in ctx.Projects on DT_Order.ProjectID equals DT_Projects.ID
                    //join DT_Properties in ctx.OrderUpdateProperties on DT_Order.ID equals DT_Properties.OrderID

                    join DT_PropertyMap in ctx.OrderUpdateProperties on DT_Order.ID equals DT_PropertyMap.OrderID into DT_PropertiesMap
                    from DT_Properties in DT_PropertiesMap.DefaultIfEmpty()

                    join DT_lblMap in ctx.Labels on DT_Article.LabelID equals DT_lblMap.ID into DT_LabelsMap
                    from DT_Label in DT_LabelsMap.DefaultIfEmpty()

                    where
                  // buscar toda el order group, por numero o por ID o buscar articulos para un partial order individual
                  (
                        (!string.IsNullOrEmpty(filter.OrderNumber) && DT_Order.OrderNumber.Equals(filter.OrderNumber))
                        || (filter.OrderID.Count > 0 && filter.OrderID.Contains(DT_Order.ID))
                        || (filter.OrderID.Count <= 0 && filter.OrderGroupID > 0 && DT_Order.OrderGroupID.Equals(filter.OrderGroupID))
                    )

                    // by article type
                    && (
                        filter.ArticleType.Equals(ArticleTypeFilter.All)
                        || (filter.ArticleType.Equals(ArticleTypeFilter.Label) && DT_Article.LabelID != null)
                        || (filter.ArticleType.Equals(ArticleTypeFilter.Item) && DT_Article.LabelID == null)
                        || (filter.ArticleType.Equals(ArticleTypeFilter.ItemExtra) && DT_Article.LabelID == null && DT_JobDetails.QuantityRequested.Equals(0))
                        || (filter.ArticleType.Equals(ArticleTypeFilter.CareLabel) && (DT_Article.LabelID != null && DT_Label.Type == LabelType.CareLabel))
                        || (filter.ArticleType.Equals(ArticleTypeFilter.HangTag) && (DT_Article.LabelID != null && DT_Label.Type == LabelType.HangTag))

                    )

                        && (
                        filter.IncludeCompo.Equals(IncludeCompositionFilter.All)
                        || (filter.IncludeCompo.Equals(IncludeCompositionFilter.Yes) && DT_Article.LabelID != null && DT_Label.IncludeComposition.Equals(true))
                        || (filter.IncludeCompo.Equals(IncludeCompositionFilter.No) && DT_Article.LabelID != null && DT_Label.IncludeComposition.Equals(false))
                        )

                    && (
                                filter.ActiveFilter.Equals(OrderActiveFilter.All)
                            || (filter.ActiveFilter.Equals(OrderActiveFilter.Active)      && DT_Properties != null && DT_Properties.IsActive.Equals(true) && DT_Properties.IsRejected.Equals(false))
                            || (filter.ActiveFilter.Equals(OrderActiveFilter.Rejected)    && DT_Properties != null && DT_Properties.IsRejected.Equals(true))
                            || (filter.ActiveFilter.Equals(OrderActiveFilter.Pending)    && (DT_Properties == null || DT_Properties.IsActive.Equals(false) && DT_Properties.IsRejected.Equals(false)))
                            || (filter.ActiveFilter.Equals(OrderActiveFilter.NoRejected) && (DT_Properties == null || DT_Properties.IsRejected.Equals(false)))
                        )

                    && (
                        filter.Source.Equals(OrderSourceFilter.NotSet)
                        || (filter.Source.Equals(OrderSourceFilter.FromWeb) && DT_Order.Source == DocumentSource.Web)
                        || (filter.Source.Equals(OrderSourceFilter.FromFtp) && DT_Order.Source == DocumentSource.FTP)
                        || (filter.Source.Equals(OrderSourceFilter.FromApi) && DT_Order.Source == DocumentSource.API)
                        || (filter.Source.Equals(OrderSourceFilter.FromValidation) && DT_Order.Source == DocumentSource.Validation)
                        || (filter.Source.Equals(OrderSourceFilter.NotFromValidation) && DT_Order.Source != DocumentSource.Validation)
                        || (filter.Source.Equals(OrderSourceFilter.FromRepetition) && DT_Order.Source != DocumentSource.Repetition)

                    )

                    && (
                        filter.OrderStatus == OrderStatusFilter.All
                        || (filter.OrderStatus.Equals(OrderStatusFilter.None) && !excludeFromAllStatusOption.Contains(DT_Order.OrderStatus))
                        || DT_Order.OrderStatus == (OrderStatus)orderStatus
                    )

                    select new OrderDetailDTO
                    {
                        ArticleID = DT_Article.ID,
                        Article = DT_Article.Name,
                        ArticleCode = DT_Article.ArticleCode,
                        Description = DT_Article.Description,
                        Quantity = DT_JobDetails.Quantity,
                        QuantityRequested = DT_JobDetails.QuantityRequested,
                        ArticleBillingCode = DT_Article.BillingCode,
                        IsItem = DT_Article.LabelID == null || DT_Article.LabelID < 1 ? true : false,
                        UpdatedDate = DT_PrinterJob.UpdatedDate.ToCSVDateFormat(),
                        LabelID = DT_Article.LabelID,
                        Label = DT_Label != null ? DT_Label.Name : string.Empty,
                        LabelType = DT_Label != null ? DT_Label.Type : LabelType.Sticker, // TODO: if article is not a lable, this is not correct
                        LabelTypeStr = DT_Label != null ? DT_Label.Type.ToString() : string.Empty, // TODO - use Extension to get string and remove this field
                        HasRFID = DT_Label != null ? DT_Label.EncodeRFID : false,
                        RequiresDataEntry = DT_Label.RequiresDataEntry,
                        ProductDataID = DT_JobDetails.ProductDataID,
                        PrinterJobDetailID = DT_JobDetails.ID,
                        PrinterJobID = DT_PrinterJob.ID,
                        PackCode = DT_JobDetails.PackCode,
                        OrderID = DT_PrinterJob.CompanyOrderID,
                        ProjectID = DT_Order.ProjectID,
                        OrderGroupID = DT_Order.OrderGroupID,
                        OrderDataID = DT_Order.OrderDataID,
                        OrderNumber = DT_Order.OrderNumber,
                        OrderStatus = DT_Order.OrderStatus,
                        IsBilled = DT_Order.IsBilled,
                        MaxQuantityPercentage = DT_Projects.MaxQuantityPercentage,
                        MaxQuantity = DT_Projects.MaxQuantity,
                        AllowQuantityEdition = DT_Projects.AllowQuantityEdition,

                        MaxAllowed = (DT_Article.LabelID == null || DT_Article.LabelID < 1) ? CalculateMaxAllowed(DT_Order.Source, DT_JobDetails.QuantityRequested, (OrderQuantityEditionOption)DT_Projects.AllowExtrasDuringValidation, DT_Projects.MaxExtrasPercentage, DT_Projects.MaxExtras) : CalculateMaxAllowed(DT_Order.Source, DT_JobDetails.QuantityRequested, (OrderQuantityEditionOption)DT_Projects.AllowQuantityEdition, DT_Projects.MaxQuantityPercentage, DT_Projects.MaxQuantity),
                        MinAllowed = CalculateMinAllowed(userData, DT_JobDetails.QuantityRequested, DT_Order.OrderNumber, DT_Order.Source, DT_Projects.AllowQuantityZero),
                        HasPackCode = !string.IsNullOrEmpty(DT_JobDetails.PackCode),

                        SyncWithSage = DT_Article.SyncWithSage,
                        SageReference = DT_Article.SageRef,
                        DisplayField = GetGroupingColumn(DT_Label.GroupingFields),
                        LabelGroupingFields = GetLabelGroupingFields(DT_Label.GroupingFields),
                        IncludeComposition = DT_Label != null ? DT_Label.IncludeComposition : false,
                        LocationID = DT_Order.LocationID,
                        CategoryName = DT_Article.Category.Name,
                        Source = DT_Order.Source


                    })
                                        .OrderBy(ord => ord.ProductDataID) // the same order file data is received
                                        .ToList();


                var defaultProductDetails = new List<string> { "Barcode", "TXT1", "Size", "Color" };

                var articleIdList = OrderDetailDTOList.Where(w => !string.IsNullOrEmpty(w.PackCode)).Select(s => s.ArticleID).Distinct().ToList();

                if(OrderDetailDTOList.Count > 0)
                {
                    // pack joins
                    var packs = ctx.PackArticles.Include(p => p.Pack)
                        .Where(w => articleIdList.Any(a => a.Equals(w.ArticleID)))
                        .ToList();

                    var productDataList = OrderDetailDTOList.Select(s => s.ProductDataID); // looking for details only for results found
                    var allLabels = new List<string>();
                    var LabelGrupingFieldslist = OrderDetailDTOList.Select(l => l.LabelGroupingFields).Distinct();
                    OrderDetailDTOList.Select(l => l.LabelGroupingFields).Distinct().ForEach(la => allLabels.AddRange(la));
                    var productColumns = string.Empty;

                    var productFields = GetProductFields(defaultProductDetails, customProductFields, allLabels.Distinct().ToList(), addProductDetails);
                    productColumns = string.Join(',', productFields.Select(s => "Product." + s).ToArray());

                    productColumns = ", " + productColumns;

                    var productDetails = dynamicDB.Select(detailCatalog.CatalogID, $@"
                                            SELECT Details.ID AS ProductDataID {productColumns}
                                            FROM #TABLE Details
                                            INNER JOIN {productCatalog.Name}_{productCatalog.CatalogID} Product ON Details.Product = Product.ID
                                            WHERE Details.ID in ({string.Join(",", productDataList.ToArray())})
                                            ");

                    // complete detail info with variable data
                    OrderDetailDTOList.ForEach(d =>
                    {
                        d.IsDetailedArticle = ctx.ArticleDetails.Any(ad => ad.ArticleID == d.ArticleID && ad.CompanyID == filter.SendToCompanyID);

                        if(!d.IsItem)
                        {
                            var product = productDetails.Where(w => ((JObject)w).GetValue<int>("ProductDataID").Equals(d.ProductDataID)).First();

                            d.Size = ((JObject)product).GetValue<string>("Size");
                            d.UnitDetails = ((JObject)product).GetValue<string>("TXT1");
                            d.Color = ((JObject)product).GetValue<string>("Color");
                            d.GroupingField = ((JObject)product).GetValue<string>(d.DisplayField);

                            if(addProductDetails != ProductDetails.None)
                                d.ProductData = (JObject)product;
                        }
                        var packConfig = packs.Where(w => w.ArticleID.Equals(d.ArticleID) && w.Pack.PackCode.Equals(d.PackCode)).FirstOrDefault();

                        if(packConfig != null)
                        {
                            d.PackConfigQuantity = packConfig.Quantity;
                        }

                    });
                }

                return OrderDetailDTOList;
            }
        }

        private bool GetIsDetailedArticle(PrintDB ctx, int iD, int? sendToCompanyID)
        {
            if(!sendToCompanyID.HasValue)
                return false;

            return ctx.ArticleDetails.Any(ad => ad.ArticleID == iD && ad.CompanyID == sendToCompanyID.Value);

        }

        private List<string> GetProductFields(List<string> defaultProductFields, List<string> customProductFields, List<string> labelProductFields, ProductDetails productDetails)
        {
            if(productDetails == ProductDetails.Custom)
            {
                if(customProductFields != null && customProductFields.Count > 0)
                    defaultProductFields.AddRange(customProductFields);
                return defaultProductFields.Distinct().ToList();
            }

            if(productDetails == ProductDetails.Label)
            {
                if(labelProductFields != null && labelProductFields.Count > 0)
                    defaultProductFields.AddRange(labelProductFields);
                return defaultProductFields.Distinct().ToList();
            }

            if(productDetails == ProductDetails.All)
            {
                if(customProductFields != null && customProductFields.Count > 0)
                    defaultProductFields.AddRange(customProductFields);

                if(labelProductFields != null && labelProductFields.Count > 0)
                    defaultProductFields.AddRange(labelProductFields);
                return defaultProductFields.Distinct().ToList();
            }
            return defaultProductFields;
        }


        public List<OrderGroupSelectionDTO> GetArticleDetailSelection(IEnumerable<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter, bool addProductDetail = false, List<string> productFields = null)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetArticleDetailSelection(ctx, selection, filter, addProductDetail, productFields);
            }
        }


        public List<OrderGroupSelectionDTO> GetArticleDetailSelection(PrintDB ctx, IEnumerable<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter, bool addProductDetail = false, List<string> productFields = null)
        {
            var ret = new List<OrderGroupSelectionDTO>();

            foreach(var sel in selection)
            {
                var newSel = new OrderGroupSelectionDTO(sel);
                var groupfilter = new OrderArticlesFilter(filter);

                if(sel.Orders.Length > 0)
                {
                    groupfilter.OrderID = sel.Orders.ToList();
                }
                else
                {
                    groupfilter.OrderGroupID = sel.OrderGroupID;
                }

                var itemsSelected = GetOrderArticles(ctx, groupfilter, addProductDetail ? ProductDetails.Custom : ProductDetails.None, productFields);//.Where(w => sel.Orders.Count() > 0 && sel.Orders.Contains(w.OrderID));

                newSel.Details.AddRange(itemsSelected);
                ret.Add(newSel);
            }

            return ret;
        }



        public List<OrderGroupSelectionDTO> GetItemsExtrasDetailSelection(List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetItemsExtrasDetailSelection(ctx, selection, filter);
            }
        }


        public List<OrderGroupSelectionDTO> GetItemsExtrasDetailSelection(PrintDB ctx, List<OrderGroupSelectionDTO> selection, OrderArticlesFilter filter)
        {
            var ret = new List<OrderGroupSelectionDTO>();

            List<IEnumerable<OrderDetailDTO>> tasks = new List<IEnumerable<OrderDetailDTO>>();

            foreach(var sel in selection)
            {
                var groupfilter = new OrderArticlesFilter(filter);
                groupfilter.OrderGroupID = sel.OrderGroupID;
                groupfilter.OrderID = sel.Orders.ToList();

                var itemsSelected = GetOrderArticles(ctx, groupfilter, ProductDetails.None);

                //var data = GetByGroupingField(itemsSelected);
                //sel.Details.AddRange(data);

                sel.Details.AddRange(itemsSelected);

            }

            return selection;
        }


        private int CalculateMaxAllowed(DocumentSource source, int quantityRequested, OrderQuantityEditionOption isAllowQuantityEdition, int? maxPercent, int? maxFixed)
        {
            if(source == DocumentSource.Repetition || quantityRequested == 0)
            {
                return 50000; // TODO: add to the configuration this value, max allowed for repeats or items extras add during validation process
            }

            if(isAllowQuantityEdition == OrderQuantityEditionOption.NotAllow)
            {
                return quantityRequested;
            }

            if(isAllowQuantityEdition == OrderQuantityEditionOption.MaxPercentajeValue)
            {
                return (int)Math.Ceiling(quantityRequested * (1.0 + (float)maxPercent.Value / 100.0));
            }

            return quantityRequested + maxFixed.Value;
        }


        private int CalculateMinAllowed(IUserData userData, int quantityRequested, string orderNumber = "", DocumentSource source = default, bool AllowQuantityZero = false)
        {
            // is a repeted order, allow to change quantity in a free way for all users
            // IDT user always can change the value
            if(userData.IsIDT
                        || source == DocumentSource.Repetition
                        || Regex.IsMatch(orderNumber, Order.REPEAT_PATTERN)
                        || Regex.IsMatch(orderNumber, Order.DERIVATION_PATTERN)
                        || AllowQuantityZero)
            {
                return 0;
            }

            return quantityRequested;
        }


        public OrderInfoDTO GetProjectInfo(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProjectInfo(ctx, orderID);
            }
        }


        public OrderInfoDTO GetProjectInfo(PrintDB ctx, int orderID)
        {
            var q = OrderInfo(ctx, orderID);

            var order = q.Select(s => new OrderInfoDTO()
            {
                OrderID = s.ID,
                OrderGroupID = s.OrderGroupID,
                OrderNumber = s.OrderNumber,
                OrderDataID = s.OrderDataID,
                ProjectID = s.ProjectID,
                BrandID = s.Project.BrandID,
                CompanyID = s.Project.Brand.CompanyID,
                OrderStatus = s.OrderStatus,
                LocationID = s.LocationID,
                ProviderRecordID = s.ProviderRecordID,
                HasOrderWorkflow = s.HasOrderWorkflow,
                ItemID = s.ItemID,
                Source = s.Source
            }).AsNoTracking().FirstOrDefault();

            return order;
        }


        public OrderInfoDTO GetBillingInfo(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetBillingInfo(ctx, orderID);
            }
        }


        public OrderInfoDTO GetBillingInfo(PrintDB ctx, int orderID)
        {
            var q = OrderInfo(ctx, orderID);

            var result = q.AsQueryable<Order>()
                .Include(o => o.Location)
                .Join(ctx.OrderGroups,
                    ord => ord.OrderGroupID,
                    grp => grp.ID,
                    (o, g) => new { Order = o, OrderGroup = g }
                )
                .Join(ctx.Companies,
                    ord => ord.OrderGroup.BillToCompanyID,
                    company => company.ID,
                    (j1, cmp) => new { j1.Order, j1.OrderGroup, BillCompany = cmp }
                )
                //.Join(ctx.CompanyProviders,
                //    ord => ord.Order.CompanyID,
                //    prv => prv.CompanyID,
                //    (j2, provider) => new { j2.Order, j2.OrderGroup, j2.Company, Provider = provider }
                //)
                //.Where(w => w.Provider.ProviderCompanyID.Equals(w.OrderGroup.BillToCompanyID))

                .Select(s => new OrderInfoDTO()
                {
                    OrderID = s.Order.ID,
                    OrderNumber = s.Order.OrderNumber,
                    OrderGroupID = s.Order.OrderGroupID,
                    OrderDataID = s.Order.OrderDataID,
                    SendTo = s.Order.SendTo,// ??? este campo puede estar vacio, predomina el del OrderGroup
                    BillTo = s.BillCompany.Name,
                    IsBilled = s.Order.IsBilled,
                    IsBillable = s.Order.IsBillable,
                    ProjectID = s.Order.ProjectID,
                    ProjectCode = s.Order.Project.ProjectCode,
                    BrandID = s.Order.Project.BrandID,
                    CompanyID = s.Order.Project.Brand.CompanyID,
                    CompanyCode = s.Order.Project.Brand.Company.CompanyCode,
                    SendToCompanyID = s.Order.SendToCompanyID,
                    BillToCompanyID = s.BillCompany.ID,
                    SendToAddressID = s.Order.SendToAddressID,
                    OrderStatus = s.Order.OrderStatus,
                    BillToSyncWithSage = s.BillCompany.SyncWithSage,
                    BillToSageRef = s.BillCompany.SageRef,
                    DueDate = s.Order.DueDate,
                    MDOrderNumber = s.Order.MDOrderNumber,
                    CreatedDate = s.Order.CreatedDate,
                    ProductionType = s.Order.ProductionType,
                    LocationID = s.Order.LocationID,
                    FabricCode = s.Order.Location != null ? s.Order.Location.FactoryCode : null,
                    BrandName = s.Order.Project.Brand.Name,
                    BillToCompanyCode = s.BillCompany.CompanyCode,
                    ProviderRecordID = s.Order.ProviderRecordID

                }).AsNoTracking().FirstOrDefault();

            //var location = ctx.Locations.FirstOrDefault(x => x.ID == result.LocationID);

            //if (location != null)
            //{
            //    result.FabricCode = location.FactoryCode;
            //}

            return result;
        }


        private IQueryable<Order> OrderInfo(PrintDB ctx, int orderID)
        {
            var q = ctx.CompanyOrders
                    .Include(o => o.Project)
                    .Include(o => o.Project.Brand)
                    .Where(w => w.ID.Equals(orderID));

            return q;
        }


        public IAddress GetOrderShippingAddress(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderShippingAddress(ctx, orderID);
            }
        }


        public IAddress GetOrderShippingAddress(PrintDB ctx, int orderID)
        {
            //var order = GetByID(ctx, orderID);
            var order = GetBillingInfo(ctx, orderID);

            IAddress a;

            if(order.SendToAddressID.HasValue)
            {
                a = ctx.Addresses.FirstOrDefault(f => f.ID.Equals(order.SendToAddressID));
            }
            else
            {
                var addRepo = factory.GetInstance<IAddressRepository>();
                a = addRepo.GetDefaultByCompany(ctx, order.CompanyID);
            }

            return a;
        }


        public List<OrderGroupSelectionDTO> GetOrderShippingAddressByGroup(List<OrderGroupSelectionDTO> selection)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderShippingAddressByGroup(ctx, selection);
            }
        }


        public List<OrderGroupSelectionDTO> GetOrderShippingAddressByGroup(PrintDB ctx, List<OrderGroupSelectionDTO> selection)
        {
            var workSelection = selection.ToList();

            foreach(var sel in workSelection)
            {
                var groupRepo = factory.GetInstance<IOrderGroupRepository>();
                var addressRepo = factory.GetInstance<IAddressRepository>();

                var groupInfo = groupRepo.GetBillingInfo(ctx, sel.OrderGroupID);

                IAddress address;
                if(groupInfo.SendToAddressID.HasValue && groupInfo.SendToAddressID.Value > 0)
                {
                    address = addressRepo.GetByID(ctx, groupInfo.SendToAddressID.Value);
                }
                else
                {
                    address = addressRepo.GetDefaultByCompany(ctx, groupInfo.SendToCompanyID);
                }

                if(address != null)
                {
                    sel.ShippingAddressID = address.ID;
                }

                sel.SendToCompanyID = groupInfo.SendToCompanyID;
            }

            // For this PrintWeb Version - send selected ordergroup to the same order

            var validAddress = workSelection
                .Where(w => w.ShippingAddressID != null && w.ShippingAddressID > 0)
                .Select(s => s.ShippingAddressID).FirstOrDefault();

            if(validAddress != null)
            {// set to all groups de the same address
                workSelection.ForEach(e => e.ShippingAddressID = validAddress);
            }

            return workSelection;
        }


        public void AddExtraItemsByGroup(List<OrderGroupExtraItemsDTO> articleList, bool isActive = false)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                AddExtraItemsByGroup(ctx, articleList, isActive);
            }
        }


        public void AddExtraItemsByGroup(PrintDB ctx, List<OrderGroupExtraItemsDTO> articleList, bool isActive = false)
        {
            if(articleList.Count < 1)
                return;

            using(var dynamicDB = connManager.CreateDynamicDB())
            {
                Parallel.ForEach(articleList, (gp) =>
                {

                    var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == gp.ProjectID && c.Name == "OrderDetails" select c).FirstOrDefault();
                    var productCatalog = (from c in ctx.Catalogs where c.ProjectID == gp.ProjectID && c.Name == "VariableData" select c).FirstOrDefault();
                    var orderCatalog = (from c in ctx.Catalogs where c.ProjectID == gp.ProjectID && c.Name == "Orders" select c).FirstOrDefault();

                    var orderFields = orderCatalog.Fields;
                    var relationFieldID = orderFields.FirstOrDefault(x => x.Name == "Details").FieldID;


                    // only update or insert items with Quantity Assigned

                    foreach(var extra in gp.Items)
                    {
                        if(extra.PrinterJobDetailID.HasValue && extra.PrinterJobDetailID.Value > 0 && extra.PrinterJobID.HasValue && extra.PrinterJobID.Value > 0)
                        {
                            if(extra.Value <= 0)
                            {
                                _RemoveExtra(ctx, dynamicDB, extra, productCatalog.CatalogID, detailCatalog.CatalogID);
                            }
                            else
                            {
                                _UpdateExtra(ctx, dynamicDB, extra, productCatalog.CatalogID, detailCatalog.CatalogID, isActive);
                            }
                        }
                        else
                        {
                            if(extra.Value > 0)
                                _InsertExtra(ctx, dynamicDB, extra, gp.OrderGroupID, productCatalog.CatalogID, detailCatalog.CatalogID, orderCatalog.CatalogID, relationFieldID, isActive);
                        }
                    }
                });
            }
        }



        private void _RemoveExtra(PrintDB ctx, DynamicDB dynamicDB, ExtraQuantityState extra, int productCatalogID, int detailCatalogID)
        {
            //var pj = pjRepo.GetByID(extra.PrintJobID.Value);
            var order = GetByID(ctx, extra.OrderID.Value);

            var settingsRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();
            var settings = settingsRepo.GetByOrderID(ctx, extra.OrderID.Value);

            //var pjd = ctx.PrinterJobDetails.First(f => f.ID.Equals(extra.PrintJobDetailID));
            //// productdataid es el id de la tabla OrderDetails
            //var orderDetail = dynamicDB.SelectOne(detailCatalogID, pjd.ProductDataID);
            //dynamicDB.Delete(productCatalogID, orderDetail.GetValue<int>("Product"));
            //dynamicDB.Delete(detailCatalogID, pjd.ProductDataID);
            //ctx.PrinterJobDetails.Remove(new PrinterJobDetail() { ID = extra.PrintJobDetailID.Value });
            //ctx.PrinterJobs.Remove(new PrinterJob() { ID = extra.PrintJobID.Value });

            // DESACTIVAR ORDER

            order.OrderStatus = OrderStatus.Cancelled;
            settings.IsActive = false;
            settings.IsRejected = true; // hide order article in order report

            Update(ctx, order);

            settingsRepo.Update(ctx, settings);
        }


        private void _UpdateExtra(PrintDB ctx, DynamicDB dynamicDB, ExtraQuantityState extra, int productCatalogID, int detailCatalogID, bool isActive)
        {
            var pj = ctx.PrinterJobs.FirstOrDefault(f => f.ID.Equals(extra.PrinterJobID.Value));
            var pjd = ctx.PrinterJobDetails.FirstOrDefault(f => f.ID.Equals(extra.PrinterJobDetailID.Value));
            var order = ctx.CompanyOrders.FirstOrDefault(f => f.ID.Equals(extra.OrderID.Value));

            pj.Quantity = extra.Value;
            pjd.Quantity = extra.Value;
            order.Quantity = extra.Value;

            //ctx.CompanyOrders.Attach(order);
            //ctx.Entry(pj).Property(p => p.Quantity).IsModified = true;
            //ctx.PrinterJobs.Attach(pj);
            //ctx.Entry(pj).Property(p => p.Quantity).IsModified = true;
            //ctx.PrinterJobDetails.Attach(pjd);
            //ctx.Entry(pjd).Property(p => p.Quantity).IsModified = true;
            //ctx.PrinterJobDetails.Attach(pjd);
            //ctx.Entry(pjd).Property(p => p.Quantity).IsModified = true;

            ctx.SaveChanges();

            if(isActive)
            {
                var settingsRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();
                var settings = settingsRepo.GetByOrderID(ctx, extra.OrderID.Value);

                settings.IsActive = true;

                settingsRepo.Update(ctx, settings);
            }

            //ESTO ES NECESARIO ?
            /*var detail = dynamicDB.SelectOne(detailCatalogID, pjd.ProductDataID);
            detail["Quantity"] = extra.Value;
            dynamicDB.Update(detailCatalogID, Newtonsoft.Json.JsonConvert.SerializeObject(detail));*/

            // TODO: este campo de cantidad no existe en la tabla VariableData
            //var product = dynamicDB.SelectOne(productCatalogID, detail.GetValue<int>("Product"));
            //product["Quantity"] = extra.Value;
            //dynamicDB.Update(productCatalogID, Newtonsoft.Json.JsonConvert.SerializeObject(product));
        }



        private void _InsertExtra(PrintDB ctx, DynamicDB dynamicDB, ExtraQuantityState extra, int orderGroupID, int productCatalogID, int detailCatalogID, int orderCatalogID, int realtionFieldId, bool isActive)
        {
            var printJobRepo = factory.GetInstance<IPrinterJobRepository>();
            var projectRepo = factory.GetInstance<IProjectRepository>();
            var groupRepo = factory.GetInstance<IOrderGroupRepository>();
            var groupInfo = groupRepo.GetBillingInfo(ctx, orderGroupID);

            var companyRepo = factory.GetInstance<ICompanyRepository>();
            var sendTo = companyRepo.GetByID(ctx, groupInfo.SendToCompanyID);
            var billto = companyRepo.GetByID(ctx, groupInfo.BillToCompanyID);

            var articleEntity = ctx.Articles.First(f => f.ArticleCode.Equals(extra.ArticleCode) && f.ProjectID.Equals(groupInfo.ProjectID));

            // insert on Print_Data First
            dynamic product = new JObject();
            product.Barcode = "-";
            product.TXT1 = articleEntity.ArticleCode;
            product.TXT2 = "";
            product.TXT3 = "";
            product.Size = "";
            product.Color = "";
            product.Price = "";
            product.Currency = "";

            var productID = dynamicDB.Insert(productCatalogID, (JObject)product);

            dynamic detail = new JObject();
            detail.ArticleCode = extra.ArticleCode;
            detail.Quantity = extra.Value;
            detail.Product = productID;

            var detailID = dynamicDB.Insert(detailCatalogID, (JObject)detail);

            dynamic orderData = new JObject();
            orderData.OrderNumber = groupInfo.OrderNumber;
            orderData.OrderDate = DateTime.Now;
            orderData.BillTo = billto.CompanyCode;
            orderData.SendTo = sendTo.CompanyCode;

            var orderDataID = dynamicDB.Insert(orderCatalogID, (JObject)orderData);

            // register relation between order  and details in PrintData Database
            dynamicDB.InsertRel(orderCatalogID, detailCatalogID, realtionFieldId, orderDataID, detailID);

            // create partial order
            var partialOrder = CreateCustomPartialOrder(ctx, groupInfo, extra.Value, orderDataID, isActive);
            extra.OrderID = partialOrder.ID;

            var project = projectRepo.GetByID(partialOrder.ProjectID, true);

            if(project.EnableValidationWorkflow == true)
            {
                var validatorService = factory.GetInstance<IOrderSetValidatorService>();

                validatorService.Execute(groupInfo.OrderGroupID, partialOrder.ID, partialOrder.OrderNumber, project.ID, project.BrandID);

            }

            // insert on Print 

            IPrinterJob jobData = new PrinterJob()
            {
                CompanyID = groupInfo.CompanyID,
                CompanyOrderID = partialOrder.ID,
                ProjectID = groupInfo.ProjectID,
                ProductionLocationID = null,
                AssignedPrinter = null,
                ArticleID = extra.ArticleID,
                Quantity = extra.Value,
                Printed = 0,
                Errors = 0,
                Extras = 0,
                DueDate = DateTime.Now.AddDays(7),
                Status = JobStatus.Pending,
                AutoStart = false,
                CreatedDate = DateTime.Now,
                CompletedDate = null
            };

            //OrderInfoDTO orderInfo = GetProjectInfo(partialOrder.ID);

            var inserted = printJobRepo.AddExtraJob(ctx, jobData);

            var jobDetailData = new PrinterJobDetail()
            {
                PrinterJobID = inserted.ID,
                ProductDataID = detailID,
                Quantity = extra.Value,
                Extras = 0,
                PackCode = null,
                UpdatedDate = DateTime.Now
            };

            var detailInserted = printJobRepo.AddExtraDetailToJob(ctx, jobDetailData);

            extra.PrinterJobDetailID = detailInserted.ID;
            extra.PrinterJobID = inserted.ID;
        }

        /// <summary>
        /// Create Order without correct location 
        /// TODO: migrate method usign ProverderClientReference
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="groupInfo"></param>
        /// <param name="quantity"></param>
        /// <param name="orderDataID"></param>
        /// <param name="isActive"></param>
        /// <param name="fileGUID"></param>
        /// <returns></returns>
        public IOrder CreateCustomPartialOrder(PrintDB ctx, OrderInfoDTO groupInfo, int quantity, int orderDataID, bool isActive, Guid? fileGUID = null)
        {
            var userData = factory.GetInstance<IUserData>();
            var settingsRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();
            var companyRepo = factory.GetInstance<ICompanyRepository>();

            int? providerRecordID;
            int? locationID = null; // TODO: can calculated by provider
            var company = companyRepo.GetByID(groupInfo.SendToCompanyID);

            // TODO: this method return always first ProviderReferece, provider reference is required
            var sendtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, groupInfo.ProjectID, company.CompanyCode, out providerRecordID);

            var statusWithLoation = new List<OrderStatus>() { OrderStatus.InFlow, OrderStatus.Validated, OrderStatus.Billed, OrderStatus.ProdReady, OrderStatus.Printing, OrderStatus.Completed };


            var firstOrderInGroup = GetOrdersByGroupID(ctx, groupInfo.OrderGroupID)
                .Where(w => w.LocationID != null && statusWithLoation.Any(a => a == w.OrderStatus))
                .FirstOrDefault();

            if(firstOrderInGroup != null && firstOrderInGroup.ProviderRecordID != null)
            {
                if(providerRecordID != firstOrderInGroup.ProviderRecordID)
                {
                    log.LogWarning($"NOTIFY TO IT - Set Provider [{company.CompanyCode}] with different ClientReference [{providerRecordID}] -> system try to asign [{firstOrderInGroup.ProviderRecordID}]");
                }

                providerRecordID = firstOrderInGroup.ProviderRecordID;
                locationID = firstOrderInGroup.LocationID;
            }

            var partialOrder = Create();
            partialOrder.CompanyID = groupInfo.CompanyID;
            partialOrder.ProjectID = groupInfo.ProjectID;
            partialOrder.OrderDataID = orderDataID;
            partialOrder.OrderNumber = groupInfo.OrderNumber;
            partialOrder.OrderDate = DateTime.Now;// TODO: La fecha de la orden viene en algunos casos en el archivo
            partialOrder.UserName = userData.UserName;
            partialOrder.Source = DocumentSource.Validation;
            partialOrder.ProductionType = ProductionType.IDTLocation;
            partialOrder.Quantity = quantity;
            partialOrder.ConfirmedByMD = false;
            partialOrder.PreviewGenerated = false;
            partialOrder.BillToCompanyID = groupInfo.BillToCompanyID;
            partialOrder.SendToCompanyID = groupInfo.SendToCompanyID;
            partialOrder.SendToAddressID = groupInfo.SendToAddressID;
            partialOrder.SendTo = company.CompanyCode;
            partialOrder.ProviderRecordID = providerRecordID;
            partialOrder.OrderStatus = OrderStatus.InFlow;
            partialOrder.OrderGroupID = groupInfo.OrderGroupID;
            partialOrder.LocationID = locationID;
            partialOrder.HasOrderWorkflow = true;

            var insertedOrder = Insert(ctx, partialOrder);

            var data = settingsRepo.Create();

            data.IsActive = isActive;
            data.IsRejected = false;
            data.OrderID = insertedOrder.ID;

            var settings = settingsRepo.Insert(ctx, data);

            if(fileGUID != null)
            {
                SetOrderFile(insertedOrder.ID, fileGUID.Value);
            }
            else
            {
                var buffer = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(insertedOrder));
                MemoryStream fileContent = new MemoryStream(buffer);
                SetOrderFile(insertedOrder.ID, fileContent);
            }

            //events.Send(new OrderFileReceivedEvent(insertedOrder.OrderGroupID, insertedOrder.ID, insertedOrder.OrderNumber, insertedOrder.CompanyID, groupInfo.BrandID, insertedOrder.ProjectID));

            InsertItemOnWorkflow(insertedOrder).GetAwaiter().GetResult();

            return insertedOrder;
        }


        public async Task<IEnumerable<CompanyOrderDTO>> GetOrderReportPage(OrderReportFilter filter, CancellationToken ct = default(CancellationToken))
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return (await GetOrderReportPage(ctx, filter, ct)).ToList();
            }
        }


        public async Task<IEnumerable<CompanyOrderDTO>> GetOrderReportPage(PrintDB ctx, OrderReportFilter filter, CancellationToken ct = default(CancellationToken))
        {
            var providerRepo = factory.GetInstance<IProviderRepository>();
            var userData = factory.GetInstance<IUserData>();

            var details = (await GetOrderReportDetail(ctx, userData, filter, ct)).ToList();
            //filter.TotalRecords = GetOrderReportCount(ctx, userData, filter);   // TODO: calculate from data retrieved in the previous line to avoid double query??

            // add order groups registers
            Console.WriteLine("Realizando Busqueda {0}", filter.CompanyID);
            List<CompanyOrderDTO> page = new List<CompanyOrderDTO>();
            var groupsIDs = details.Select(s => s.OrderGroupID).Distinct().ToList();
            var groups = GetOrderReportGroupDetails(ctx, groupsIDs).ToList();

            foreach(var ID in groupsIDs)
            {
                var group = groups.First(f => f.OrderGroupID.Equals(ID));
                var groupDetail = details
                    .Where(w => w.OrderGroupID.Equals(group.OrderGroupID))
                    .OrderByDescending(o => o.RequireValidation)
                    .ThenBy(o => o.IsItem)
                    .ToList();

                // resort details
                if(groupDetail.Any(any => !string.IsNullOrEmpty(any.PackCode)))
                {
                    groupDetail = groupDetail
                        .OrderByDescending(o => o.PackCode)
                        .ThenBy(o => o.OrderDate).ToList();
                }

                if(groupDetail.Count() > 0)
                {
                    group.ProviderClientReference = groupDetail.ElementAt(0).ProviderClientReference;
                }

                //if some orders has a parent, fill de aggretaedQuantity to the parent
                //var containsParent = groupDetail.Any(x => x.ParentOrderID != null);
                //var pendingToSetAgregateValue = groupDetail.Where(w => w.ParentOrderID == null && !w.IsItem);

                var parentsOrdersIds = groupDetail
                    .Where(w => w.ParentOrderID != null)
                    .Select(x => x.ParentOrderID)
                    .Distinct()
                    .ToList();

                if(parentsOrdersIds.Any() && groupDetail.Any(w => parentsOrdersIds.Any(a => a == w.OrderID)))
                {

                    var ordersWithParent = ctx.CompanyOrders.Where(w => parentsOrdersIds.Any(a => a == w.ParentOrderID))
                        .ToList();

                    // only include derivation
                    var parentAgregationCount = ordersWithParent.Where(w => Regex.IsMatch(w.OrderNumber, Order.DERIVATION_PATTERN))
                        .GroupBy(g => g.ParentOrderID)
                        .Select(s => new
                        {
                            OrderID = s.Key,
                            Quantity = s.Sum(child => child.Quantity)
                        })
                        ;


                    // fill the info, how many are distributed  -> for Derived Orders
                    groupDetail.ForEach(d =>
                    {
                        var found = parentAgregationCount.FirstOrDefault(w => w.OrderID == d.OrderID);
                        if(found != null)
                        {
                            d.AggregatedQuantity = found.Quantity;
                        }
                    });
                }

                page.Add(group);
                page.AddRange(groupDetail);
            }

            return page;
        }

        // TODO: make async method, because report is a slowly process
        public async Task<MemoryStream> GetOrderFileReport(OrderReportFilter filter)
        {

            var rpt = new List<CSVCompanyOrderDTO>();

            using(var ctx = factory.GetInstance<PrintDB>())
            {

                var minDate = filter.OrderDate;
                var maxDate = filter.OrderDateTo;

                while(minDate < maxDate)
                {
                    filter.OrderDate = minDate;

                    if(minDate.AddDays(30) < maxDate)
                        filter.OrderDateTo = minDate.AddDays(30);
                    else
                        filter.OrderDateTo = maxDate;

                    minDate = filter.OrderDateTo.AddTicks(1);


                    var rptSection = await GetOrderFileReport(ctx, filter);

                    // convert to csv report columns interface
                    rpt.AddRange(rptSection.Select(s => new CSVCompanyOrderDTO(s)));


                }
            }

            // TODO: add project Name, location Name, vendor name

            var dtt = rpt.ToDataTable();

            using(var strm = new MemoryStream())
            {
                using(var writer = new StreamWriter(strm))
                {
                    Rfc4180Writer.CreateStream(dtt, writer, true);
                    strm.Position = 0;
                    return strm;
                }
            }
        }
        private async Task<IEnumerable<CompanyOrderDTO>> GetOrderFileReport(PrintDB ctx, OrderReportFilter filter)
        {
            var userData = factory.GetInstance<IUserData>();

            var details = await GetOrderReportDetail(ctx, userData, filter);

            return details;
        }


        private int GetOrderReportCount(PrintDB ctx, IUserData userData, OrderReportFilter filter)
        {
            ProductionType prodType = (ProductionType)filter.ProductionType;
            var excludeFromAllStatusOption = new List<OrderStatus>()
            {
                OrderStatus.Cancelled
            };

            var vendorID = 0;
            var showTestingOrders = false;
            // for current company and suppliers ?
            if(userData.SelectedCompanyID != 1)
            {
                vendorID = userData.SelectedCompanyID;
            }

            if(userData.CompanyID == 1)
            {
                showTestingOrders = true;
            }

            int q = (from j in ctx.PrinterJobs
                     join a in ctx.Articles on j.ArticleID equals a.ID
                     join o in ctx.CompanyOrders on j.CompanyOrderID equals o.ID
                     join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID
                     join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                     join project in ctx.Projects on o.ProjectID equals project.ID
                     join brand in ctx.Brands on project.BrandID equals brand.ID
                     // left join is not necesary to count
                     join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                     from provider in Providers.DefaultIfEmpty()

                     where
                     (
                        filter.OrderStatus == OrderStatus.Cancelled
                        || (
                            props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                            && grp.IsActive.Equals(true) && grp.IsRejected.Equals(false)
                        )
                    )
                    && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                    && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                    && (vendorID.Equals(0) || grp.SendToCompanyID == vendorID || brand.CompanyID == vendorID)
                    //&& (vendorID.Equals(0) || provider.CompanyID.Equals(vendorID) || provider.Parents.Contains($".{vendorID}."))
                    && (filter.CompanyID.Equals(0) || brand.CompanyID.Equals(filter.CompanyID))
                    && (filter.ProjectID.Equals(0) || project.ID.Equals(filter.ProjectID))
                    && ((filter.OrderStatus.Equals(OrderStatus.None) && !excludeFromAllStatusOption.Contains(o.OrderStatus))
                    || o.OrderStatus.Equals(filter.OrderStatus))
                    && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                    && (
                        filter.InConflict.Equals(ConflictFilter.Ignore)
                        || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                        || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                    )
                    && (
                        filter.IsBilled.Equals(BilledFilter.Ignore)
                        || (filter.IsBilled.Equals(BilledFilter.Yes) && o.IsBilled.Equals(true))
                        || (filter.IsBilled.Equals(BilledFilter.No) && o.IsBilled.Equals(false))
                    )
                    && (
                        filter.IsStopped.Equals(StopFilter.Ignore)
                        || filter.IsStopped.Equals(StopFilter.Stoped) && o.IsStopped.Equals(true)
                        || filter.IsStopped.Equals(StopFilter.NoStoped) && o.IsStopped.Equals(false)
                    )
                    && (
                        !string.IsNullOrEmpty(filter.OrderNumber)
                        || (o.CreatedDate >= filter.OrderDate && o.CreatedDate <= filter.OrderDateTo)
                      )
                    &&
                    (
                        prodType == ProductionType.All
                        || o.ProductionType == prodType
                    )
                    && (filter.FactoryID == 0 || o.LocationID == filter.FactoryID)
                    &&
                    (
                        string.IsNullOrEmpty(filter.ProviderClientReference)
                        || provider.ClientReference.Contains(filter.ProviderClientReference)
                    )
                    && (
                        string.IsNullOrEmpty(filter.OrderCategoryClient)
                        || grp.OrderCategoryClient.Contains(filter.OrderCategoryClient)
                    ) && (
                        showTestingOrders
                        || o.CompanyID != Company.TEST_COMPANY_ID && o.SendToCompanyID != Company.TEST_COMPANY_ID  // TODO: company for testing make a mark or configure
                    )
                     select o.ID).Count();

            return q;
        }


        private async Task<IEnumerable<CompanyOrderDTO>> GetOrderReportDetail(PrintDB ctx, IUserData userData, OrderReportFilter filter, CancellationToken ct = default(CancellationToken))
        {
            ProductionType prodType = (ProductionType)filter.ProductionType;
            var vendorID = 0;
            var showTestingOrders = false;

            var excludeFromAllStatusOption = new List<OrderStatus>()
            {
                OrderStatus.Cancelled
            };

            // for current company and suppliers ?
            if(userData.SelectedCompanyID != 1)
            {
                vendorID = userData.SelectedCompanyID;
            }

            // only for external location, set default factory
            if(userData.IsIDTExternal)
            {
                filter.FactoryID = userData.LocationID;
            }

            if(userData.CompanyID == 1)
            {
                showTestingOrders = true;
            }

            var qry = (
                from o in ctx.CompanyOrders
                join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                join j in ctx.PrinterJobs on o.ID equals j.CompanyOrderID
                join a in ctx.Articles on j.ArticleID equals a.ID
                join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID

                join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                from provider in Providers.DefaultIfEmpty()

                where
                (
                    filter.OrderStatus == OrderStatus.Cancelled
                    || filter.OrderStatus == OrderStatus.Received
                    || (
                        props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                        && grp.IsActive.Equals(true) && grp.IsRejected.Equals(false)
                    )
                )
                && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                && (vendorID.Equals(0) || o.SendToCompanyID == vendorID || o.CompanyID == vendorID || provider.CompanyID == vendorID)
                && (filter.CompanyID.Equals(0) || o.CompanyID.Equals(filter.CompanyID))
                && (filter.ProjectID.Equals(0) || o.ProjectID.Equals(filter.ProjectID))
                && ((filter.OrderStatus.Equals(OrderStatus.None) && o.OrderStatus != OrderStatus.Cancelled)
                || o.OrderStatus.Equals(filter.OrderStatus))
                && ((filter.DeliveryStatus < 0 || o.DeliveryStatusID.Equals(filter.DeliveryStatus)))
                && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                && (
                filter.InConflict.Equals(ConflictFilter.Ignore)
                || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                )
                && (
                    filter.IsBilled.Equals(BilledFilter.Ignore)
                    || (filter.IsBilled.Equals(BilledFilter.Yes) && o.IsBilled.Equals(true) && o.SyncWithSage == true)
                    || (filter.IsBilled.Equals(BilledFilter.No) && o.IsBilled.Equals(false) && o.SyncWithSage == false)
                )
                && (
                    filter.IsStopped.Equals(StopFilter.Ignore)
                    || filter.IsStopped.Equals(StopFilter.Stoped) && o.IsStopped.Equals(true)
                    || filter.IsStopped.Equals(StopFilter.NoStoped) && o.IsStopped.Equals(false)
                )
                && (
                !string.IsNullOrEmpty(filter.OrderNumber) || filter.OrderID > 0
                || (o.CreatedDate >= filter.OrderDate && o.CreatedDate <= filter.OrderDateTo)
                )
                &&
                (
                    prodType == ProductionType.All
                    || o.ProductionType == prodType
                )
                && (filter.FactoryID < 1 || o.LocationID == filter.FactoryID)
                &&
                (
                    string.IsNullOrEmpty(filter.ProviderClientReference)
                    || provider.ClientReference.Contains(filter.ProviderClientReference)
                )
                && (
                    showTestingOrders
                    || o.CompanyID != Company.TEST_COMPANY_ID && o.SendToCompanyID != Company.TEST_COMPANY_ID  // TODO: company for testing make a mark or configure
                )
                orderby o.CreatedDate descending, o.OrderGroupID
                select new CompanyOrderDTO()
                {

                    ArticleID = j.ArticleID,
                    ArticleCode = a.ArticleCode,
                    ArticleName = a.Name,
                    IsItem = a.LabelID == null ? true : false,
                    Quantity = j.Quantity,

                    IsStopped = o.IsStopped,
                    IsBilled = o.IsBilled,
                    IsInConflict = o.IsInConflict,

                    UserName = o.UserName,

                    SendToCompanyID = o.SendToCompanyID,
                    ProjectID = o.ProjectID,
                    OrderDate = o.CreatedDate,
                    OrderDueDate = o.DueDate,
                    OrderID = j.CompanyOrderID,
                    OrderStatus = o.OrderStatus,
                    OrderDataID = o.OrderDataID,
                    OrderStatusText = o.OrderStatus.GetText(userData.IsIDT),
                    Source = o.Source,
                    OrderGroupID = o.OrderGroupID,
                    OrderNumber = o.OrderNumber,
                    MDOrderNumber = string.IsNullOrEmpty(o.MDOrderNumber) ? string.Empty : o.MDOrderNumber,
                    IsGroup = false,
                    //RequireValidation = wzd != null,
                    //ValidationProgress = wzd != null ? wzd.Progress : 0,
                    NextStates = userData.IsSysAdmin ?
                    OrderUtil.NextStates(o.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst) :
                    OrderUtil.NextManualStates(o.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst),
                    SageReference = o.SageReference,
                    DeliveryStatus = o.DeliveryStatus,
                    CreditStatus = o.CreditStatus,
                    InvoiceStatus = o.InvoiceStatus,
                    SageStatus = o.SageStatus,

                    LocationID = o.LocationID,
                    //LocationName = loc.Name,
                    //FactoryCode = loc.FactoryCode,
                    ProductionType = o.ProductionType,
                    ParentOrderID = o.ParentOrderID,
                    ProviderClientReference = provider != null ? provider.ClientReference : string.Empty,
                    PrintJobId = j.ID,
                    SageItemRef = a.BillingCode,
                    WFItemID = o.ItemID,
                    ExceptionMessage = null,
                    DeliveryStatusId = o.DeliveryStatusID,
                    ValidationDate = o.ValidationDate,
                    AllowRepeatedOrders = o.AllowRepeatedOrders
                })
                .AsNoTracking();

            if(filter.CSV == true)
            {
                return await qry.ToListAsync(ct);
            }

            // TODO: can execute query count qnd query result at the same time
            filter.TotalRecords = await qry.CountAsync(ct);

            var details = await qry
                .Skip((filter.CurrentPage - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);


            #region Extra Info in One Loop
            _AddExtraInfoLoop(ctx, details, userData);
            #endregion Extra Info in One Loop

            return details;
        }

        private async Task<IEnumerable<CompanyOrderDTO>> GetDeliveryReportDetail(PrintDB ctx, IUserData userData, OrderReportFilter filter, CancellationToken ct = default(CancellationToken))
        {
            ProductionType prodType = (ProductionType)filter.ProductionType;
            var vendorID = 0;
            var showTestingOrders = false;

            var excludeFromAllStatusOption = new List<OrderStatus>()
            {
                OrderStatus.Cancelled
            };

            // for current company and suppliers ?
            if(userData.SelectedCompanyID != 1)
            {
                vendorID = userData.SelectedCompanyID;
            }

            // only for external location, set default factory
            if(userData.IsIDTExternal)
            {
                filter.FactoryID = userData.LocationID;
            }

            if(userData.CompanyID == 1)
            {
                showTestingOrders = true;
            }

            var qry = (
                from o in ctx.CompanyOrders
                join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                join j in ctx.PrinterJobs on o.ID equals j.CompanyOrderID
                join a in ctx.Articles on j.ArticleID equals a.ID
                join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID
                join loc in ctx.Locations on o.LocationID equals loc.ID
                join c in ctx.Companies on o.SendToCompanyID equals c.ID
                join oc in ctx.Companies on o.CompanyID equals oc.ID
                join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                from provider in Providers.DefaultIfEmpty()

                where
                (
                    filter.OrderStatus == OrderStatus.Cancelled
                    || filter.OrderStatus == OrderStatus.Received
                    || (
                        props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                        && grp.IsActive.Equals(true) && grp.IsRejected.Equals(false)
                    )
                )
                && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                && (vendorID.Equals(0) || o.SendToCompanyID == vendorID || o.CompanyID == vendorID || provider.CompanyID == vendorID)
                && (filter.CompanyID.Equals(0) || o.CompanyID.Equals(filter.CompanyID))
                && (filter.ProjectID.Equals(0) || o.ProjectID.Equals(filter.ProjectID))
                && ((filter.OrderStatus.Equals(OrderStatus.None) && o.OrderStatus != OrderStatus.Cancelled)
                || o.OrderStatus.Equals(filter.OrderStatus))
                && ((filter.DeliveryStatus < 0 || o.DeliveryStatusID.Equals(filter.DeliveryStatus)))
                && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                && (
                filter.InConflict.Equals(ConflictFilter.Ignore)
                || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                )
                && (
                    filter.IsBilled.Equals(BilledFilter.Ignore)
                    || (filter.IsBilled.Equals(BilledFilter.Yes) && o.IsBilled.Equals(true) && o.SyncWithSage == true)
                    || (filter.IsBilled.Equals(BilledFilter.No) && o.IsBilled.Equals(false) && o.SyncWithSage == false)
                )
                && (
                    filter.IsStopped.Equals(StopFilter.Ignore)
                    || filter.IsStopped.Equals(StopFilter.Stoped) && o.IsStopped.Equals(true)
                    || filter.IsStopped.Equals(StopFilter.NoStoped) && o.IsStopped.Equals(false)
                )
                && (
                !string.IsNullOrEmpty(filter.OrderNumber) || filter.OrderID > 0
                || (o.CreatedDate >= filter.OrderDate && o.CreatedDate <= filter.OrderDateTo)
                )
                &&
                (
                    prodType == ProductionType.All
                    || o.ProductionType == prodType
                )
                && (filter.FactoryID < 1 || o.LocationID == filter.FactoryID)
                &&
                (
                    string.IsNullOrEmpty(filter.ProviderClientReference)
                    || provider.ClientReference.Contains(filter.ProviderClientReference)
                )
                && (
                    showTestingOrders
                    || o.CompanyID != Company.TEST_COMPANY_ID && o.SendToCompanyID != Company.TEST_COMPANY_ID  // TODO: company for testing make a mark or configure
                )
				&& o.DeliveryStatusID != DeliveryStatus.Delivered

				orderby o.CreatedDate descending, o.OrderGroupID
                select new CompanyOrderDTO()
                {
                    ArticleID = j.ArticleID,
                    ArticleCode = a.ArticleCode,
                    ArticleName = a.Name,
                    IsItem = a.LabelID == null ? true : false,
                    Quantity = j.Quantity,
                    IsStopped = o.IsStopped,
                    IsBilled = o.IsBilled,
                    IsInConflict = o.IsInConflict,
                    UserName = o.UserName,
                    SendToCompanyID = o.SendToCompanyID,
                    ProjectID = o.ProjectID,
                    OrderDate = o.CreatedDate,
                    OrderDueDate = o.DueDate,
                    OrderID = j.CompanyOrderID,
                    OrderStatus = o.OrderStatus,
                    OrderDataID = o.OrderDataID,
                    OrderStatusText = o.OrderStatus.GetText(userData.IsIDT),
                    Source = o.Source,
                    OrderGroupID = o.OrderGroupID,
                    OrderNumber = o.OrderNumber,
                    MDOrderNumber = string.IsNullOrEmpty(o.MDOrderNumber) ? string.Empty : o.MDOrderNumber,
                    IsGroup = false,
                    NextStates = userData.IsSysAdmin ?
                    OrderUtil.NextStates(o.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst) :
                    OrderUtil.NextManualStates(o.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst),
                    SageReference = o.SageReference,
                    DeliveryStatus = o.DeliveryStatus,
                    CreditStatus = o.CreditStatus,
                    InvoiceStatus = o.InvoiceStatus,
                    SageStatus = o.SageStatus,
                    SendTo = c.Name,
                    CompanyName = oc.Name,
                    LocationID = o.LocationID,
                    LocationName = loc.Name,
                    FactoryCode = loc.FactoryCode,
                    ProductionType = o.ProductionType,
                    ParentOrderID = o.ParentOrderID,
                    ProviderClientReference = provider != null ? provider.ClientReference : string.Empty,
                    PrintJobId = j.ID,
                    SageItemRef = a.BillingCode,
                    WFItemID = o.ItemID,
                    ExceptionMessage = null,
                    DeliveryStatusId = o.DeliveryStatusID
                })
                .AsNoTracking();

            return await qry.ToListAsync(ct);
        }

        #region order detail extra info 
        // add all info inner less loops posible
        private void _AddExtraInfoLoop(PrintDB ctx, List<CompanyOrderDTO> details, IUserData userData)
        {
            // map for faster loops


            var ordersMap = new Dictionary<int, CompanyOrderDTO>();

            details.ForEach(d => ordersMap.TryAdd(d.OrderID, d));

            // add packcode to the order list
            var pjIds = details.Select(d => d.PrintJobId);

            var locations = ctx.Locations
                .Where(w => details.Select(s => s.LocationID).Distinct().Any(a => a == w.ID))
                .Select(s => s)
                .AsNoTracking()
                .ToList();

            var wizards = ctx.Wizards
                .Where(w => ordersMap.Keys.Any(a => a == w.OrderID))
                .Select(s => s)
                .AsNoTracking()
                .ToList();


            var pjPacks = ctx.PrinterJobDetails
                    .Where(w => pjIds.Any(a => w.PrinterJobID == a))
                    .Select(s => new
                    {
                        PrinterJobID = s.PrinterJobID,
                        PackCode = s.PackCode
                    })
                    .Distinct()
                    .ToList();

            var wf = apm.GetWorkflowAsync("Order Processing").Result;
            var itemIds = details.Where(p => p.WFItemID.HasValue).Select((p => p.WFItemID.Value));
            var wfQueries = factory.GetInstance<IWorkflowQueries>();
            var exceptionMessages = wfQueries.GetItemsLastExceptionsAsync(wf.WorkflowID, itemIds).Result;


            details.ForEach(d =>
            {

                // add calculated properties
                d.OrderStatusText = d.OrderStatus.GetText(userData.IsIDT);

                d.NextStates = userData.IsSysAdmin ?
                   OrderUtil.NextStates(d.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst) :
                   OrderUtil.NextManualStates(d.OrderStatus, NextOrderStateIncludeCurrentOption.AtFirst);

                d.MDOrderNumber = string.IsNullOrEmpty(d.MDOrderNumber) ? string.Empty : d.MDOrderNumber;


                // Add factory info
                var location = locations.FirstOrDefault(f => f.ID == d.LocationID);

                if(location != null)
                {
                    d.FactoryCode = location.FactoryCode;
                    d.LocationName = location.Name;
                }

                // add wizard progress
                var wizard = wizards.FirstOrDefault(f => f.OrderID == d.OrderID);

                if(wizard != null)
                {
                    d.RequireValidation = true;
                    d.ValidationProgress = wizard.Progress;
                }

                // add pack info
                var packInfo = pjPacks.FirstOrDefault(f => f.PrinterJobID == d.PrintJobId);

                if(packInfo != null) d.PackCode = packInfo.PackCode;

                // add the last error messages from order processing
                var msg = exceptionMessages.FirstOrDefault(f => f.ItemId == d.WFItemID);

                if(msg != null) d.ExceptionMessage = msg.LastErrorMessage;


            });
        }
        private void _PageFillWithPackInfo(PrintDB ctx, IList<CompanyOrderDTO> details)
        {
            // add packcode to the order list
            var pjIds = details.Select(s => s.PrintJobId).ToList();

            if(pjIds.Count() > 0)
            {
                var pjPacks = ctx.PrinterJobDetails
                    .Where(w => pjIds.Any(a => w.PrinterJobID == a))
                    .Select(s => new
                    {
                        PrinterJobID = s.PrinterJobID,
                        PackCode = s.PackCode
                    });

                pjPacks.ForEach(p =>
                {
                    var d = details.FirstOrDefault(f => f.PrintJobId.Equals(p.PrinterJobID));
                    if(d != null)
                        d.PackCode = p.PackCode;
                });
            }
        }

        private void _PageFillWithExceptionInfo(List<CompanyOrderDTO> orders)
        {
            var wf = apm.GetWorkflowAsync("Order Processing").Result;
            var itemIds = orders.Where(p => p.WFItemID.HasValue).Select((p => p.WFItemID.Value));
            var wfQueries = factory.GetInstance<IWorkflowQueries>();
            var exceptionMessages = wfQueries.GetItemsLastExceptionsAsync(wf.WorkflowID, itemIds).Result;

            orders.Join(exceptionMessages, o => o.WFItemID, orderWfExceptionInfo => orderWfExceptionInfo.ItemId, (d, i) =>
            {
                d.ExceptionMessage = i.LastErrorMessage;
                return d;
            }).ToList(); // si no se llama ToList no se ejecuta la lambda
        }

        private void _PageFillWidthLocationInfo(PrintDB ctx, List<CompanyOrderDTO> details)
        {
            var locations = ctx.Locations
                .Where(w => details.Select(s => s.LocationID).Any(a => a == w.ID))
                .Select(s => s)
                .AsNoTracking()
                .ToList();

            details.Join(locations, d => d.LocationID, l => l.ID, (d, l) =>
            {
                d.FactoryCode = l.FactoryCode;
                d.LocationName = l.Name;
                return d;
            }).ToList(); // si no se llama ToList no se ejecuta la lambda
        }

        private void _PageFillWidthWizardInfo(PrintDB ctx, Dictionary<int, CompanyOrderDTO> detailMap)
        {
            var wizards = ctx.Wizards
                .Where(w => detailMap.Keys.Any(a => a == w.OrderID))
                .Select(s => s)
                .AsNoTracking()
                .ToList();

            wizards.ForEach(wzd =>
            {
                detailMap.TryGetValue(wzd.OrderID, out var d);// always found
                d.RequireValidation = true;
                d.ValidationProgress = wzd.Progress;
            });
        }

        #endregion order detail extra info 
        private IEnumerable<CompanyOrderDTO> GetOrderReportGroupDetails(PrintDB ctx, IEnumerable<int> groupIDs)
        {

            var groups = from og in ctx.OrderGroups
                         join pj in ctx.Projects on og.ProjectID equals pj.ID
                         join bnd in ctx.Brands on pj.BrandID equals bnd.ID
                         join cpn in ctx.Companies on bnd.CompanyID equals cpn.ID
                         join sendTo in ctx.Companies on og.SendToCompanyID equals sendTo.ID
                         join billTo in ctx.Companies on og.BillToCompanyID equals billTo.ID

                         // left join providers with miltiple keys
                         //join prvmap in ctx.CompanyProviders on new { k1 = bnd.CompanyID, k2 = sendTo.ID } equals new { k1 = prvmap.CompanyID, k2 = prvmap.ProviderCompanyID } into Providers
                         //join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                         //from provider in Providers.DefaultIfEmpty()

                         where groupIDs.Contains(og.ID)
                         select new CompanyOrderDTO()
                         {
                             OrderNumber = og.OrderNumber,
                             CompanyID = cpn.ID,
                             CompanyName = cpn.Name,
                             CompanyCode = cpn.CompanyCode,
                             BrandID = bnd.ID,
                             Brand = bnd.Name,
                             BrandCode = string.Empty,
                             ProjectID = pj.ID,
                             Project = pj.Name,
                             ProjectCode = pj.ProjectCode,

                             SendToCompanyID = sendTo.ID,
                             SendToCode = sendTo.CompanyCode,
                             SendTo = sendTo.Name,

                             BillToCompanyID = billTo.ID,
                             BillToCode = billTo.CompanyCode,
                             BillTo = billTo.Name,

                             IsCompleted = og.IsCompleted,
                             CompletedDate = og.CompletedDate,
                             OrderDate = og.CreatedDate,
                             OrderGroupID = og.ID,
                             IsGroup = true,

                             ProviderClientReference = string.Empty,

                             MDOrderNumber = og.ERPReference/*,
                             OrderCategoryClient = og.OrderCategoryClient*/

                         };

            return groups;
        }


        private List<CompanyOrderDTO> CalculateGroupOrderReport(List<CompanyOrderDTO> result)
        {
            var lastGroup = "";
            List<CompanyOrderDTO> grouped = new List<CompanyOrderDTO>();

            foreach(var r in result.ToList())
            {
                if(r.OrderNumber != lastGroup)
                {
                    var partials = result.Where(w => w.OrderNumber.Equals(r.OrderNumber));

                    grouped.Add(new CompanyOrderDTO()
                    {

                        OrderNumber = r.OrderNumber,
                        Quantity = partials.Sum(s => s.Quantity)

                    });
                }
            }

            return grouped;
        }

        /// <summary>
        /// use for check for company orders registerd in Sage
        /// return order open in sage in the las 30 days
        /// </summary>
        public IEnumerable<OrderDetailDTO> GetRegisteredInSage()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetRegisteredInSage(ctx);
            }
        }


        public IEnumerable<OrderDetailDTO> GetRegisteredInSage(PrintDB ctx)
        {
            var now = DateTime.Now;
            var maxDaysToTracking = 30;

            var q = ctx.CompanyOrders
                .Where(w => w.SyncWithSage.Equals(true))
                .Where(w => w.SageStatus == SageOrderStatus.Open)
                .Where(w => w.RegisteredOn != null && w.RegisteredOn.Value.AddDays(maxDaysToTracking) >= now)
                .Select(s => new OrderDetailDTO()
                {
                    OrderID = s.ID,
                    OrderGroupID = s.OrderGroupID,
                    SageReference = s.SageReference
                });

            return q.ToList();
        }


        private string GetGroupingColumn(string groupingFields)
        {
            // NOTE: Any error at this point is due to missconfiguration, to fix the problem fix the system configuration, then reprocess the print package. Until that is done, this bit of code will keep throwing and prevent the order from moving to the Pending state...
            if(String.IsNullOrWhiteSpace(groupingFields))
                return "Barcode";
            var grouping = JsonConvert.DeserializeObject<GroupingColumnInfo>(groupingFields);
            if(String.IsNullOrWhiteSpace(grouping.GroupingFields))
                return "Barcode";
            string[] tokens = grouping.GroupingFields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return tokens[0];
        }

        private List<string> GetLabelGroupingFields(string groupingFields)
        {
            // NOTE: Any error at this point is due to missconfiguration, to fix the problem fix the system configuration, then reprocess the print package. Until that is done, this bit of code will keep throwing and prevent the order from moving to the Pending state...
            if(String.IsNullOrWhiteSpace(groupingFields))
                return new List<string>() { "Barcode" };
            var grouping = JsonConvert.DeserializeObject<GroupingColumnInfo>(groupingFields);
            if(String.IsNullOrWhiteSpace(grouping.GroupingFields))
                return new List<string>() { "Barcode" };
            string[] tokens = grouping.GroupingFields.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return tokens.ToList<string>();
        }


        public class GroupingColumnInfo
        {
            public string GroupingFields;
            public string DisplayFields;
        }

        /// <summary>
        /// TODO: why this grouping is required ?
        /// if you try to validate lines in the order with the same article, this will be joined here
        /// that is a problem
        /// </summary>
        /// <param name="itemsSelected"></param>
        /// <returns></returns>
        private IEnumerable<OrderDetailDTO> GetByGroupingField(IEnumerable<OrderDetailDTO> itemsSelected)
        {

            var grouped = itemsSelected.GroupBy(g1 => new { g1.ArticleID, g1.GroupingField });

            return grouped.Select(x => new OrderDetailDTO
            {
                ArticleID = x.Max(f => f.ArticleID),
                Article = x.Max(f => f.Article),
                ArticleCode = x.Max(f => f.ArticleCode),
                Description = x.Max(f => f.Description),
                Quantity = x.Sum(f => f.Quantity),
                QuantityRequested = x.Sum(f => f.QuantityRequested),
                ArticleBillingCode = x.Max(f => f.ArticleBillingCode),
                IsItem = x.Max(f => f.IsItem),
                UpdatedDate = x.Max(f => f.UpdatedDate),
                LabelID = x.Max(f => f.LabelID),
                Label = x.Max(f => f.Label),
                LabelTypeStr = x.Max(f => f.LabelTypeStr),
                RequiresDataEntry = x.Max(f => f.RequiresDataEntry),
                ProductDataID = x.Max(f => f.ProductDataID),
                PrinterJobDetailID = x.Max(f => f.PrinterJobDetailID),
                PrinterJobID = x.Max(f => f.PrinterJobID),
                PackCode = x.Max(f => f.PackCode),
                OrderID = x.Max(f => f.OrderID),
                ProjectID = x.Max(f => f.ProjectID),
                OrderGroupID = x.Max(f => f.OrderGroupID),
                OrderNumber = x.Max(f => f.OrderNumber),
                OrderStatus = x.Max(f => f.OrderStatus),
                IsBilled = x.Max(f => f.IsBilled),
                //MaxAllowed = CalculateMaxAllowed( x.Min(f => f.)  x.Sum(f => f.QuantityRequested), (OrderQuantityEditionOption)x.Max(f => f.AllowQuantityEdition), x.Max(f => f.MaxQuantityPercentage), x.Max(f => f.MaxQuantity)),
                MaxAllowed = 0,
                MinAllowed = x.Max(f => f.MinAllowed),
                HasPackCode = x.Max(f => f.HasPackCode),
                SyncWithSage = x.Max(f => f.SyncWithSage),
                SageReference = x.Max(f => f.SageReference),
                UnitDetails = x.Max(f => f.UnitDetails),
                PackConfigQuantity = x.Max(f => f.PackConfigQuantity),
                Size = x.Max(f => f.Size),
                CategoryName = x.Max(f => f.CategoryName)
            }).ToList();
        }

        public IEnumerable<CompanyOrderDTO> GetPendingOrdersForFactory(IEnumerable<int> currentOrders, int locationID, double deltaTimeHours)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetPendingOrdersForFactory(ctx, currentOrders, locationID, deltaTimeHours);
            }
        }

        public IEnumerable<CompanyOrderDTO> GetPendingOrdersForFactory(PrintDB ctx, IEnumerable<int> currentOrders, int locationID, double deltaTimeHours)
        {
            var fromDate = DateTime.Now.AddHours(-1 * deltaTimeHours);
            var pending = ctx.CompanyOrders
                .Join(ctx.CompanyProviders, o => o.ProviderRecordID, p => p.ID, (order, provider) => new { Order = order, Provider = provider })
                .Join(ctx.Companies, j1 => j1.Provider.ProviderCompanyID, c => c.ID, (join1, c) => new { join1.Order, join1.Provider, Company = c })
                .Join(ctx.PrinterJobs, j2 => j2.Order.ID, pj => pj.CompanyOrderID, (join2, pj) => new { join2.Order, join2.Provider, join2.Company, ArticleId = pj.ArticleID })
                .Join(ctx.Articles, j3 => j3.ArticleId, ar => ar.ID, (join3, article) => new { join3.Order, join3.Provider, join3.Company, join3.ArticleId, Article = article })
                .Join(ctx.Companies, j4 => j4.Order.CompanyID, c2 => c2.ID, (join4, c2) => new { join4.Order, join4.Provider, join4.Company, join4.ArticleId, join4.Article, RequestedBy = c2 })
                .Where(w => w.Order.LocationID == locationID && !currentOrders.Contains(w.Order.ID) && w.Order.ValidationDate >= fromDate && w.Order.OrderStatus == OrderStatus.ProdReady)
                .Select(s => new CompanyOrderDTO()
                {
                    OrderID = s.Order.ID,
                    OrderNumber = s.Order.OrderNumber,
                    SendToCompanyID = s.Order.SendToCompanyID,
                    ProviderClientReference = s.Provider.ClientReference,
                    SendTo = s.Company.Name,
                    ArticleCode = s.Article.ArticleCode,
                    ProjectPrefix = s.Order.ProjectPrefix,
                    LocationID = s.Order.LocationID,
                    CompanyName = s.RequestedBy.Name,
                    MDOrderNumber = s.Order.MDOrderNumber,
                    ValidationDate = s.Order.ValidationDate // order ready to print, has been validated

                })
                .ToList();

            return pending;
        }

        public void SyncOrderWithFactory(IEnumerable<CompanyOrderDTO> orders)
        {

            orders.ToList().ForEach(order =>
            {
                events.Send(new PrintPackageReadyEvent(order.OrderID, order.LocationID.Value, order.ProjectPrefix));
            });

        }

        public IEnumerable<OrderPrinterJobDetailDTO> GetDetailsByLabel(int orderID, int labelID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetDetailsByLabel(ctx, orderID, labelID);
            }
        }

        public IEnumerable<OrderPrinterJobDetailDTO> GetDetailsByLabel(PrintDB ctx, int orderID, int labelID)
        {
            /* Ingnore LabelID 20260127*/

            var q = ctx.CompanyOrders
                .Join(ctx.PrinterJobs, ord => ord.ID, job => job.CompanyOrderID, (o, j) => new { Orders = o, PrinterJobs = j })
                .Join(ctx.Articles, j1 => j1.PrinterJobs.ArticleID, art => art.ID, (j1, a) => new { j1.Orders, j1.PrinterJobs, Articles = a })
                .Join(ctx.PrinterJobDetails, j2 => j2.PrinterJobs.ID, jd => jd.PrinterJobID, (j2, jd) => new { j2.Orders, j2.PrinterJobs, j2.Articles, PrinterJobDetails = jd })
                .Where(w => w.PrinterJobs.CompanyOrderID.Equals(orderID))
                //.Where(w => w.Articles.LabelID.Equals(labelID))
                .Where(w => w.Orders.OrderStatus != OrderStatus.Cancelled)
                .Select(s => new OrderPrinterJobDetailDTO()
                {
                    OrderID = s.Orders.ID,
                    ArticleCode = s.Articles.ArticleCode,
                    ProductDataID = s.PrinterJobDetails.ProductDataID,
                    ID = s.PrinterJobDetails.ProductDataID,
                    Quantity = s.PrinterJobDetails.Quantity,
                    PackCode = s.PrinterJobDetails.PackCode
                });

            return q.ToList();

        }


        public IEnumerable<IOrder> GetOrdersByGroupID(int orderGroupID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrdersByGroupID(ctx, orderGroupID);
            }
        }

        // TODO: add more filters to get only active
        public IEnumerable<IOrder> GetOrdersByGroupID(PrintDB ctx, int orderGroupID)
        {
            var q = ctx.CompanyOrders
                .Where(w => w.OrderGroupID.Equals(orderGroupID))
                .ToList();

            return q;
        }

        public IEnumerable<IOrder> GetOrderWithSharedData(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderWithSharedData(ctx, orderID).ToList();
            }
        }

        public IEnumerable<IOrder> GetOrderWithSharedData(PrintDB ctx, int orderID)
        {
            var sharedData = ctx.CompanyOrders
                .Join(ctx.CompanyOrders,
                    r1 => r1.OrderDataID,
                    r2 => r2.OrderDataID,
                    (shared, origin) => new { Result = shared, Like = origin })
                .Where(w => w.Like.ID == orderID)
                .Where(w => w.Like.ProjectID == w.Result.ProjectID)
                .Select(s => s.Result);

            return sharedData;
        }

        public IEnumerable<IOrder> GetEncodedByProjectInStatusBetween(int projectID, IEnumerable<OrderStatus> orderStatus, DateTime from, DateTime to)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetEncodedByProjectInStatusBetween(ctx, projectID, orderStatus, from, to);
            }
        }


        public IEnumerable<IOrder> GetEncodedByProjectInStatusBetween(PrintDB ctx, int projectID, IEnumerable<OrderStatus> orderStatus, DateTime from, DateTime to)
        {
            var q = ctx.CompanyOrders
                .Join(ctx.PrinterJobs, o => o.ID, pj => pj.CompanyOrderID, (o, pj) => new { Order = o, PrinterJob = pj })
                .Join(ctx.Articles, j1 => j1.PrinterJob.ArticleID, a => a.ID, (jn, a) => new { jn.Order, jn.PrinterJob, Article = a })
                .Join(ctx.Labels, j2 => j2.Article.LabelID, l => l.ID, (jn, l) => new { jn.Order, jn.PrinterJob, jn.Article, Label = l })
                .Where(w => w.Order.ProjectID == projectID)
                .Where(w => orderStatus.Any(a => a == w.Order.OrderStatus))
                .Where(w => w.Order.UpdatedDate >= from && w.Order.UpdatedDate <= to)
                .Where(w => w.Article.LabelID != null && w.Label.EncodeRFID)

                .Select(s => s.Order)
                .AsNoTracking()

                .ToList();


            return q;
        }

        [Obsolete("actualizar firma utilice ProductDetails")]
        public IEnumerable<OrderDetailDTO> GetOrderArticles(OrderArticlesFilter filter, bool addProductDetails = false, List<string> productFields = null)
        {
            var EnumProductDetails = ProductDetails.None;
            if(addProductDetails)
            {
                EnumProductDetails = ProductDetails.Custom;
            }
            return GetOrderArticles(filter, EnumProductDetails, productFields);
        }

        [Obsolete("actualizar firma utilice ProductDetails")]
        public IEnumerable<OrderDetailDTO> GetOrderArticles(PrintDB ctx, OrderArticlesFilter filter, bool addProductDetails = false, List<string> productFields = null)
        {
            var EnumProductDetails = ProductDetails.None;
            if(addProductDetails)
            {
                EnumProductDetails = ProductDetails.Custom;
            }
            return GetOrderArticles(ctx, filter, EnumProductDetails, productFields);
        }


        /// <summary>
        /// This method is a copy of the OrderProcessing InsertItem Task
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private async Task InsertItemOnWorkflow(IOrder order)
        {
            if(order.ItemID != null || order.ItemID > 0)
                return;


            #region test
            OrderItem item = new OrderItem();
            //DocumentSource[] HighPriorityOrders = { DocumentSource.Web, DocumentSource.Validation, DocumentSource.Repetition };

            var projectRepo = factory.GetInstance<IProjectRepository>();
            var brandRepo = factory.GetInstance<IBrandRepository>();


            item.OrderGroupID = order.OrderGroupID;
            item.OrderNumber = order.OrderNumber;
            item.CompanyID = order.CompanyID;
            item.ProjectID = order.ProjectID;

            var company = companyRepo.GetByID(order.CompanyID);
            var project = projectRepo.GetByID(order.ProjectID);
            var brand = brandRepo.GetByID(project.BrandID);
            item.BrandID = project.BrandID;
            item.OrderID = order.ID;


            item.CompanyName = company.Name;
            item.ProjectName = project.Name;
            item.BrandName = brand.Name;

            item.SendOrderReceivedEmailCompleted = true;
            item.OrderExistVerifierCompleted = true;

            item.PrimaryCustomer = MiscHelper.Coalesce(project.CustomerSupport1, company.CustomerSupport1);
            item.SecondaryCustomer = MiscHelper.Coalesce(project.CustomerSupport2, company.CustomerSupport2);

            item.FileDropEventCount = 0;
            item.OrderWorkflowConfigID = project.OrderWorkflowConfigID;

            // busqueda, se pueden poner varios campos separados por comas
            item.Keywords = order.OrderNumber + "," + company.Name + "," + brand.Name + "," + project.Name;

            item.Priority = ItemPriority.High;


            #endregion test


            var wf = await apm.GetWorkflowAsync("Order Processing");// harcode workflow name
            var wfTask = wf.GetTask("OrderSetValidator"); // harcode task name, if the task name is change in future this code will be broken

            await wf.InsertItemAsync(item, wfTask.TaskID, ItemStatus.Delayed, true);


            order.ItemID = item.ItemID;

            await UpdateAsync(order);

            var i = await wf.WaitForItemStatus(item.ItemID,
                        ItemStatus.Completed | ItemStatus.Rejected | ItemStatus.Waiting,
                        TimeSpan.FromSeconds(120));



            //Debug.WriteLine(i.Name);

        }


        public OrderDetailDTO GetOrderArticle(int orderID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderArticle(ctx, orderID);
            }
        }

        public OrderDetailDTO GetOrderArticle(PrintDB ctx, int orderID)
        {
            var q = ctx.CompanyOrders
                .Join(ctx.PrinterJobs, o => o.ID, j => j.CompanyOrderID, (order, job) => new { Order = order, Job = job })
                .Join(ctx.Articles, j1 => j1.Job.ArticleID, a => a.ID, (j1, a) => new { j1.Order, Article = a })
                //.Join(ctx.Labels, j2 => j2.Article.LabelID, l => l.ID, (j2, l) => new { j2.Order, j2.Article, Label = l })
                .Where(w => w.Order.ID == orderID)
                .Select(s => new OrderDetailDTO
                {
                    OrderID = s.Order.ID,
                    ArticleID = s.Article.ID,
                    ArticleCode = s.Article.ArticleCode,
                    IsItem = s.Article.LabelID == null ? true : false,
                    LabelID = s.Article.LabelID,
                    //HasRFID = s.Article.LabelID == null ? false : s.Label.EncodeRFID,
                    HasRFID = false,
                    ProjectID = s.Order.ProjectID,
                    OrderDataID = s.Order.OrderDataID
                })
                .FirstOrDefault();


            if(!q.IsItem)
            {
                var lbl = ctx.Labels.Where(w => w.ID == q.LabelID).Single();
                q.HasRFID = lbl.EncodeRFID;
                q.IncludeComposition = lbl.IncludeComposition;
            }

            return q;
        }

    }

}
