using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Middleware;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;
using WebLink.Services;

namespace WebLink.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private IFactory factory;
        private IOrderRepository repo;
        private IUserData userData;
        private IEventQueue events;
        private ILocalizationService g;
        private ILogService log;
        private readonly IAppConfig config;
        private IOrderRegisterInERP orderRegisterInERP;
        private IExtractSizeRangeService extractSizeRangeService;
        private IOrderGroupRepository orderGroupRepository;
        private IProjectRepository  projectRepository; 
        private IUserRepository userRepository;
        private IOrderEmailService  orderEmailService;


        // cache
        private IMemoryCache memoryCache;
        private static HashSet<string> _refreshingKeys = new HashSet<string>();


        public OrdersController(
            IFactory factory,
            IOrderRepository repo,
            IUserData userData,
            IEventQueue events,
            ILocalizationService g,
            ILogService log,
            IAppConfig config,
            IOrderRegisterInERP orderRegisterInERP
,
            IExtractSizeRangeService extractSizeRangeService,
            IMemoryCache memoryCache,
            IOrderGroupRepository orderGroupRepository
,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            IOrderEmailService orderEmailService)
        {
            this.factory = factory;
            this.repo = repo;
            this.userData = userData;
            this.events = events;
            this.g = g;
            this.log = log;
            this.config = config;
            this.orderRegisterInERP = orderRegisterInERP;
            this.extractSizeRangeService = extractSizeRangeService;
            this.memoryCache = memoryCache;
            this.orderGroupRepository = orderGroupRepository;
            this.projectRepository = projectRepository;
            this.userRepository = userRepository;
            this.orderEmailService = orderEmailService;
        }

        [HttpPost, Route("/orders/insert")]
        public OperationResult Insert([FromBody] Order data)
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadOrder)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Order Created!"], repo.Insert(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/orders/updateduedate")]
        public OperationResult UpdateDueDate([FromBody] OrderDueDateDTO data)
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadOrder)
                    return OperationResult.Forbid;

                return new OperationResult(true, g["Order saved!"], repo.ChangeDueDate(data, orderRegisterInERP));
                //return new OperationResult(true, g["Order saved!"], "Ok");

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/orders/update")]
        public OperationResult Update([FromBody] Order data)
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadOrder)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Order saved!"], repo.Update(data));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/orders/delete/{id}")]
        public OperationResult Delete(int id)
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadOrder)
                    return OperationResult.Forbid;
                repo.Delete(id);
                return new OperationResult(true, g["Order Deleted!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/orders/getbyid/{id}")]
        public IOrder GetByID(int id)
        {
            try
            {
                return repo.GetByID(id);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/orders/getlist")]
        public List<IOrder> GetList()
        {
            try
            {
                return repo.GetList();
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/orders/getlistbystatus/{status}")]
        public List<IOrder> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                return repo.GetOrdersByStatus(status);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [Obsolete("No used, replaced by GetOrderGroupReport")]
        [HttpPost, Route("/orders/getreport")]
        public OperationResult GetReport([FromBody] OrderReportFilter filter)
        {
            List<CompanyOrderDTO> orders;
            string cacheKey = filter.ToString();

            try
            {

                if(memoryCache.TryGetValue(cacheKey, out orders))
                {
                    orders = repo.GetOrderReport(filter);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(10))
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

                    memoryCache.Set(cacheKey, orders, cacheEntryOptions);
                }


                return new OperationResult(true, null, orders);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpPost, Route("/groups/orders/getreport")]
        public async Task<OperationResult> GetOrderGroupReport([FromBody] OrderReportFilter filter, CancellationToken cancellationToken)
        {
            IEnumerable<CompanyOrderDTO> orders = new List<CompanyOrderDTO>();
            bool success = true;

            try
            {
                //await Task.Delay(10000);

                orders = await repo.GetOrderReportPage(filter, cancellationToken);

            }
            catch(TaskCanceledException ce)
            {
                orders = new List<CompanyOrderDTO>();
            }
            catch(Exception ex)
            {
                orders = new List<CompanyOrderDTO>();
                log.LogException(ex);
                success = false;
            }


            return await Task.FromResult(new OperationResult(success, null, new { Records = orders, Filter = filter }));

        }

        //[HttpPost, Route("/groups/orders/getreportCACHE")]
        //public OperationResult GetOrderGroupReportCACHE([FromBody] OrderReportFilter filter)
        //{


        //    IEnumerable<CompanyOrderDTO> orders = new List<CompanyOrderDTO>();
        //    string cacheKey = Newtonsoft.Json.JsonConvert.SerializeObject(filter);
        //    string cacheKeyFilter = cacheKey + "f";
        //    bool enableCache = this.config.GetValue("Cache.Orders.List", false);
        //    //var hasCode = filter.GetHashCode();

        //    if(enableCache && memoryCache.TryGetValue(cacheKey, out orders) && memoryCache.TryGetValue(cacheKeyFilter, out var cacheFilter))
        //        return new OperationResult(true, null, new { Records = orders, Filter = cacheFilter });


        //    try
        //    {
        //        orders = repo.GetOrderReportPage(filter);
        //        return new OperationResult(true, null, new { Records = orders, Filter = filter });
        //    }
        //    catch(Exception ex)
        //    {
        //        log.LogException(ex);
        //        return new OperationResult(false, g["Operation could not be completed."], null);
        //    }
        //    finally
        //    {
        //        // store in cache result
        //        if(enableCache && !_refreshingKeys.Contains(cacheKey))
        //        {
        //            lock(_refreshingKeys)
        //            {
        //                // double check is required
        //                if(!_refreshingKeys.Contains(cacheKey))
        //                {
        //                    _refreshingKeys.Add(cacheKey);

        //                    try
        //                    {

        //                        var cacheEntryOptions = new MemoryCacheEntryOptions()
        //                            .SetSlidingExpiration(TimeSpan.FromSeconds(3))
        //                            .SetAbsoluteExpiration(TimeSpan.FromSeconds(6));

        //                        memoryCache.Set(cacheKey, orders, cacheEntryOptions);
        //                        memoryCache.Set(cacheKeyFilter, filter, cacheEntryOptions);

        //                    }
        //                    finally
        //                    {
        //                        _refreshingKeys.Remove(cacheKey);
        //                    }


        //                }
        //            }
        //        }
        //    }

        //}


        [Route("/orders/getorderfile/{id}")]
        public IActionResult GetOrderFile(int id)
        {
            Response.Headers.Add("Content-Disposition", "attachment");
            try
            {
                IFileData file = repo.GetOrderFile(id);
                if(file != null)
                    return File(file.GetContentAsStream(), MimeTypes.GetMimeType(Path.GetExtension(file.FileName)), file.FileName);
                else
                    return NotFound();
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }


        [Route("/orders/getpdfpreview/{orderid}")]
        public IActionResult GetPDFPreview(int orderid)
        {
            try
            {
                var order = repo.GetByID(orderid);
                string fileName = $"OrderPreview_{order.ID}.pdf";
                var header = (new ContentDispositionHeaderValue("attachment") { FileName = fileName }).ToString();
                Response.Headers.Add("Content-Disposition", header);
                IAttachmentData file = repo.GetOrderAttachment(orderid, "Documents", fileName);
                if(file != null)
                    return File(file.GetContentAsStream(), MimeTypes.GetMimeType(Path.GetExtension(file.FileName)), fileName);
                else
                    return NotFound();
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }



        [HttpGet, Route("/orders/getprintpackage/{orderid}")]
        public async Task<IActionResult> GetPrintPackage(int orderid)
        {
            Response.Headers.Add("Content-Disposition", "attachment");
            try
            {
                var file = repo.GetOrderAttachment(orderid, "PrintPackage", "Order-" + orderid + ".zip") as IRemoteAttachment;
                if(file != null)
                {
                    log.LogMessage($"Returning print package: {file.FileSize} bytes");
                    var stream = await file.GetContentAsStreamAsync();
                    log.LogMessage($"Returning print package Got Source Stream, returning back to user-agent");
                    return File(stream, MimeTypes.GetMimeType(Path.GetExtension(file.FileName)), file.FileName);
                }
                else
                {
                    return NotFound();
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }


        [HttpGet, Route("/orders/getprintpackages/{factoryid}/{orders}")]
        public IActionResult GetPrintPackages(int factoryid, string orders)
        {
            var ids = orders.Split('.').Select((i) => Convert.ToInt32(i));
            Response.Headers.Add("Content-Disposition", "attachment");
            try
            {
                string fileName = repo.PackMultiplePrintPackages(factoryid, ids);
                return File(System.IO.File.OpenRead(fileName), MimeTypes.GetMimeType(Path.GetExtension(fileName)), Path.GetFileName(fileName));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }


        [HttpGet, Route("/orders/getarticledetails/{orderid}")]
        public OperationResult GetArticleDetails(int orderID)
        {

            try
            {
                return new OperationResult(true, null, repo.GetOrderArticles(new OrderArticlesFilter() { OrderID = new List<int>() { orderID } }, ProductDetails.None));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Cannot get order articles"], null);
            }
        }

        [HttpGet, Route("/orders/getdistinctsizes/{orderid}/{projectid}")]
        public OperationResult GetDistinctSizes(int orderID, int projectid)
        {
            try
            {
                List<string> sizes = new List<string>();
                var result = repo.GetOrderArticles(new OrderArticlesFilter() { OrderID = new List<int>() { orderID } }, ProductDetails.Custom, new List<string> { "Size" });
                var Filesizes = this.extractSizeRangeService.ExtractOrderSizesListByLines(result, projectid);

                //var sizes = result.ToList().Select(s => s.ProductData.GetValue("Size", "-"));

                sizes = this.extractSizeRangeService.ExtractOrderSizesListByUseInSizes(result, projectid);
                return new OperationResult(true, g["Order sizes recovered"], data: new { UseInSize = sizes, FileSizes = Filesizes });

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Cannot get order articles"], null);
            }

        }

        //private List<string> ExtractOrderSizesList(IEnumerable<OrderDetailDTO> detail, int projectId)
        //{
        //    var listOfSizes = new List<string>();

        //    using (var dynamicDB = connManager.CreateDynamicDB())
        //    {
        //        var detailCatalog = (from c in ctx.Catalogs where c.ProjectID == projectId && c.Name == Catalog.ORDERDETAILS_CATALOG select c).FirstOrDefault();
        //        var productCatalog = (from c in ctx.Catalogs where c.ProjectID == projectId && c.Name == Catalog.VARIABLEDATA_CATALOG select c).FirstOrDefault();
        //        var productDataList = detail.Select(s => s.ProductDataID);

        //        var productDetails = dynamicDB.Select(detailCatalog.CatalogID, $@"
        //                                    SELECT Details.ID AS ProductDataID ,Product.Barcode,Product.TXT1,Product.Size,Product.Color
        //                                    FROM #TABLE Details
        //                                    INNER JOIN {productCatalog.Name}_{productCatalog.CatalogID} Product ON Details.Product = Product.ID
        //                                    WHERE Details.ID in ({string.Join(",", productDataList.ToArray())})
        //                                    ");

        //        foreach (var d in detail)
        //        {
        //            var product = productDetails.Where(w => ((JObject)w).GetValue<int>("ProductDataID").Equals(d.ProductDataID)).First();
        //            var size = ((JObject)product).GetValue<string>("Size");
        //            if (!string.IsNullOrEmpty(size) && !listOfSizes.Any(s => s == size))
        //            {
        //                listOfSizes.Add(size);
        //            }
        //        }
        //    }
        //    return listOfSizes;
        //}



        [HttpPost, Route("/orders/getarticledetails")]
        public OperationResult GetArticleDetails([FromBody] OrderArticlesFilter filter)
        {
            try
            {
                var orders = repo.GetOrderArticles(filter, ProductDetails.None);

                var data = orders.GroupBy(a => new { a.PackCode, a.ArticleCode, a.GroupingField }).Select(x => new OrderDetailDTO
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
                    MaxAllowed = CalculateMaxAllowed(x.Sum(f => f.QuantityRequested), (OrderQuantityEditionOption)x.Max(f => f.AllowQuantityEdition), x.Max(f => f.MaxQuantityPercentage), x.Max(f => f.MaxQuantity)),
                    MinAllowed = x.Max(f => f.MinAllowed),
                    HasPackCode = x.Max(f => f.HasPackCode),
                    SyncWithSage = x.Max(f => f.SyncWithSage),
                    SageReference = x.Max(f => f.SageReference),
                    UnitDetails = x.Max(f => f.UnitDetails),
                    PackConfigQuantity = x.Max(f => f.PackConfigQuantity),
                    Size = x.Max(f => f.Size),
                    Color = x.Max(f => f.Color)

                }).ToList();

                return new OperationResult(true, null, data);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Cannot get order details"], null);
            }
        }

        [HttpPost, Route("/orders/getbyproject")]
        [AuthorizeRoles(Roles.SysAdmin, Roles.IDTLabelDesign, Roles.IDTCostumerService)]
        public OperationResult GetOrdersByProject([FromBody] OrderByLabelFilter filter)
        {
            // Used while updating the label preview (to select an order and a detail within that order)
            try
            {
                var data = repo.GetOrdersByLabelID(filter.ProjectID, filter.LabelId, filter.Count, filter.OrderNumber);

                return new OperationResult(true, null, data);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpGet, Route("/orders/simpleprintdetailsdata/{orderID}/{labelID}")]
        public OperationResult GetOrderDetailsByLabel(int orderID, int labelID)
        {
            try
            {
                var details = repo.GetDetailsByLabel(orderID, labelID);
                return new OperationResult() { Data = details, Success = true };
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpGet, Route("/orders/compare/{id}/{prevOrderId}")]
        public OperationResult Compare(int id, int prevOrderId)
        {
            try
            {
                return new OperationResult(true, g["Order Compared!"], repo.Compare(id, prevOrderId, false, 0));
            }
            catch(Exception ex)
            {
                log.LogException("OrdersController::Compare", ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/orders/pendingtosync")]
        public IEnumerable<CompanyOrderDTO> GetPendingToSync([FromBody] PendingOrdersRq rq)
        {
            try
            {
                var pending = repo.GetPendingOrdersForFactory(rq.Orders, rq.FactoryID, rq.DeltaTime);

                if(rq.ExecuteSync)
                {
                    repo.SyncOrderWithFactory(pending);
                    log.LogMessage("Orders Will be sync soon [{0}] with factory [{1}]", pending.Count(), rq.FactoryID);
                }

                return pending;
            }
            catch(Exception ex)
            {
                log.LogException("OrdersController::GetPendingToSync", ex);
                return new List<CompanyOrderDTO>();
            }
        }

        private int CalculateMaxAllowed(int quantityRequested, OrderQuantityEditionOption isAllowQuantityEdition, int? maxPercent, int? maxFixed)
        {
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

        [HttpGet, Route("/orders/getencodedbyorder/{id}")]
        public ActionResult<List<EncodedEntity>> GetEncodedByOrder(int id)
        {
            return repo.GetEncodedByOrder(id);

        }

        [HttpPost, Route("/orders/customreport")]
        public OperationResult GetCustomReport([FromBody] OrderReportFilter filter)
        {
            try
            {
                var data = repo.GetOrderCustomReportPage(filter).ToList();

                return new OperationResult(true, null, new { Records = data, Filter = filter });
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpPost, Route("/orders/attachordergroupdocument")]
        public OperationResult AttachOrderGroupDocument([FromForm] OrderAttachDocumentRequest attachRequest)
        {
            try
            {
                if(Request.Form.Files == null || Request.Form.Files.Count != 1)
                    return new OperationResult(false, $"Invalid Request. Was expecting a single file.", null);

                // 90 numero de dias entre la fecha actual para encontrar Order Groups
                var orderGroups = orderGroupRepository.GetGroupByOrderNumberList(attachRequest.OrderNumber, attachRequest.ProjectID, 90).ToList();
                /*var orderGroup = orderGroupRepository.GetGroupFor(
                     new OrderGroup()
                     {
                         ProjectID = attachRequest.ProjectID,
                         OrderNumber = attachRequest.OrderNumber
                     });*/

                if(orderGroups.Count() == 0)
                    return new OperationResult(false, $"The order {attachRequest.OrderNumber} for project {attachRequest.ProjectID} could not be found, therefore the PDF could not be assigned to the order.", null);

                foreach(var orderGroup in orderGroups)
                {
                    orderGroupRepository.SetOrderGroupAttachment(
                    orderGroup.ID,
                    "SupportFiles",
                    Request.Form.Files[0].FileName,
                    Request.Form.Files[0].OpenReadStream());
                }


                return new OperationResult(true, null, null);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, null);
            }
        }

        [HttpGet, Route("/orders/getorderpdf/{orderGroupid}/{ordernumber}")]
        public OperationResult GetOrderPdf(int orderGroupId, string orderNumber)
        {
            Response.Headers.Add("Content-Disposition", "attachment");
            try
            {
                var orderpdfResult = orderGroupRepository.GetOrderPdf(orderGroupId, orderNumber, "SupportFiles");

                if(orderpdfResult != null)
                {
                    using(var ms = new MemoryStream())
                    {
                        orderpdfResult.Content.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        var base64Pdf = Convert.ToBase64String(fileBytes);

                        return new OperationResult(
                            success: true,
                            message: "PDF generated correctly",
                            data: new
                            {
                                FileName = orderpdfResult.Filename,
                                MimeType = MimeTypes.GetMimeType(Path.GetExtension(orderpdfResult.Filename)),
                                ContentBase64 = base64Pdf
                            }
                        );
                    }
                }
                else
                {
                    return new OperationResult(false, "PDF not found", null);
                }
                /*if(orderpdfResult != null)
                    return File(orderpdfResult.Content, MimeTypes.GetMimeType(Path.GetExtension(orderpdfResult.Filename)));
                else
                    return NotFound();*/
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, null);
            }
        }

        [HttpPost, Route("/orders/orderrecievedmailnotification/{projectid}")]
        public async Task<OperationResult> OrderRecievedMailNotification([FromBody] CustomersOrderRecievedMailNotification mailNotification, int projectID)
        {
            try
            {
                var emails = new List<string>();
                //              var customers = notificationRepository.GetIDTStakeholders(projectID,null); 
                var customers = projectRepository.GetCustomerEmails(projectID);


                foreach(var customer in customers)
                {
                    var customerRepository = userRepository.GetByID(customer);
                    if(customerRepository == null)
                    {
                        log.LogException($"This customer ID:{customer} not exists!");
                        return new OperationResult(false, g["Operation could not be completed."]);
                    }
                    emails.Add(customerRepository?.Email);


                    await orderEmailService.SendMessage(string.Join(';', emails), mailNotification.Subject, mailNotification.Body, null);
                }

                return new OperationResult(true, g["Operation OK! "]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]); ;
            }
        }

        [HttpGet, Route("/orders/getcountrybyorderlocation/{orderGroupid}")]
        public OperationResult GetCountryByOrderLocation(int orderGroupID)
        {
            try
            {
                var data = repo.GetCountryByOrderLocation(orderGroupID);

                return new OperationResult(true, null, new { Data = data });
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, null);

            }
            
        }
    }
}