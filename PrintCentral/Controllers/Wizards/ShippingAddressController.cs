using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    public class ShippingAddressController
    {

        private IFactory factory;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;

        private string ViewFolder = "~/Views/Wizards/";

        public ShippingAddressController(
            IFactory factory,
            ILocalizationService g,
            ILogService log,
            IEventQueue events
            )
        {
            this.factory = factory;
            this.g = g;
            this.log = log;
            this.events = events;
        }

        [HttpPost, Route("/order/validate/getshippingaddress")]
        public OperationResult GetAddress([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                var orderRepo = factory.GetInstance<IOrderRepository>();

                var workSelection = orderRepo.GetOrderShippingAddressByGroup(selection);

                var success = true;
                var msg = g["Current shipping address loaded"];

                if (!workSelection[0].ShippingAddressID.HasValue || workSelection[0].ShippingAddressID.Value < 1)
                {
                    success = false;
                    msg = "";
                }

                return new OperationResult(success, msg, workSelection);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't get address info, please try again"], selection);
            }
        }

        [HttpPost, Route("/order/validate/saveshippingaddress")]
        public OperationResult SaveState([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                _SaveState(selection);

                return new OperationResult(true, g["State Saved"]);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }

        [HttpPost, Route("/order/validate/solveshippingaddress")]
        public OperationResult Solve([FromBody] List<OrderGroupSelectionDTO> selection)
        {

            try
            {

                _SaveState(selection);

                _UpdateWizardProgress(selection);

                _RegisterOrderLogs(selection);

                return new OperationResult(true, null);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        private void _SaveState(List<OrderGroupSelectionDTO> selection)
        {
            var orderRepo = factory.GetInstance<IOrderRepository>();

            var groupRepo = factory.GetInstance<IOrderGroupRepository>();

            foreach (var sel in selection)
            {
                IOrderGroup gp = groupRepo.GetByID(sel.OrderGroupID);
                
                gp.SendToAddressID = sel.ShippingAddressID;

                groupRepo.Update(gp);

                foreach(var o in sel.Orders)
                {
                    IOrder order = orderRepo.GetByID(o);
                    order.SendToAddressID = sel.ShippingAddressID;
                    orderRepo.Update(order);
                }
            }

            // set shipping address to extra articles
            var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Item, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow };
            var result = orderRepo.GetItemsExtrasDetailSelection(selection, filter);

            foreach (var grp in result)
            {
                foreach (var article in grp.Details)
                {
                    if (article.IsBilled) { continue; }

                    var order = orderRepo.GetByID(article.OrderID);
                    order.SendToAddressID = grp.ShippingAddressID;
                    orderRepo.Update(order);
                }
            }

            CheckShippingAddress(selection);

        }

        private void CheckShippingAddress(List<OrderGroupSelectionDTO> selection)
        {
            var orderRepo = factory.GetInstance<IOrderRepository>();

            var groupRepo = factory.GetInstance<IOrderGroupRepository>();

            var addressRepo = factory.GetInstance<IAddressRepository>();

            foreach(var sel in selection)
            {
                IOrderGroup gp = groupRepo.GetByID(sel.OrderGroupID);

                var allAddress = addressRepo.GetByCompany(gp.SendToCompanyID);



                foreach(var o in sel.Orders)
                {
                    IOrder order = orderRepo.GetByID(o);

                    var found = allAddress.Where(f => f.ID == order.SendToAddressID);

                    if(!found.Any()) throw new InvalidDeliveryAddressException($"Order ID [{order.ID}] has an invalid address ID [{order.SendToAddressID}]");

                   
                }
            }
        }

        private void _UpdateWizardProgress(List<OrderGroupSelectionDTO> selection)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            //wzdRepo.MarkAsComplete(rq.WizardStepID);

            //repo.UpdateProgress(rq.WizardID);

            foreach (var gp in selection)
            {

                var selectedOrders = gp.Orders.Select(s => s);

                wzStpRepo.MarkAsCompleteByGroup(gp.WizardStepPosition, selectedOrders);

                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach (var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);

                    events.Send(new AddressStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupSelectionDTO> rq)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();


            //orderLog.InfoAsync(rq.OrderID, g["Quantities Validation Completed"]);
            foreach (var gp in rq)
            {
                var selectedOrders = gp.Orders.Select(s => s);

                foreach (var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Shipping Address Validation Completed"]);

                }
            }
        }

        [Serializable]
        private class InvalidDeliveryAddressException : Exception
        {
            public InvalidDeliveryAddressException()
            {
            }

            public InvalidDeliveryAddressException(string message) : base(message)
            {
            }

            public InvalidDeliveryAddressException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected InvalidDeliveryAddressException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }


   
}
