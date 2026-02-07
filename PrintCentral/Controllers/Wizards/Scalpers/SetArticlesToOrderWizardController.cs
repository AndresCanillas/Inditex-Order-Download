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
    public class SetArticlesToOrderWizard : Controller
    {
        private IFactory factory;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private IOrderRepository orderRepo;
        private IScalpersOrderValidationService customerSrv;
        

        public SetArticlesToOrderWizard(
            IFactory factory,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IOrderRepository orderRepo,
            IScalpersOrderValidationService customerSrv
            )
        {
            this.factory = factory;
            this.g = g;
            this.log = log;
            this.events = events;
            this.orderRepo = orderRepo;
            this.customerSrv = customerSrv;
            
        }

        /// <summary>
        /// Articles depend of CODSECTION
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        [HttpPost, Route("/order/validate/scalpers/getorderdata")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                // try to get Sizes, Colors, Gama for this Orders
                var result = customerSrv.GetOrderData(selection);
                var labelCatalogs = -1;
                return new OperationResult(true, null, new { OrderData = result, LabelCatalogs = labelCatalogs });
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/order/validate/scalpers/savequantities")]
        [DisableRequestSizeLimit]
        //[RequestFormSizeLimit(valueCountLimit: 4000)]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult SaveState([FromForm] List<ScalpersOrderGroupQuantitiesDTO> rq)
        {
            try
            {
                _SaveState(rq);

                return new OperationResult(true, g["State Saved"]);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }


        [HttpPost, Route("/order/validate/scalpers/solvequantities")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult Solve([FromForm]List<ScalpersOrderGroupQuantitiesDTO> rq)
        {

            try
            {

                _SaveState(rq);

                #region Shared Data - Refresh OrderGroupSelection
                var selection = new List<OrderGroupSelectionDTO>();

                rq.ForEach(e =>
                {
                    var sel = new OrderGroupSelectionDTO()
                    {
                        OrderGroupID = e.OrderGroupID
                    };

                    selection.Add(sel);
                });

                var result = customerSrv.GetOrderData(selection);
                

                rq.ForEach(q => {

                    var orderSelected = orderRepo.GetByID(q.Quantities[0].OrderID, true);

                    // result contains all companyorders inner the same OrderGroup
                    // for wizard with SharedData with
                    result.ForEach(sel =>
                    {
                        var details = sel.Details.Where(w => w.OrderDataID == orderSelected.OrderDataID).ToList();

                        sel.Details = details;
                        sel.Orders = sel.Details.Select(s => s.OrderID).ToArray();

                    });
                });


                #endregion Shared Data - Refresh OrderGroupSelection

                _UpdateWizardProgress(result);

                _RegisterOrderLogs(result);

                return new OperationResult(true, null, result);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        private List<ScalpersOrderGroupQuantitiesDTO> _SaveState(List<ScalpersOrderGroupQuantitiesDTO> rq)
        {
            return customerSrv.UpdateOrders(rq);
        }


        private void _UpdateWizardProgress(List<OrderGroupSelectionDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            foreach (var gp in rq)
            {

                var selectedOrders = gp.Orders.Select(s => s);

                wzStpRepo.MarkAsCompleteByGroup(gp.WizardStepPosition, selectedOrders);

                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach (var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);

                    events.Send(new QuantitiesStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupSelectionDTO> rq)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();

            foreach (var gp in rq)
            {
                var selectedOrders = gp.Orders.Select(s => s);

                foreach (var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Quantities Validation Completed"]);

                }
            }
        }

        /*
        private void _UpdateWizardProgress(List<OrderGroupQuantitiesDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            //wzdRepo.MarkAsComplete(rq.WizardStepID);

            //repo.UpdateProgress(rq.WizardID);

            foreach (var gp in rq)
            {

                var selectedOrders = gp.Quantities.Select(s => s.OrderID);

                wzStpRepo.MarkAsCompleteByGroup(gp.WizardStepPosition, selectedOrders);

                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach (var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);

                    events.Send(new QuantitiesStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupQuantitiesDTO> rq)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();


            //orderLog.InfoAsync(rq.OrderID, g["Quantities Validation Completed"]);
            foreach (var gp in rq)
            {
                // register every OrderID one time
                var selectedOrders = gp.Quantities.Select(s => s.OrderID).Distinct();

                foreach (var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Quantities Validation Completed"]);
                }
            }
        }

        */
    }

}
