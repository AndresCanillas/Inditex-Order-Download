using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers.Wizards
{
    public class ReviewController : Controller
    {
        private IFactory factory;
        private IOrderDocumentService docSrv;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private IOrderRepository orderRepo;
        private readonly IProviderRepository providerRepo;
        private readonly ITempFileService tempFileService;

        public ReviewController(
            IFactory factory,
            IOrderDocumentService docSrv,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IOrderRepository orderRepo,
            IProviderRepository providerRepo,
            ITempFileService tempFileService
            )
        {
            this.factory = factory;
            this.docSrv = docSrv;
            this.g = g;
            this.log = log;
            this.events = events;
            this.orderRepo = orderRepo;
            this.providerRepo = providerRepo;
            this.tempFileService = tempFileService;
        }


        [HttpGet, Route("/order/validate/getpreview/{orderNumber}")]
        public async Task GetPreviewDocument(string orderNumber)
        {
            try
            {
                var orderID = int.Parse(orderNumber.Split('-')[0]);
                var order = orderRepo.GetByID(orderID, true);
                var filter = new OrderReportFilter() { OrderID = orderID, OrderDate = order.CreatedDate.AddDays(-1), OrderDateTo = order.CreatedDate.AddDays(1) };
                var found = await orderRepo.GetOrderReportPage(filter, CancellationToken.None);
                var header = found.Where(w => w.IsGroup).FirstOrDefault();
                var detail = found.Where(w => w.OrderID.Equals(orderID)).FirstOrDefault();
                var file = docSrv.GetPreviewDocument(orderID, true);

                var fileName = System.Net.WebUtility.UrlEncode(tempFileService.SanitizeFileName($"PV_{header.ProviderClientReference}_{header.OrderNumber}_{detail.ArticleCode}_{orderID}.pdf"));

                Response.Headers.Add("Content-Type", "application/pdf");
                Response.Headers.Add("content-disposition", $"filename={fileName}");


                using(var fs = file.GetContentAsStream())
                {
                    fs.CopyTo(Response.Body, 4096);
                }

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                Response.StatusCode = 404;
            }
        }

        [HttpGet, Route("/order/validate/getprodsheet/{orderNumber}")]
        public void GetProductionSheetDocument(string orderNumber)
        {
            try
            {
                var orderId = int.Parse(orderNumber.Split('-')[0]);
                var file = docSrv.GetProdSheetDocument(orderId);
                Response.Headers.Add("Content-Type", "application/pdf");
                using(var fs = file.GetContentAsStream())
                {
                    fs.CopyTo(Response.Body, 4096);
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                Response.StatusCode = 404;
            }
        }

        [HttpGet, Route("/order/validate/getorderdetail/{orderNumber}")]
        public void GetOrderDetailDocument(string orderNumber)
        {
            try
            {
                var orderId = int.Parse(orderNumber);
                _ = docSrv.CreateOrderDetailDocument(orderId).GetAwaiter().GetResult();
                var file = docSrv.GetOrderDetailDocument(orderId);
                Response.Headers.Add("Content-Type", "application/pdf");
                using(var fs = file.GetContentAsStream())
                {
                    fs.CopyTo(Response.Body, 4096);
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                Response.StatusCode = 404;
            }
        }

        [HttpPost, Route("/order/validate/createorderdetail/{orderNumber}")]
        public void CreateOrderDetailDocument(string orderNumber)
        {
            try
            {
                var orderId = int.Parse(orderNumber);
                _ = docSrv.CreateOrderDetailDocument(orderId).GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                Response.StatusCode = 404;
            }
        }


        [HttpPost, Route("/order/validate/solvereview")]
        public OperationResult Solve([FromBody] List<OrderGroupSelectionDTO> selection)
        {

            try
            {

                _SaveState(selection);
                // TODO: wizard was updated when status changed to Validated
                _UpdateWizardProgress(selection);

                _RegisterOrderLogs(selection);

                return new OperationResult(true, g["Orders were marked as Validated"]);

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }


        [HttpPost, Route("/order/validate/getordersdetailsforreview")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                //var repo = factory.GetInstance<IOrderRepository>();
                var cfgRepository = factory.GetInstance<IConfigurationRepository>();

                var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Label, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow };
                var result = orderRepo.GetArticleDetailSelection(selection, filter);

                #region Adding ExtraItems to the review images
                var cloneSelection = new List<OrderGroupSelectionDTO>();

                selection.ForEach(e =>
                {
                    var itemSel = new OrderGroupSelectionDTO(e);
                    itemSel.Details = new List<OrderDetailDTO>();
                    itemSel.Orders = new int[0];
                    cloneSelection.Add(itemSel);
                });


                var filterItems = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Item, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow };
                var itemsPendingApprove = orderRepo.GetItemsExtrasDetailSelection(selection, filterItems); // this query only looking by OrderGroupID


                var itemsDetails = new List<OrderDetailDTO>();

                foreach(var o in itemsPendingApprove)
                {

                    var uniqueItemDetails = new List<OrderDetailDTO>();

                    var sel = result.Where(x => x.OrderGroupID == o.OrderGroupID).First();

                    itemsDetails.AddRange(o.Details);

                    var groups = itemsDetails.GroupBy(g => g.ArticleCode).ToList();

                    groups.ForEach(e => uniqueItemDetails.Add(e.First()));

                    sel.Details.AddRange(uniqueItemDetails);

                }
                #endregion Adding ExtraItems to the review images


                foreach(var sel in result)
                {

                    if(sel.Orders.Length > 0)
                    {
                        var orderId = sel.Orders[0];
                        var order = orderRepo.GetByID(orderId);
                        if(order.ProductionType == ProductionType.IDTLocation)
                        {
                            var provider = providerRepo.GetByID(order.ProviderRecordID.Value);
                            sel.DueDate = cfgRepository.GetOrderDueDate(order.CompanyID, provider.ClientReference, order.ProjectID);
                        }

                        var groups = sel.Details.GroupBy(g => new { g.ArticleCode, g.OrderID }).ToList();

                        sel.Details = new List<OrderDetailDTO>();

                        groups.ForEach(e => sel.Details.Add(e.First()));

                        sel.Orders = sel.Details.Select(d => d.OrderID).Distinct().ToArray();
                    }

                }

                return new OperationResult(true, g["Articles Found"], result);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }



        [HttpGet, Route("/orders/validate/getpreviewdocumentzip/{orders}")]
        public IActionResult GetPreviewDocumentZip(string orders)
        {
            try
            {
                var ids = orders.Split('.').Select((i) => Convert.ToInt32(i));
                Response.Headers.Add("Content-Disposition", "attachment");

                var zipfilepath = orderRepo.PackMultipleOrdersValidationPreview(ids);
                return File(System.IO.File.OpenRead(zipfilepath), MimeTypes.GetMimeType(Path.GetExtension(zipfilepath)), Path.GetFileName(zipfilepath));

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }


        private void _SaveState(List<OrderGroupSelectionDTO> selection)
        {

            // sent validated event for stock items
            //var orderRepo = sp.GetRequiredService<IOrderRepository>();
            var repo = factory.GetInstance<IOrderRepository>();

            // proyect configuration
            var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Item, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow };
            var result = repo.GetItemsExtrasDetailSelection(selection, filter);

            //result.ForEach(r => r.Details = r.Details.Where(w => w.QuantityRequested < 1).ToList());
            foreach(var grp in result)
            {
                foreach(var article in grp.Details)
                {
                    if(article.IsBilled) { continue; }

                    repo.ChangeStatus(article.OrderID, OrderStatus.Validated);

                    var orderInfo = repo.GetProjectInfo(article.OrderID);

                    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                }
            }


            foreach(var sel in selection)
            {
                sel.Orders.ToList().ForEach(e =>
                {
                    repo.ChangeStatus(e, OrderStatus.Validated);

                    var orderInfo = repo.GetProjectInfo(e);

                    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                });
            }



        }

        private void _UpdateWizardProgress(List<OrderGroupSelectionDTO> selection)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            //wzdRepo.MarkAsComplete(rq.WizardStepID);

            //repo.UpdateProgress(rq.WizardID);

            foreach(var gp in selection)
            {

                var selectedOrders = gp.Orders.Select(s => s);

                if(selectedOrders.Count() < 1)
                {
                    // XXX: 20241108 - TEMP LOG TO IDENTIFY ISSUE
                    log.LogWarning("Cannot complete wizard, selectedOrders are empty");
                    log.LogWarning(Newtonsoft.Json.JsonConvert.SerializeObject(gp));
                }
                else
                    wzStpRepo.MarkAsCompleteByGroup(gp.WizardStepPosition, selectedOrders);


                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach(var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);

                    events.Send(new QuantitiesStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupSelectionDTO> selection)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();


            //orderLog.InfoAsync(rq.OrderID, g["Quantities Validation Completed"]);
            foreach(var gp in selection)
            {
                var selectedOrders = gp.Orders.Select(s => s);

                foreach(var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Order Item Validation Completed"]);

                }
            }
        }
    }
}