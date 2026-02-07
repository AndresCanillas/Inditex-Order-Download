using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers.Orders
{
    public class ConflictResolverController : Controller
    {
        private IFactory factory;
        private IOrderRepository repo;
        private IUserData userData;
        private ILocalizationService g;
        private IEventQueue events;
        private ILogService log;


        public ConflictResolverController(
            IFactory factory,
            IOrderRepository repo,
            IUserData userData,
            ILocalizationService g,
            IEventQueue events,
            ILogService log)
        {
            this.factory = factory;
            this.repo = repo;
            this.userData = userData;
            this.g = g;
            this.events = events;
            this.log = log;
        }

        [HttpPost, Route("order/solveconflict")]
        public OperationResult Solve([FromBody] SolveConflictRequest rq)
        {
            try {

                _SaveState(rq);

                return new OperationResult(true, g["Conflict is solved now"]);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        private void _SaveState(SolveConflictRequest rq)
        {
            var repo = factory.GetInstance<IOrderRepository>();
            var propRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();
            var orderActionService = factory.GetInstance<IOrderActionsService>();
            var orderLog = factory.GetInstance<IOrderLogService>();
            var orderUpdateService = factory.GetInstance<IOrderUpdateService>();
            IComparerConfiguration comparerConfig = null;

            // check the conflict order state, current order cannot be cancelled if already in production or registerd inner ERP, require a IDT user to execute this action
            Parallel.ForEach(rq.OrdersInConflic, (OrderOption orderOption) => {
                var currentState = repo.GetByID(orderOption.OrderId);
                if (!string.IsNullOrEmpty(currentState.SageReference) || currentState.OrderStatus == OrderStatus.Printing)
                    orderOption.Accepted = true;
            });

            foreach (var option in rq.OrdersInConflic)
            {
                if (comparerConfig == null)
                {
                    comparerConfig = repo.GetComparerType(option.OrderId);
                }

                var props = propRepo.GetByOrderID(option.OrderId);
                var order = repo.GetByID(option.OrderId);

                // conflict is solved
                order.IsInConflict = false;

                if (option.Accepted)
                {
                    props.IsActive = true;
                    props.IsRejected = false;
                    order.IsStopped = false;

                    if (order.OrderStatus == OrderStatus.Received)
                    {
                        var newInfo = repo.GetProjectInfo(option.OrderId);
                        events.Send(new OrderExistVerifiedEvent(newInfo.OrderGroupID, newInfo.OrderID, newInfo.OrderNumber, newInfo.CompanyID, newInfo.BrandID, newInfo.ProjectID, true));
                    }
                    else
                    {
                        //Resume Order lanza el evento OrderResumedEvent para Print Local
                        orderActionService.ResumeOrder(option.OrderId);
                    }
                }
                else
                {
					var originalOrderstatus = order.OrderStatus;

					props.IsActive = false;
                    props.IsRejected = true;
                    order.OrderStatus = OrderStatus.Cancelled;
                    order.IsStopped = true;
                    orderLog.Info(option.OrderId, "Rejected In Conflict Resolver");
                    orderActionService.RejectOrder(option.OrderId);

                    // Lanzamos evento OrderExistVerifiedEvent
                    if (originalOrderstatus == OrderStatus.Received)
                    {
						var newInfo = repo.GetProjectInfo(option.OrderId);
						events.Send(new OrderExistVerifiedEvent(newInfo.OrderGroupID, newInfo.OrderID, newInfo.OrderNumber, newInfo.CompanyID, newInfo.BrandID, newInfo.ProjectID, false));
                    }
                }

                propRepo.Update(props);
                repo.Update(order);

                #region Conflict Method SharerData
                if (comparerConfig.Method == ConflictMethod.SharedData) {

                    using (var ctx = factory.GetInstance<PrintDB>())
                    {

                        repo.SetConflictStatusByShareData(ctx, false, option.OrderId);

                        var sharredData = repo.GetOrderWithSharedData(ctx, option.OrderId).ToList();

                        // remove stop
                        if (option.Accepted == true)
                        {
                            sharredData.ToList().ForEach(ord => orderActionService.ResumeOrder(ctx, ord.ID));
                        }
                        else
                        {
                            orderUpdateService.RejectBySharedData(ctx, option.OrderId);
                            sharredData.ForEach(ord => orderActionService.RejectOrder(ord.ID));
                        }

                    }
                }

                #endregion



            }

        }


        //private void _SaveStateOld(SolveConflictRequest rq)
        //{

        //    var repo = factory.GetInstance<IOrderRepository>();
        //    var propRepo = factory.GetInstance<IOrderUpdatePropertiesRepository>();
        //    var orderActionService = factory.GetInstance<IOrderActionsService>();
        //    var orderLog = factory.GetInstance<IOrderLogService>();
        //    var acceptedID = rq.PrevOrder.Accepted ? rq.PrevOrder.OrderId : rq.NewOrder.OrderId;
        //    var rejectedID = rq.PrevOrder.Accepted ? rq.NewOrder.OrderId : rq.PrevOrder.OrderId;
        //    var propAccepted = propRepo.GetByOrderID(acceptedID);
        //    var propRejected = propRepo.GetByOrderID(rejectedID);

        //    propAccepted.IsActive = true;
        //    propAccepted.IsRejected = false;
        //    propRepo.Update(propAccepted);

        //    propRejected.IsActive = false;
        //    propRejected.IsRejected = true;
        //    propRepo.Update(propRejected);

        //    // conflict was resolved
        //    var orderConflicted = repo.GetByID(rq.PrevOrder.OrderId);
        //    orderConflicted.IsInConflict = false;
        //    repo.Update(orderConflicted);

        //    // cancel rejected order 
        //    // TODO: maybe move this to orderActionsService
        //    repo.ChangeStatus(rejectedID, OrderStatus.Cancelled);
        //    orderLog.Warn(rejectedID, "Rejected In Conflict Resolver");

        //    // TODO: evento de la nueva orden
        //    if (rq.NewOrder.Accepted)
        //    {
        //        var newInfo = repo.GetProjectInfo(rq.NewOrder.OrderId);
        //        events.Send(new OrderReceivedEvent(newInfo.OrderGroupID, newInfo.OrderID, newInfo.OrderNumber, newInfo.CompanyID, newInfo.BrandID, newInfo.ProjectID));

        //        orderActionService.RejectOrder(rq.PrevOrder.OrderId);
        //    }
        //    else //if (rq.PrevOrder.Accepted)
        //    {
        //        orderActionService.ResumeOrder(rq.PrevOrder.OrderId);
        //    }




        //}

        [HttpGet, Route("order/imagecomparer/{labelId}/{prevOrderId}/{prevDataId}/{newLabelId}/{newOrderId}/{newDataId}")]
        public async Task<IActionResult> ImageComparer(int labelId, int prevOrderId, int prevDataId, int newLabelId, int newOrderId, int newDataId)
        {
            try
            {
                log.LogMessage($"ImageComparer operation started.");
                var sw = new Stopwatch();
                sw.Start();

                var labelRepo = factory.GetInstance<ILabelRepository>();
                var leftImage = await labelRepo.GetArticlePreviewReferenceAsync(labelId, prevOrderId, prevDataId);
                var rightImage = await labelRepo.GetArticlePreviewReferenceAsync(newLabelId, newOrderId, newDataId);
                var image = await repo.GetComparerPreviews(leftImage, rightImage, newDataId);

                sw.Stop();
                log.LogMessage($"ImageComparer completed in {sw.ElapsedMilliseconds} ms, image size {image.Length} bytes.");
                return File(image, "image/png");
            }
            catch (Exception ex)
            {
                log.LogException("ImageComparer error", ex);
                return File("~/images/no_preview.png", "image/png", "no_preview.png"); ;
            }
        }
    }

    public class SolveConflictRequest
    {
        public IList<OrderOption> OrdersInConflic { get; set; }
    }

    public class OrderOption
    {
        public int OrderId { get; set; }
        public bool Accepted { get; set; }
        public List<string> Items { get; set; }
    }
}


