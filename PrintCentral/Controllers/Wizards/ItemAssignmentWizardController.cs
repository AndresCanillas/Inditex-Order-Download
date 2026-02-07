using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services.Wizards;

namespace PrintCentral.Controllers.Wizards
{
    [Authorize]
    public class ItemAssignmentWizardController : Controller
    {

        private IFactory factory;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private IItemAssigmentService customerSrv;

        public ItemAssignmentWizardController(
            IFactory factory,
            IUserData userData,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IItemAssigmentService customerSrv)
        {
            this.factory = factory;
            this.userData = userData;
            this.g = g;
            this.log = log;
            this.events = events;
            this.customerSrv = customerSrv;
        }


        [HttpPost, Route("/order/validate/itemassignment/getordersdetails")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                var result = customerSrv.GetOrderData(selection);
                var labelCatalogs = -1;
                return new OperationResult(true, null, new { OrderData = result, LabelCatalogs = labelCatalogs });
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/order/validate/itemassignment/assigmentdetails")]
        public OperationResult GetOrderAssigmentArticlesDetailed([FromBody] CustomDetailSelectionDTO rq)
        {
            try
            {
                var repo = factory.GetInstance<IOrderRepository>();

                // all articles
                var filter = new OrderArticlesFilter()
                {
                    ArticleType = rq.GetAllArticles ? ArticleTypeFilter.All : ArticleTypeFilter.Label,
                    ActiveFilter = OrderActiveFilter.All,
                    OrderStatus = OrderStatusFilter.InFlow
                };


                // get all articles by OrderGroupID
                var cloneSelection = new List<OrderGroupSelectionDTO>();

                rq.Selection.ForEach(e =>
                {
                    var orders = new List<int>() { };// leave empty to get all orders for this group
                    var itemSel = new OrderGroupSelectionDTO(e);
                    itemSel.Details = new List<OrderDetailDTO>();
                    itemSel.Orders = orders.ToArray();
                    cloneSelection.Add(itemSel);
                });

                var result = repo.GetArticleDetailSelection(cloneSelection, filter, true, rq.ProductFields);


                // update order IDS from details in result to update wizard selection
                result.ForEach(r =>
                {
                    if(r.Details == null || r.Details.Count() < 1)
                    {
                        return;
                    }

                    r.Orders = r.Details.OrderBy(a => a.ArticleID).ThenBy(b => b.PrinterJobDetailID).Select(s => s.OrderID).Distinct().ToArray();
                    r.Details = r.Details.OrderBy(a => a.ArticleID).ThenBy(b => b.PrinterJobDetailID).ToList();

                });


                return new OperationResult(true, null, result);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/order/validate/itemassignment/savearticles")]
        public OperationResult Save([FromBody] CustomDetailSelectionDTO rq)
        {
            try
            {
                _SaveState(rq);


                return new OperationResult(true, null, rq);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        //private void _SaveBaseData(CustomDetailSelectionDTO rq)
        //{
        //    customerSrv.UpdateBaseDataOrders(rq);
        //}

        [HttpPost, Route("/order/validate/itemassignment/solve")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult Solve([FromBody] CustomDetailSelectionDTO rq)
        {

            try
            {
                _SaveState(rq);

                #region Shared Data - Refresh OrderGroupSelection
                var selection = new List<OrderGroupSelectionDTO>();

                rq.Selection.ForEach(e =>
                {
                    var sel = new OrderGroupSelectionDTO()
                    {
                        OrderGroupID = e.OrderGroupID
                    };

                    selection.Add(sel);
                });

                var result = customerSrv.GetOrderData(selection, GetOrderFilter(rq));


                //rq.ForEach(q => {

                //    var orderSelected = orderRepo.GetByID(q.Quantities[0].OrderID, true);

                //    // result contains all companyorders inner the same OrderGroup
                //    // for wizard with SharedData with
                //    result.ForEach(sel =>
                //    {
                //        var details = sel.Details.Where(w => w.OrderDataID == orderSelected.OrderDataID).ToList();

                //        sel.Details = details;
                //        sel.Orders = sel.Details.Select(s => s.OrderID).ToArray();

                //    });
                //});


                #endregion Shared Data - Refresh OrderGroupSelection

                _UpdateWizardProgress(result);

                _RegisterOrderLogs(result);

                return new OperationResult(true, null, result);

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        private CustomDetailSelectionDTO _SaveState(CustomDetailSelectionDTO rq)
        {
            return customerSrv.UpdateOrders(rq);
        }

        private void _UpdateWizardProgress(List<OrderGroupSelectionDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            foreach(var gp in rq)
            {

                var selectedOrders = gp.Orders.Select(s => s);

                wzStpRepo.MarkAsCompleteByGroup(gp.WizardStepPosition, selectedOrders);

                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach(var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);

                    events.Send(new QuantitiesStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupSelectionDTO> rq)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();

            foreach(var gp in rq)
            {
                var selectedOrders = gp.Orders.Select(s => s);

                foreach(var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Quantities Validation Completed"]);

                }
            }
        }

        private OrderArticlesFilter GetOrderFilter(CustomDetailSelectionDTO rq)
        {
            return new OrderArticlesFilter()
            {
                ArticleType = rq.GetAllArticles ? ArticleTypeFilter.All : ArticleTypeFilter.Label,
                ActiveFilter = OrderActiveFilter.All,
                OrderStatus = OrderStatusFilter.InFlow
            };
        }

        [HttpPost]
        [Route("/order/validate/itemassignment/SaveSizes")]
        public Task<OperationResult> SaveSizes([FromBody] List<FieldsToUpdateDTO> rq)
        {
            var response = new OperationResult(true, g["Sizes are Updated"]);

            try
            {
                customerSrv.UpdateSizes(rq);
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = g["Operation could not be completed."];
                if(userData.IsSysAdmin)
                {
                    response.Data = ex.Message;
                }


                log.LogException(ex);
            }

            return Task.FromResult(response);

        }

        [HttpPost]
        [Route("/order/validate/itemassignment/SaveTagTypes")]
        public Task<OperationResult> SaveTagTypes([FromBody] List<FieldsToUpdateDTO> rq)
        {
            var response = new OperationResult(true, g["TagTypes are Updated"]);

            try
            {
                customerSrv.UpdateTagtypes(rq);
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = g["Operation could not be completed."];
                if(userData.IsSysAdmin)
                {
                    response.Data = ex.Message;
                }


                log.LogException(ex);
            }

            return Task.FromResult(response);

        }

        [HttpPost]
        [Route("/order/validate/itemassignment/UpdateTrackinCode")]
        public Task<OperationResult> UpdateTrackingCode([FromBody] List<int> OrderIds)
        {
            var response = new OperationResult(true, g["TagTypes are Updated"]);

            try
            {
                customerSrv.UpdateCustomTrackinCode(OrderIds);
            }
            catch(Exception ex)
            {
                response.Success = false;
                response.Message = g["Operation could not be completed."];
                if(userData.IsSysAdmin)
                {
                    response.Data = ex.Message;
                }


                log.LogException(ex);
            }

            return Task.FromResult(response);



            return Task.FromResult(response);

        }
    }


}