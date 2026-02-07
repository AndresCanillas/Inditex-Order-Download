using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    public class ArticlesExtrasWizardController : Controller
    {
        private IFactory factory;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;

        public ArticlesExtrasWizardController(
            IFactory factory,
            IUserData userData,
            ILocalizationService g,
            ILogService log,
            IEventQueue events
            )
        {
            this.factory = factory;
            this.userData = userData;
            this.g = g;
            this.log = log;
            this.events = events;
        }

        [HttpPost, Route("/order/validate/getordersextradetails")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                var repo = factory.GetInstance<IOrderRepository>();

                // proyect configuration
                var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Item, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow };
                var result = repo.GetItemsExtrasDetailSelection(selection, filter);

                //result.ForEach(r => r.Details = r.Details.Where(w => w.QuantityRequested < 1).ToList());
                    

                return new OperationResult(true, null, result);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/order/validate/getordersextradetails/unselected")]
        public OperationResult GetOrderArticlesDetailedUnselected([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                var repo = factory.GetInstance<IOrderRepository>();


                var cloneSelection = new List<OrderGroupSelectionDTO>();

                selection.ForEach(s =>
                {
                    var c = new OrderGroupSelectionDTO(s);
                    c.Orders = Array.Empty<int>(); // remove orders selecte to get all orders inner group
                    cloneSelection.Add(c);
                });


                // proyect configuration
                var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Item, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow };
                var result = repo.GetItemsExtrasDetailSelection(cloneSelection, filter);


                // return only no selected 

                result.ForEach(r =>
                {
                    var s = selection.First(f => f.OrderGroupID == r.OrderGroupID);

                    r.Details = r.Details.Where(w => s.Orders.Any(a => a != w.OrderID)).ToList();
                });

                return new OperationResult(true, null, result);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                var data = string.Empty;

                if(userData.IsIDTAdminRoles)
                    data = ex.Message;

                return new OperationResult(false, g["Operation could not be completed."], data);
            }
        }


        [HttpPost, Route("/order/validate/saveextras/")]
        [DisableRequestSizeLimit]
        public OperationResult SaveState([FromForm]List<OrderGroupExtraItemsDTO> rq)
        {
            try
            {
                bool asActive = Request.Query["asActive"] == "1" ?  true : false;

                _SaveState(rq, asActive);

                return new OperationResult(true, g["State Saved"], rq);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }


        [HttpPost, Route("/order/validate/solveextras")]
        [DisableRequestSizeLimit]
        public OperationResult Solve([FromBody] List<OrderGroupSelectionDTO> selection)
        {

            try
            {

                //_SaveState(toSolve.rq);

                _UpdateWizardProgress(selection);

                _RegisterOrderLogs(selection);

                return new OperationResult(true, null, selection);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        private void _SaveState(List<OrderGroupExtraItemsDTO> rq, bool asActive)
        {
            var orderRepo = factory.GetInstance<IOrderRepository>();
            // if "isActive" is true, articles will be show in order list
            orderRepo.AddExtraItemsByGroup(rq, asActive);

            // TODO: la validacion se puede marcar cuando se confirme en el review y esto se puede eliminar,
            // ya esta realizada la modificacion solo falta probar
            // ir a ReviewController :: _SaveState
            //if (asActive)
            //{
            //    rq.ForEach(gp => {
            //        gp.Items.ForEach(item => {
            //            orderRepo.ChangeStatus(item.OrderID.Value, OrderStatus.Validated);
            //        });
            //    });
            //}
        }

        private void _UpdateWizardProgress(List<OrderGroupSelectionDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            //wzdRepo.MarkAsComplete(rq.WizardStepID);

            //repo.UpdateProgress(rq.WizardID);

            foreach (var gp in rq)
            {
                // update only the saved orders
                var selectedOrders = gp.Orders.Where(w => w > 0).Select(s => s);

                wzStpRepo.MarkAsCompleteByGroup(gp.WizardStepPosition, selectedOrders);

                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach (var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);

                    events.Send(new ArticlesExtraStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupSelectionDTO> rq)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();


            //orderLog.InfoAsync(rq.OrderID, g["Quantities Validation Completed"]);
            foreach (var gp in rq)
            {
                var selectedOrders = gp.Orders.Where(w => w > 0).Select(s => s);

                foreach (var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Articles Extras Completed"]);

                }
            }
        }
    }

}
