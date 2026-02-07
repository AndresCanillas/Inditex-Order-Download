using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services.Wizards;

namespace WebLink.Controllers
{
    public class QuantitiesWizardController : Controller
    {
        private IFactory factory;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private readonly ICatalogRepository catalogRepo;
        private readonly IDBConnectionManager connManager;
        private readonly IOrderRepository orderRepo;
        private readonly IProjectRepository projectRepo;
        private readonly IItemAssigmentService itemAssigmentService;
        private readonly IArticleRepository articleRepo;

        public QuantitiesWizardController(
            IFactory factory,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IOrderRepository orderRepo,
            IProjectRepository projectRepository,
            IItemAssigmentService itemAssigmentService,
            IArticleRepository articleRepo,
            ICatalogRepository catalogRepo,
            IDBConnectionManager connManager
            )
        {
            this.factory = factory;
            this.g = g;
            this.log = log;
            this.events = events;
            this.orderRepo = orderRepo;
            this.projectRepo = projectRepository;
            this.itemAssigmentService = itemAssigmentService;
            this.articleRepo = articleRepo;
            this.catalogRepo = catalogRepo;
            this.connManager = connManager;
        }

        [HttpPost, Route("/order/validate/getordersdetails")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            var addProductDetail = false;

            var productFields = new List<string>();

            try
            {
                if (selection.Count > 0)
                {
                    var project = projectRepo.GetByID(selection.First().ProjectID);

                    if (project.AllowUpdateMadeIn != 0)
                    {
                        addProductDetail = true;

                        productFields.Add("MadeIn");

                        productFields.Add("ReceivedMadeIn");
                    }
                }

                var filter = new OrderArticlesFilter() { 
                    ArticleType = ArticleTypeFilter.Label, 
                    ActiveFilter = OrderActiveFilter.Active, 
                    Source = OrderSourceFilter.NotSet 
                };

                var result = orderRepo.GetArticleDetailSelection(selection, filter, addProductDetail, productFields);

                return new OperationResult(true, null, result);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/order/validate/savequantities")]
        [DisableRequestSizeLimit]
        //[RequestFormSizeLimit(valueCountLimit: 4000)]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult SaveState([FromForm]List<OrderGroupQuantitiesDTO> rq)
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


        [HttpPost, Route("/order/validate/solvequantities")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult Solve([FromForm]List<OrderGroupQuantitiesDTO> rq)
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

        private void _SaveState(List<OrderGroupQuantitiesDTO> rq)
        {
            var printerJobRepo = factory.GetInstance<IPrinterJobRepository>();
            
            // update quantities in printjobs and companyorders / partial orders
            printerJobRepo.UpdateQuantiesByGroup(rq);

            // 
            _UpdateVariableData(rq);

        }

        private void _UpdateWizardProgress(List<OrderGroupQuantitiesDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            //wzdRepo.MarkAsComplete(rq.WizardStepID);

            //repo.UpdateProgress(rq.WizardID);

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


            //orderLog.InfoAsync(rq.OrderID, g["Quantities Validation Completed"]);
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

        private void _UpdateVariableData(List<OrderGroupQuantitiesDTO> rq)
        {

            int currentProjectID = -1;

            string currentMadeIn = string.Empty;

            IProject project = null;

            ICatalog madeInCatalog = null;

            string fullMadeInText = string.Empty;

            Parallel.ForEach(rq, gp => {

                if (currentProjectID != gp.ProjectID)
                {
                    project = projectRepo.GetByID(gp.ProjectID);// all groups in rq must belong to the same project

                    currentProjectID = gp.ProjectID;
                }

                if (currentMadeIn != gp.MadeIn && int.TryParse(gp.MadeIn, out int madeInID))
                {
                    madeInCatalog = catalogRepo.GetByName(project.ID, Catalog.BRAND_MADEIN_CATALOG);
                    
                    using (var dynamicDB = connManager.CreateDynamicDB())
                    {
                        var row = dynamicDB.SelectOne(madeInCatalog.CatalogID, madeInID);

                        fullMadeInText = row.GetValue("AllLangs").ToString();
                    }

                }


                var commonFields = new List<ProductField>();

                if(project.AllowUpdateMadeIn == MadeInEnable.YES)
                {

                    var madeInField = new ProductField()
                    {
                        Name = "MadeIn", // Standard Field name
                        Value = gp.MadeIn
                    };

                    var fullMadeIn = new ProductField()
                    {
                        Name = "FullMadeIn", // Standard Field name
                        Value = fullMadeInText
                    };

                    commonFields.Add(madeInField);
                    commonFields.Add(fullMadeIn);
                }

                //get order article
                var orders = gp.Quantities.Select(s => s.OrderID).Distinct();

                foreach (var order in orders)
                {
                    var articles = articleRepo.GetByOrder(order);

                    foreach (var article in articles)
                    {
                        itemAssigmentService.SetVariableData(
                        commonFields: commonFields,
                        selectedarticle: new CustomArticle { ArticleID = article.ID },
                        order: new Order { ID = order, ProjectID = gp.ProjectID }
                        );

                    }
                }

            });
        }
    }

}
