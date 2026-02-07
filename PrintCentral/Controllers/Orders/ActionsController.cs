using LinqKit;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;

namespace PrintCentral.Controllers
{
    public class ActionsController : Controller
    {
        private IFactory factory;
        private IOrderRepository repo;
        private IProjectRepository projectRepo;
        private IOrderActionsService actionsService;
        private IUserData userData;
        private ILocalizationService g;
        private IEventQueue events;
        private ILogService log;
        private IOrderLogService orderLog;
        private ISageClientService sageClient;
        private IOrderRepository orderRepo;
        private IOrderNotificationManager notificationMng;
        private readonly IOrderGroupRepository orderGroupRepo;
        private readonly ITempFileService tempFileService;

        public ActionsController(
            IFactory factory,
            IOrderRepository repo,
            IProjectRepository projectRepo,
            IOrderActionsService actionsService,
            IUserData userData,
            ILocalizationService g,
            IEventQueue events,
            ILogService log,
            IOrderLogService orderLog,
            ISageClientService sageClient,
            IOrderRepository orderRepo,
            IOrderNotificationManager notificationMng,
            IOrderGroupRepository orderGroupRepo,
            ITempFileService tempFileService
            )
        {
            this.factory = factory;
            this.repo = repo;
            this.projectRepo = projectRepo;
            this.actionsService = actionsService;
            this.userData = userData;
            this.g = g;
            this.events = events;
            this.log = log;
            this.orderLog = orderLog;
            this.sageClient = sageClient;
            this.orderRepo = orderRepo;
            this.notificationMng = notificationMng;
            this.orderGroupRepo = orderGroupRepo;
            this.tempFileService = tempFileService;
        }


        [HttpPost, Route("/orders/actions/changestatus")]
        public OperationResult ChangeStatus([FromBody] ActionRequest data)
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadOrder)
                    return OperationResult.Forbid;


                var order = orderRepo.GetByID(data.OrderID);

                #region CHECK if order exist in ERP
                if(data.OrderStatus == OrderStatus.Cancelled)
                {

                    bool existInERP = false;

                    if(order.SyncWithSage && !string.IsNullOrEmpty(order.SageReference))
                    {

                        existInERP = sageClient.CheckIfOrderExistAsync(order.SageReference, order.ID).Result;
                    }

                    if(existInERP)
                    {
                        // TODO: notify CUSTOMER
                        //orderLog.Debug(data.OrderID, g["Cannot Cancel Order, Remove Order from ERP first"]);
                        //return new OperationResult() { Success = false, Message = "Remove Order From ERP before cancel it" };
                    }
                }
                #endregion CHECK if order exist in ERP

                IOrder responseData;

                if(order.HasOrderWorkflow)
                {
                    responseData = repo.ChangeStatusWF(data.OrderID, data.OrderStatus, data.ResetEvent);
                }
                else
                {
                    responseData = repo.ChangeStatus(data.OrderID, data.OrderStatus);
                }

                if(responseData.OrderStatus == OrderStatus.Cancelled)
                {
                    actionsService.StopOrder(data.OrderID);
                    orderLog.Info(data.OrderID, "Order was cancelled.", data.Comments);
                }

                if(data.ResetEvent)
                {
                    repo.ResetStatusEvent(data.OrderID);
                }

                return new OperationResult(true, g["Order was updated!"], responseData);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }

        }

        [HttpPost, Route("/orders/actions/changestatusbygroup")]
        public OperationResult ChangeStatusByGroup([FromBody] UpdateStatusRequest rq)
        {

            var validatedStatus = new List<OrderStatus> {
                OrderStatus.Validated,
                OrderStatus.Billed,
                OrderStatus.ProdReady,
                OrderStatus.Printing,
                OrderStatus.Completed

            };

            var messages = new List<string>();

            try
            {
                if(!userData.Can_Orders_Delete) return OperationResult.Forbid;



                rq.Selection.ForEach(sel =>
                {
                    bool existInERP = false;
                    bool validated = false;

                    sel.Orders.ToList().ForEach(orderID =>
                    {
                        var order = orderRepo.GetByID(orderID);

                        validated = validatedStatus.Contains(order.OrderStatus);

                        #region CHECK if order exist in ERP
                        if(rq.OrderStatus == OrderStatus.Cancelled)
                        {

                            if(order.SyncWithSage && !string.IsNullOrEmpty(order.SageReference))
                            {
                                existInERP = sageClient.CheckIfOrderExistAsync(order.SageReference, order.ID).Result;
                            }

                            if(userData.IsIDT && existInERP)
                            {
                                messages.Add(g[$"Cannot Cancel Order [{orderID}], Remove Order from ERP first"]);
                                // TODO: notify CUSTOMER
                                orderLog.Debug(orderID, messages.Last());
                                //return new OperationResult() { Success = false, Message = "Remove Order From ERP before cancel it" };
                            }

                            if(!userData.IsIDT && validated)
                            {
                                messages.Add(g[$"Cannot Cancel Order [{orderID}], this order is already validated. Please, Contact the Customer Service"]);
                                orderLog.Debug(orderID, messages.Last());
                            }

                            if(existInERP || (!userData.IsIDT && validated))
                            {
                                // next
                                return; // continue de foreach, this order can be cancelled
                            }
                        }
                        #endregion CHECK if order exist in ERP

                        var responseData = repo.ChangeStatus(orderID, rq.OrderStatus);

                        if(order.HasOrderWorkflow)
                            repo.SetWorkflowTask(orderID, rq.OrderStatus, rq.ResetEvent);

                        if(responseData.OrderStatus == OrderStatus.Cancelled)
                        {
                            // register the log for cacelled orders and notify the factory
                            actionsService.StopOrder(orderID);
                            orderLog.Info(orderID, "Order was cancelled.", rq.Comments);

                        }

                        if(rq.ResetEvent)
                        {
                            repo.ResetStatusEvent(orderID);
                        }

                    });
                });

                return new OperationResult() { Success = true, Data = messages, Message = messages.Count < 1 ? g["Action executed"] : g["Some Orders can't be updated"] };
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/orders/actions/stop/{orderId}")]
        public OperationResult StopOrder(int orderId)
        {
            try
            {
                actionsService.StopOrder(orderId);
                return new OperationResult(true, g["Order was stopped"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, cannot stop selected order"]);
            }
        }

        [HttpPost, Route("/orders/actions/active/{orderId}")]
        public OperationResult ActiveOrder(int orderId)
        {
            try
            {
                actionsService.ActiveOrderWithDuplicatedEPC(orderId);
                return new OperationResult(true, g["Order was active"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, cannot active selected order"]);
            }
        }

        [HttpPost, Route("/orders/actions/duplicatedepc/{orderId}")]
        public OperationResult OrderWithDuplicatedEPC(int orderId)
        {
            try
            {
                actionsService.OrderWithDuplicatedEPC(orderId);
                return new OperationResult(true, g["Order has duplicated epc data"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, there was a problem trying to update this Order"]);
            }
        }

        [Obsolete("replaced by continuebygroup")]
        [HttpPost, Route("/orders/actions/resume")]
        public OperationResult ResumeOrder([FromBody] ActionRequest rq)
        {
            try
            {
                actionsService.ResumeOrder(rq.OrderID);
                return new OperationResult(true, g["Order has resumed"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, order(s) remain stopped"]);
            }
        }

        [HttpPost, Route("/orders/actions/stopbygroup")]
        public OperationResult StopByGroup([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                selection.ForEach(StopOrders);
                return new OperationResult(true, g["Selected order(s) were stopped"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, cannot stop selected order(s)"]);
            }
        }

        [HttpPost, Route("/orders/actions/continuebygroup")]
        public OperationResult ContinueByGroup([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                foreach(var order in selection)
                {
                    if(!CheckCanContinue(order))
                    {
                        return new OperationResult(false, g["Cannot continue selected order(s) user not allowed"]);
                    }
                }

                selection.ForEach(ResumeOrders);
                return new OperationResult(true, g["Selected order(s) are running now"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, cannot continue selected order(s)"]);
            }
        }

        [HttpPost, Route("/orders/actions/movebygroup")]
        public OperationResult MoveByGroup([FromBody] UpdateLocationRequest selection)
        {
            try
            {
                UpdateOrdersLocation(selection);
                return new OperationResult(true, g["Selected order(s) were moved"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, cannot move selected order(s)"]);
            }
        }

        /// <summary>
        /// Return Next validation step for selected order
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        [Obsolete("replaced by GetNextStepBySelection")]
        [HttpGet, Route("/orders/actions/getnextstep/{orderid}")]
        public OperationResult GetNextStep(int orderID)
        {



            try
            {
                var steps = repo.GetNextStep(orderID);

                // translate


                return new OperationResult(true, null, steps);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected error: Cannot get next step"]);
            }
        }

        /// <summary>
        /// Return Next validation step for multiple selection
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        [HttpPost, Route("/orders/actions/getnextstep")]
        public OperationResult GetNextStepBySelection([FromBody] OrderGroupSelectionDTO[] selection)
        {
            /* TEST translate wizards titles */

            _ = g["Add Articles"];
            _ = g["Confirm Order"];
            _ = g["Custom Quantities"];
            _ = g["Custom Quantity Wizard for Mango, all seasons"];
            _ = g["Define Composition"];
            _ = g["Delivery Address"];
            _ = g["Item Assignment"];
            _ = g["Labels Fixed"];
            _ = g["Packs & Extras"];
            _ = g["Quantities"];
            _ = g["Review"];
            _ = g["Set Articles"];
            _ = g["Stickers & Composition"];
            _ = g["Support Files"];

            _ = g["Add Articles"];
            _ = g["Confirm Order"];
            _ = g["Define Composition"];
            _ = g["Delivery Address"];
            _ = g["Item Assignment"];
            _ = g["Labels Fixed"];
            _ = g["Order Data"];
            _ = g["Packs & Extras"];
            _ = g["Quantities"];
            _ = g["Review"];
            _ = g["Stickers & Composition"];
            _ = g["Support Files"];


            try
            {
                //var nextStep = repo.GetNextStepBySelection(selection.ToList());
                var allSteps = repo.GetAllWizardSteps(selection.ToList()).OrderBy(o => o.Position);

                allSteps.ForEach(s => s.Name = g[s.Name]); // translate steps

                var nextStep = allSteps.Where(w => !w.IsCompleted).FirstOrDefault();

                return new OperationResult(true, null, new { NextStep = nextStep, AllSteps = allSteps });
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected error: Cannot get next step"]);
            }
        }

        /// <summary>
        /// Return selected step in selected order
        /// </summary>
        /// <param name="orderID"></param>
        /// <returns></returns>
        [HttpPost, Route("/orders/actions/getstep")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult GetStepBySelection([FromForm] ActionRequestGetStepBySelection rq)
        {
            try
            {
                var wzdRepo = factory.GetInstance<IWizardRepository>();
                var step = repo.GetStepBySelection(rq.Position, rq.Selection);

                var allSteps = new List<IWizardStep>();

                if(step != null)
                {
                    allSteps = wzdRepo.GetSteps(step.WizardID).ToList();
                }

                return new OperationResult(true, null, new { Step = step, AllSteps = allSteps });
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected Error: Cannot get step"]);
            }
        }

        [HttpPost, Route("/orders/actions/updatedocuments")]
        public async Task<OperationResult> UpdateDocuments([FromBody] ActionRequest rq)
        {
            try
            {
                IOrderDocumentService docSrv = factory.GetInstance<IOrderDocumentService>();
                await docSrv.InvalidateCache(rq.OrderID);
                _ = await docSrv.CreatePreviewDocument(rq.OrderID);
                _ = await docSrv.CreateProdSheetDocument(rq.OrderID);
                _ = await docSrv.CreateOrderDetailDocument(rq.OrderID);
                return new OperationResult(true, g["Documents regenerated!"]);

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        [HttpGet, Route("orders/actions/getlog/{OrderID}")]
        public OperationResult GetLog(int OrderID)
        {
            try
            {

                var logRepo = factory.GetInstance<IOrderLogRepository>();

                OrderLogLevel level = OrderLogLevel.WARN;

                if(userData.IsIDTAdminRoles)
                {
                    level = OrderLogLevel.DEBUG;
                }
                else if(userData.IsPublicRoles)
                {
                    level = OrderLogLevel.ERROR;
                }
                //else if (userData.IsPublicRoles)
                //{
                //    level = OrderLogLevel.WARN;
                //}
                else
                {
                    level = OrderLogLevel.INFO;
                }
                return new OperationResult(true, null, logRepo.GetHistory(OrderID, level));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected error: Cannot get order logs in this moment"]);
            }
        }


        [HttpPost, Route("orders/actions/getlogbymsj/{msj}")]
        public OperationResult GetLogByMsj([FromBody] List<int> orderIds, string msj)
        {
            try
            {
                var logRepo = factory.GetInstance<IOrderLogRepository>();
                return new OperationResult(true, null, logRepo.GetHistoryByMsj(orderIds, msj));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected error: Cannot get order logs in this moment"]);
            }
        }




        [HttpGet, Route("orders/actions/getgrouplog/{OrderGroupID}")]
        public OperationResult GetLogByOrderGroup(int orderGroupID)
        {

            try
            {

                var logRepo = factory.GetInstance<IOrderLogRepository>();

                OrderLogLevel level = OrderLogLevel.WARN;

                if(userData.IsSysAdmin)
                {
                    level = OrderLogLevel.DEBUG;
                }
                else if(userData.IsIDTAdminRoles)
                {
                    level = OrderLogLevel.ERROR;
                }
                else if(userData.IsPublicRoles)
                {
                    level = OrderLogLevel.WARN;
                }
                else
                {
                    level = OrderLogLevel.INFO;
                }
                return new OperationResult(true, null, logRepo.GetOrderGroupHistory(orderGroupID, level));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Unexpected error: Cannot get order logs in this moment"]);
            }
        }


        [HttpPost, Route("/orders/actions/printpackage/{orderid}")]
        public OperationResult PrintPackage(int orderid)
        {
            try
            {
                var order = orderRepo.GetByID(orderid);
                var project = projectRepo.GetByID(order.ProjectID);
                if(order.ProductionType != ProductionType.CustomerLocation && project.DisablePrintLocal == false)
                {
                    var pps = factory.GetInstance<IPrintPackageService>();
                    pps.CreatePrintPackage(orderid);
                    return new OperationResult(true, g["Operation completed successfully!"]);
                }
                else
                {
                    return new OperationResult(false, g["Invalid Operation: This order is a local order or 'Print Local' is disabled for the project."]);
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        /// <summary>
        /// generate and get productionsheet for customerservice
        /// </summary>
        /// <param name="rq"></param>
        /// <returns></returns>
        [HttpGet, Route("/orders/actions/getproductionsheet/{orderID}")]
        public void GetProductionSheet(int orderID)
        {
            try
            {
                IOrderDocumentService docSrv = factory.GetInstance<IOrderDocumentService>();
                //_ = await docSrv.CreatePreviewDocument(rq.OrderID);
                var tmp = docSrv.CreateProdSheetDocument(orderID).Result;
                //return new OperationResult(true, g["Documents regenerated!"]);

                var file = docSrv.GetProdSheetDocument(orderID);
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

        [HttpGet, Route("/orders/actions/getpreviewdocument/{orderID}")]
        public async Task GetPreviewDocument(int orderID)
        {
            try
            {
                IOrderDocumentService docSrv = factory.GetInstance<IOrderDocumentService>();
                var order = orderRepo.GetByID(orderID, true);
                var filter = new OrderReportFilter() { OrderID = orderID, OrderDate = order.CreatedDate.AddDays(-1), OrderDateTo = order.CreatedDate.AddDays(1) };
                var found = await orderRepo.GetOrderReportPage(filter);
                var header = found.Where(w => w.IsGroup).FirstOrDefault();
                var detail = found.Where(w => w.OrderID.Equals(orderID)).FirstOrDefault();
                var tmp = await docSrv.CreatePreviewDocument(orderID);

                var fileName = System.Net.WebUtility.UrlEncode(tempFileService.SanitizeFileName($"VT_{header.ProviderClientReference}_{header.OrderNumber}_{detail.ArticleCode}_{orderID}.pdf"));
                var file = docSrv.GetPreviewDocument(orderID);

                Response.Headers.Add("Content-Type", "application/pdf");
                Response.Headers.Add("content-disposition", $"filename={fileName}");
                using(var fs = file.GetContentAsStream())
                {
                    fs.CopyTo(Response.Body);
                }
            }
            catch(Exception ex)
            {
                log.LogException("Excepcion ActionsController::GetPreviewDocument", ex);
                Response.StatusCode = 404;
            }
        }

        [HttpGet, Route("/orders/actions/getpreviewdocumentbyselection/{orders}")]
        public async Task<IActionResult> GetPrintPackages(string orders)
        {
            var ids = orders.Split('.').Select((i) => Convert.ToInt32(i));
            Response.Headers.Add("Content-Disposition", "attachment");
            try
            {
                string fileName = await repo.PackMultiplePreviewDocument(ids);
                return File(System.IO.File.OpenRead(fileName), MimeTypes.GetMimeType(Path.GetExtension(fileName)), Path.GetFileName(fileName));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return NotFound();
            }
        }


        [HttpGet, Route("/orders/actions/downloadpreviewdocument/{orderID}")]
        public async Task<ActionResult> DownloadPreviewDocument(int orderID)
        {

            var fileName = $"Preview_{orderID}.pdf";

            try
            {

                IOrderDocumentService docSrv = factory.GetInstance<IOrderDocumentService>();
                var articles = orderRepo.GetOrderArticles(new OrderArticlesFilter() { OrderID = new List<int>() { orderID } }, ProductDetails.None);
                fileName = $"Preview_{articles.ElementAt(0).OrderNumber}_{articles.ElementAt(0).Article}_{articles.ElementAt(0).OrderID}.pdf";

                var tmp = await docSrv.CreatePreviewDocument(orderID);
                //var tmp = docSrv.CreateProdSheetDocument(orderID).Result;
                //return new OperationResult(true, g["Documents regenerated!"]);

                var file = docSrv.GetPreviewDocument(orderID);
                Response.Headers.Add("Content-Type", "application/pdf");

                return File(file.GetContentAsStream(), "application/force-download", fileName);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                Response.StatusCode = 404;

                var msg = "Error to Generate Document Preview";

                if(userData.IsIDT)
                {
                    msg += Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace;
                }

                MemoryStream strm = new MemoryStream(Encoding.UTF8.GetBytes(msg));

                return File(strm, "application/force-download", $"FileNotFound_For_{fileName}");
            }
        }


        [HttpPost, Route("orders/actions/setasvalidgroup")]
        public OperationResult SetAsValidGroup([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {

                selection.ForEach(ValidateSelectionAsIs);

                return new OperationResult(true, g["Selected order(s) were marked as Validated"]);

            }
            catch(NotDefaultAddressFoundException _ex1)
            {

                log.LogException(_ex1);
                return new OperationResult(false, g["Default delivery address not found.  Please, register an address first"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again"]);
            }
        }

        [HttpPost, Route("orders/actions/resetvalidationgroup")]
        public OperationResult ResetValidationGroup([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                List<string> errors = new List<string>();
                //selection.ForEach(ResetValidation, errors);
                selection.ForEach(sel => ResetValidation(sel, errors));
                return new OperationResult(true, g["Selected order(s) were marked as Validated"], errors);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again"]);
            }
        }


        private void StopOrders(OrderGroupSelectionDTO obj)
        {
            foreach(var orderID in obj.Orders)
            {
                actionsService.StopOrder(orderID);
                orderLog.Info(orderID, "Order was stoped.");
            }
        }

        private bool CheckCanContinue(OrderGroupSelectionDTO obj)
        {
            List<string> authorizedUsers = new List<string>
                    {
                        "toni.esteve",
                        "roger.civera@indetgroup.com",
                        "maria.longueira@indetgroup.com",
                        "cristina.oliet",
                        "alejandro.corral",
                        "xavier.cubero",
                        "sebastian.canal",
                        "sergio.garrido"
                    };
            using(var db = factory.GetInstance<PrintDB>())
            {
                var projects = db.Projects
                    .Join(db.Brands,
                        p => p.BrandID,
                        b => b.ID,
                        (p, b) => new { Project = p, Brand = b })
                    .Join(db.Companies,
                        pb => pb.Brand.CompanyID,
                        c => c.ID,
                        (pb, c) => new { pb.Project, pb.Brand, Company = c })
                    .Where(x => x.Company.Name.ToUpper().Contains("INDITEX"))
                    .Select(x => x.Project.ID).ToList();

                if(projects.Any(p => obj.ProjectID == p))
                {
                    if(userData.IsCompositionChecker)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                    //if(authorizedUsers.Contains(userData.UserName))
                    //{
                    //    return true;
                    //}
                    //else
                    //{
                    //    return false;
                    //}
                }
            }
            return true;
        }

        private void ResumeOrders(OrderGroupSelectionDTO obj)
        {

            foreach(var orderID in obj.Orders)
            {
                actionsService.ResumeOrder(orderID);
                orderLog.Info(orderID, "Order resumed.");
            }
        }
        private void UpdateOrdersLocation(UpdateLocationRequest data)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();

            foreach(var orderId in data.Orders)
            {
                actionsService.MoveOrder(orderId, data.LocationId);
                orderLog.Info(orderId, "Production Location was updated.");
            }
        }

        private void ValidateSelectionAsIs(OrderGroupSelectionDTO obj)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();
            var wzdRepo = factory.GetInstance<IWizardRepository>();

            foreach(var orderID in obj.Orders)
            {
                var orderInfo = repo.GetProjectInfo(orderID);
                // here is set delivery address, or throw an Exception
                repo.ChangeStatus(orderID, OrderStatus.Validated);

                wzdRepo.SetAsComplete(orderID);

                events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));

                orderLog.Info(orderID, "Validated as is");
            }
        }

        private void ResetValidation(OrderGroupSelectionDTO obj, List<string> errorsOut)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();

            foreach(var orderID in obj.Orders)
            {
                #region CHECK if order exist in ERP before reset
                var order = orderRepo.GetByID(orderID);
                bool existInERP = false;

                if(order.SyncWithSage && !string.IsNullOrEmpty(order.SageReference))
                {
                    existInERP = sageClient.CheckIfOrderExistAsync(order.SageReference, order.ID).Result;
                }


                if(existInERP)
                {
                    notificationMng.RegisterResetValidationNotification(order);
                }


                repo.ChangeStatusWF(orderID, OrderStatus.InFlow, true);
                //var wizard = wzdRepo.GetByOrder(orderID);
                //wzdRepo.Reset(wizard.ID);
                orderLog.Info(orderID, g["Order Validation has been restarted"]);


                #endregion CHECK if order exist in ERP before reset
            }
        }

        [HttpPost, Route("/orders/actions/clonebygroup")]
        public OperationResult CloneByGroup([FromBody] List<CloneRequest> selection)
        {
            try
            {
                selection.ForEach(Clone);
                return new OperationResult(true, g["Selected order(s) were cloned"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, cannot clone selected order(s)"]);
            }
        }

        private void Clone(CloneRequest data)
        {

            if(!userData.IsIDT) data.IsBillable = true; // repetitions made for external user always is billable

            repo.Clone(data.OrderId, data.IsBillable, data.ArticleCode, null, userData.UserName, false, DocumentSource.Repetition);
        }

        [HttpPost, Route("/orders/actions/changeprovider")]
        public OperationResult ChangeProvider([FromBody] ChangeProviderRequest rq)
        {
            var grpRepo = factory.GetInstance<IOrderGroupRepository>();

            try
            {
                var orders = grpRepo.ChangeProvider(rq.OrderGroupID, rq.ProviderRecordID);

                foreach(var o in orders)
                {
                    orderLog.Debug(o.ID, "New provider was assigned");
                }

                return new OperationResult(true, g["Provider was changed"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, something is wrong"]);
            }
        }

        [HttpPost, Route("/orders/actions/derive/{orderID}/{providerID}/{articleCode}")]
        public OperationResult Derive(int orderID, int providerID, string articleCode)
        {
            try
            {
                repo.Clone(orderID, true, articleCode, providerID, userData.UserName, false, DocumentSource.Repetition);

                //var order = repo.ChangeOrderProvider(orderID, providerID);

                //orderLog.Debug(order.ID, "New provider was assigned");

                return new OperationResult(true, g["Provider was changed"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Try again, something is wrong"]);
            }
        }

        [HttpPost, Route("/orders/actions/changeproductiontype")]
        public OperationResult ChangeProductionType([FromBody] ChangeProductionTypeRequest rq)
        {
            try
            {
                var orderRepo = factory.GetInstance<IOrderRepository>();

                orderRepo.ChangeProductionType(rq.OrderID, rq.ProductionType);

                orderLog.Warn(rq.OrderID, "Production Type was changed");

                return new OperationResult(true, g["Production Type was changed"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);

                return new OperationResult(false, g["Try again, something is wrong"]);
            }
        }

        [HttpPost, Route("/orders/actions/changeordernumber")]
        public OperationResult ChangeOrderNumber([FromBody] ChangeOrderNumberRequest rq)
        {
            try
            {
                orderGroupRepo.ChangeOrderNumber(rq.OrderGroupID, rq.OrderNumber);

                return new OperationResult(true, g["Order Number was changed"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);

                return new OperationResult(false, g["Try again, something is wrong change order number"]);
            }
        }

    }


    public class ActionRequest
    {
        public int OrderID { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public bool ResetEvent { get; set; }
        public string Comments { get; set; }

        public ActionRequest()
        {
            OrderID = -1;

            OrderStatus = OrderStatus.None;

            ResetEvent = false;

            Comments = null;
        }
    }


    public class ActionRequestGetStepBySelection
    {
        public int Position { get; set; }

        public List<OrderGroupSelectionDTO> Selection { get; set; }

        public ActionRequestGetStepBySelection()
        {
            Position = -1;
            Selection = new List<OrderGroupSelectionDTO>();
        }
    }

    public class UpdateLocationRequest
    {
        public int[] Orders { get; set; }
        public int LocationId { get; set; }
    }

    public class UpdateStatusRequest
    {
        public List<OrderGroupSelectionDTO> Selection { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public bool ResetEvent { get; set; }
        public string Comments { get; set; }
    }

    public class ChangeProviderRequest
    {
        public int OrderGroupID { get; set; }
        public int ProviderCompanyID { get; set; }
        public int ProviderRecordID { get; set; }
    }

    public class ChangeProductionTypeRequest
    {
        public int OrderID { get; set; }
        public ProductionType ProductionType { get; set; }
    }

    public class ChangeOrderNumberRequest
    {
        public int OrderGroupID { get; set; }
        public string OrderNumber { get; set; }
        public int ProjectID { get; set; }
    }


}