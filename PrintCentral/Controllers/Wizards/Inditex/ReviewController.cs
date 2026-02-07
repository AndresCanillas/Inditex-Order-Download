using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers.Wizards.Inditex
{
    public class ReviewController : Controller
    {
        private IFactory factory;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private IOrderRepository orderRepo;
        private readonly IProviderRepository providerRepo;

        public ReviewController(
            IFactory factory,
            IUserData userData,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IOrderRepository orderRepo,
            IProviderRepository providerRepo)
        {
            this.factory = factory;
            this.userData = userData;
            this.g = g;
            this.log = log;
            this.events = events;
            this.orderRepo = orderRepo;
            this.providerRepo = providerRepo;
        }


        [HttpPost, Route("/inditex/order/validate/solvereview")]
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
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }


        [HttpPost, Route("/inditex/order/validate/getordersdetailsforreview")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                //TODO: meter verificacion de usuario logueado

                //var repo = factory.GetInstance<IOrderRepository>();
                var cfgRepository = factory.GetInstance<IConfigurationRepository>();

                var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Label, ActiveFilter = OrderActiveFilter.NoRejected, OrderStatus = OrderStatusFilter.InFlow  };
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
                var itemsPendingApprove = orderRepo.GetItemsExtrasDetailSelection(cloneSelection, filterItems); // this query only looking by OrderGroupID


                foreach(var o in itemsPendingApprove)
                {
                     var sel=  result.Where(x => x.OrderGroupID == o.OrderGroupID).First();

                    sel.Details.AddRange(o.Details);

                }

                

                

                #endregion Adding ExtraItems to the review images


                foreach (var sel in result)
                {

                    if (sel.Orders.Length > 0) {
                        var orderId = sel.Orders[0];
                        var order = orderRepo.GetByID(orderId);
                        if (order.ProductionType == ProductionType.IDTLocation)
                        {
                            var provider = providerRepo.GetByID(order.ProviderRecordID.Value);
                            sel.DueDate = cfgRepository.GetOrderDueDate(order.CompanyID,provider.ClientReference, order.ProjectID);
                        }
                    }

                    var groups = sel.Details.GroupBy(g => g.ArticleCode).ToList();

                    sel.Details = new List<OrderDetailDTO>();

                    groups.ForEach(e => sel.Details.Add(e.First()));

                    sel.Orders = sel.Details.Select(d => d.OrderID).Distinct().ToArray();

                }

                return new OperationResult(true, g["Articles Found"], result);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
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
            foreach (var grp in result)
            {
                foreach (var article in grp.Details)
                {
                    if (article.IsBilled) { continue; }
                    var orderInfo = repo.GetProjectInfo(article.OrderID);

                    repo.ChangeStatus(article.OrderID, OrderStatus.Validated);
                    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));

                    //if(userData.IsIDT && !CheckCanContinue(orderInfo.ProjectID))
                    //{
                    //    actionsService.StopOrder(orderInfo.OrderID);
                    //}else
                    //{
                    //    repo.ChangeStatus(article.OrderID, OrderStatus.Validated);
                    //    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                    //}
                }
            }


            foreach(var sel in selection)
            {
                sel.Orders.ToList().ForEach(e =>
                {
                    var orderInfo = repo.GetProjectInfo(e);

                    repo.ChangeStatus(e, OrderStatus.Validated);
                    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));

                    //if(userData.IsIDT  && !CheckCanContinue(orderInfo.ProjectID))
                    //{
                    //    actionsService.StopOrder(orderInfo.OrderID);    
                    //}else
                    //{
                    //    repo.ChangeStatus(e, OrderStatus.Validated);
                    //    events.Send(new OrderValidatedEvent(orderInfo.OrderGroupID, orderInfo.OrderID, orderInfo.OrderNumber, orderInfo.CompanyID, orderInfo.BrandID, orderInfo.ProjectID));
                    //}
                        
                });
            }

            

        }

        private bool CheckCanContinue(int projectID)
        {
            List<string> authorizedUsers = new List<string>
                     { 
                        "toni.esteve",
                        "roger.civera@indetgroup.com",
                        "maria.longueira@indetgroup.com",
                        "cristina.oliet",
                        "alejandro.corral",
                        "xavier.cubero",
                        "sebastian.canal"
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

                if(projects.Any(p => projectID == p))
                {
                    if(authorizedUsers.Contains(userData.UserName))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
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

                    events.Send(new QuantitiesStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

        }

        private void _RegisterOrderLogs(List<OrderGroupSelectionDTO> selection)
        {
            var orderLog = factory.GetInstance<IOrderLogService>();


            //orderLog.InfoAsync(rq.OrderID, g["Quantities Validation Completed"]);
            foreach (var gp in selection)
            {
                var selectedOrders = gp.Orders.Select(s => s);

                foreach (var orderID in selectedOrders)
                {
                    orderLog.InfoAsync(orderID, g["Article Validation Completed"]);

                }
            }
        }
    }
}