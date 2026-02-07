using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers.Wizards.AlvaroMoreno
{
    public class MadeInWizardController : Controller
    {
        private ILogService log;
        private IAlvaroMorenoOrderValidationService customerSrv;
        private ILocalizationService g;
        private IFactory factory;
        private IEventQueue events;

        public MadeInWizardController(ILocalizationService g, ILogService log, IAlvaroMorenoOrderValidationService customerSrv, IFactory factory, IEventQueue events)
        {
            this.g = g;
            this.log = log;
            this.customerSrv = customerSrv;
            this.factory = factory;
            this.events = events;
        }

        [HttpPost, Route("/order/validate/alvaromoreno/getorderdata")]
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

        [HttpPost, Route("/order/validate/alvaromoreno/savequantities")]
        [DisableRequestSizeLimit]
        //[RequestFormSizeLimit(valueCountLimit: 4000)]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult SaveState([FromForm] List<OrderGroupQuantitiesDTO> rq)
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

        private void _SaveState(List<OrderGroupQuantitiesDTO> rq)
        {
            //Update Made In Order

            customerSrv.UpdateOrdersMadeIn(rq);

            //Update Quantities Order
            var pjRepo = factory.GetInstance<IPrinterJobRepository>();
            pjRepo.UpdateQuantiesByGroup(rq);
        }

        [HttpPost, Route("/order/validate/alvaromoreno/solvequantities")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult Solve([FromForm] List<OrderGroupQuantitiesDTO> rq)
        {

            try
            {

                _SaveState(rq);

                _UpdateWizardProgress(rq);

                _RegisterOrderLogs(rq);


                return new OperationResult(true, null);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }


        private void _UpdateWizardProgress(List<OrderGroupQuantitiesDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            foreach (var gp in rq)
            {

                if (gp.Quantities == null) return; // next group

                var selectedOrders = gp.Quantities.Select(s => s.OrderID);

                if (selectedOrders == null) return; // next group

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

            foreach (var gp in rq)
            {
                if (gp.Quantities == null) return; // skip

                // register every OrderID one time
                var selectedOrders = gp.Quantities.Select(s => s.OrderID).Distinct();

                foreach (var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Quantities Validation Completed"]);
                }
            }
        }


    }
}
