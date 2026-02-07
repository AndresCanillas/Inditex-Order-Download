using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.Plugins;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.Orders.DTO;
using WebLink.Contracts.Services;

namespace WebLink.Controllers
{
    public class LabellingController : Controller
    {
        private IFactory factory;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private IArticleRepository articleRepo;
        private IOrderRepository orderRepo;
        private IProjectRepository projectRepo;
        private IOrderUtilService orderUtilService;
        private IPluginManager<IWizardCompositionPlugin> pluginManager;
        private IPDFZaraExtractorService pDFZaraExtractorService;
        private readonly IAppConfig config;

        public LabellingController(
            IFactory factory,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IArticleRepository articleRepo,
            IOrderRepository orderRepo,
            IProjectRepository projectRepo,
            IOrderUtilService orderUtilService,
            IPDFZaraExtractorService pDFZaraExtractorService,
            IPluginManager<IWizardCompositionPlugin> pluginManager,
            IAppConfig config
            )
        {
            this.factory = factory;
            this.g = g;
            this.log = log;
            this.events = events;
            this.articleRepo = articleRepo;
            this.orderRepo = orderRepo;
            this.projectRepo = projectRepo;
            this.orderUtilService = orderUtilService;
            this.pluginManager = pluginManager;
            this.pDFZaraExtractorService = pDFZaraExtractorService;
            this.config = config;
        }

        [HttpPost, Route("/order/validate/getorderedlabelsgrouped")]
        public async Task<OperationResult> GetOrderedLabelsGrouped([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {

                var rq = await pDFZaraExtractorService
                                    .GetCompositionDefinitionFromCompositionJSON(selection.FirstOrDefault().ProjectID,
                                                                                selection.FirstOrDefault().Orders);


                var allowedDetails = orderUtilService.CurrentOrderedLablesGroupBySelectionV2(selection);

                return new OperationResult(true, null, allowedDetails);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        public OperationResult GetCompositionCatalogs([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                var data = orderUtilService.GetCompositionCatalogBySelection(selection);
                return new OperationResult(true, null, data);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        // can return compo articles, handtags, stickers, piggybacks
        [HttpPost, Route("/articlesbylabeltype")]
        public OperationResult GetArticlesByLabelType([FromBody] RequestLabelType rq)
        {
            try
            {
                var result = articleRepo.GetArticleCanIncludeCompo(rq);
                return new OperationResult(true, null, result);
            }
            catch(Exception ex)
            {
                log.LogException("LabellingController::GetArticlesByLabelType", ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/order/validate/composition/savestate")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult SaveState([FromBody] IList<CompositionDefinition> rq)
        {
            try
            {
                _SaveState(rq);

                return new OperationResult(true, g["State Saved"], rq);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }

        [HttpGet, Route("order/validate/composition/getcompopreview/{projectId}/{orderId}/{orderGroupId}/{id}/{isLoad}/{fillingWeightId}/{exceptionsLocation}/{articleID}/{fillingWeightText?}")]
        public OperationResult CompoPreview(int projectId, int orderId, int orderGroupId, int id, bool isLoad, int fillingWeightId,int exceptionsLocation = 0, int articleID = 0, string fillingWeightText="" )
        {
            try
            {
                List<PluginCompoPreviewData> compo = GetCompoPreview(projectId, orderId, orderGroupId, id, isLoad, fillingWeightId, fillingWeightText, exceptionsLocation,articleID: articleID);
                return new OperationResult(true, g["State Saved"], data: compo);
            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }

        }


        [HttpPost, Route("order/validate/composition/buildcompopreview")]
        public OperationResult CompoPreview([FromBody] CompoPreviewRequest request)
        {
            try
            {
                List<PluginCompoPreviewData> compo = GetCompoPreview(
                    request.ProjectId,
                    request.OrderId,
                    request.OrderGroupId,
                    request.Id,
                    request.IsLoad,
                    request.FillingWeightId,
                    request.FillingWeightText,
                    request.ExceptionsLocation,
                    request.ExceptionsComposition, 
                    request.UsesFreeExceptionComposition,
                    request.FiberConcatenation, 
                    request.ArticleID

                );
                return new OperationResult(true, g["State Saved"], data: compo);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }


        private List<PluginCompoPreviewData> GetCompoPreview(int projectId, int orderId, int orderGroupId, int id, bool isLoad, int fillingWeightId, string fillingWeightText = null, int exceptionsLocation = 0, List<ExceptionComposition> exceptionsComposition = null, bool UsesFreeExceptionComposition  = false, FiberConcatenation fiberConcatenation = null, int articleID = 0)
        {
            var opd = new OrderPluginData()
            {
                ProjectID = projectId,
                OrderGroupID = orderGroupId,
                OrderID = orderId,
                FillingWeightId = fillingWeightId,
                FillingWeightText = fillingWeightText, 
                ExceptionsLocation = exceptionsLocation, 
                ExceptionsComposition = exceptionsComposition, 
                UsesFreeExceptionComposition = UsesFreeExceptionComposition,
                FiberConcatenation = fiberConcatenation, 
                ArticleID = articleID   
            };

            ////orderData.Add(opd);

            var project = projectRepo.GetByID(projectId);
            var compo = new List<PluginCompoPreviewData>();

            if(!string.IsNullOrEmpty(project.WizardCompositionPlugin))
            {
                using(var plugin = pluginManager.GetInstanceByName(project.WizardCompositionPlugin))
                {

                    compo = plugin.GenerateCompoPreviewData(new List<OrderPluginData>() { opd }, id, isLoad);
                }
            }

            return compo;
        }

        [HttpPost, Route("/order/validate/composition/savecompopreview")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult SaveCompoPreview([FromBody] OrderCompoPreviewDTO compoPreviewDTO)
        {
            try
            {
                var opd = new OrderPluginData()
                {
                    ProjectID = compoPreviewDTO.ProjectID,
                    OrderGroupID = compoPreviewDTO.OrderGroupID,
                    OrderID = compoPreviewDTO.OrderID,
                    ExceptionsLocation = compoPreviewDTO.ExceptionsLocation, 
                    ExceptionsComposition = compoPreviewDTO.ExceptionsComposition, 
                    UsesFreeExceptionComposition = compoPreviewDTO.UsesFreeExceptionComposition, 
                    FiberConcatenation = compoPreviewDTO.FiberConcatenation,
                };

                var project = projectRepo.GetByID(compoPreviewDTO.ProjectID);
                var compo = new List<PluginCompoPreviewData>();

                if(!string.IsNullOrEmpty(project.WizardCompositionPlugin))
                {
                    using(var plugin = pluginManager.GetInstanceByName(project.WizardCompositionPlugin))
                    {
                        var data = new PluginCompoPreviewInputData()
                        {
                            labelLines = compoPreviewDTO.MaxLines,
                            ID = compoPreviewDTO.Id,
                            compoArray = compoPreviewDTO.Compo,
                            percentArray = compoPreviewDTO.Percent,
                            leatherArray = compoPreviewDTO.Leather,
                            additionalArray = compoPreviewDTO.Additionals,
                            AdditionalsCompress = compoPreviewDTO.AdditionalsCompress,
                            FiberCompress = compoPreviewDTO.FiberCompress, 
                            FibersInSpecificLang = compoPreviewDTO.FibersInSpecificLang,
                            JustifyCompo = compoPreviewDTO.JustifyCompo,
                            JustifyAdditional = compoPreviewDTO.JustifyAdditional, 


                        };
                        plugin.SaveCompoPreview(opd, data);
                    }
                }

                return new OperationResult() { Message = "", Success = true };
            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }



        [HttpPost, Route("/order/validate/composition/solveaddlabels")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult Solve([FromBody] IList<OrderGroupSelectionCompositionDTO> rq)
        {
            // TODO: wizard step position is not defined in request parameter
            try
            {
                var compositions = new List<CompositionDefinition>();

                foreach(var sel in rq)
                {
                    if(sel != null)
                        compositions.AddRange(sel.Compositions);
                }

                _SaveState(compositions);
                _UpdateWizardProgress(rq);
                return new OperationResult(true, null, rq);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }
        [HttpPost, Route("/order/validate/composition/checkinitialcomposition")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public async Task<OperationResult>CheckInitialComposition([FromBody] OrderInitialCompositionDTO initialComposition)
        {
            try
            {
                var rq = await pDFZaraExtractorService
                                    .GetCompositionDefinitionFromCompositionJSON(initialComposition.ProjectID, 
                                                                                initialComposition.OrderID);
                return new OperationResult(true, null, rq);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }
        [HttpGet, Route("/order/validate/composition/getcompositiondetailbyorder/{orderID}")]
        public  OperationResult GetCompositionDetailsByOrder(int orderID)
        {
            try
            {

                CompositionDefinition compo = orderUtilService.GetCompositionDetailsForOrder(orderID);
                return new OperationResult(true, null, compo);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        [HttpPost, Route("/order/validate/savecompodefinition")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueCountLimit = 1000000, ValueLengthLimit = 1000000)]
        public OperationResult SaveDefinedComposition(CompositionDefinition rq)
        {
            try
            {
                CheckCareInstructions(rq);

                CompositionDefinition compo = orderUtilService.SaveCompositionDefinition(rq);
                return new OperationResult(true, null, compo);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        private void CheckCareInstructions(CompositionDefinition rq)
        {
            var debuggerLog = log.GetSection("Ci_Sorted_Checker");
            var categories = rq.CareInstructions.Select(s => s.Category.First()).ToList();
            debuggerLog.LogMessage($"SAVE - [{string.Join("-", categories).PadRight(20)}] - OrderID: [{rq.OrderID.ToString("D7")}] OrderDataID: [{rq.OrderDataID.ToString("D7")}]  TargetArticle: []");

            #region BROWNIE HARCODE
            // TODO: Harcode Validation for Brownie
            var order = orderRepo.GetByID(rq.OrderID);

            if(order.CompanyID == config.GetValue("CustomSettings.Brownie.CompanyID", 24)) // brownie 
                return;

            #endregion BROWNIE HARCODE

            if(string.Join("-", categories).StartsWith("W-B-D-I-D") == false)
                throw new Exception("Disorganized washing instructions");
        }



        private void _SaveState(IList<CompositionDefinition> rq)
        {
            foreach(var compoDef in rq)
            {
                CheckCareInstructions(compoDef);

                if(!string.IsNullOrEmpty(compoDef.ArticleCode) && compoDef.ArticleID > 0)
                    orderUtilService.SaveCompositionDefinition(compoDef);

                CloneCompoPreview(compoDef);
            }
            //var orderData = new List<OrderPluginData>();

            foreach(var s in rq.GroupBy(g => new { g.OrderGroupID, g.ProjectID, g.OrderID }))
            {
                var opd = new OrderPluginData()
                {
                    ProjectID = s.Key.ProjectID,
                    OrderGroupID = s.Key.OrderGroupID,
                    OrderID = s.Key.OrderID,
                };

                //orderData.Add(opd);
                //if(rq.Any(r => r.GenerateCompoText == "false")) return;

                var project = projectRepo.GetByID(s.Key.ProjectID);

                if(!string.IsNullOrEmpty(project.WizardCompositionPlugin))
                {
                    using(var plugin = pluginManager.GetInstanceByName(project.WizardCompositionPlugin))
                    {
                        plugin.GenerateCompositionText(new List<OrderPluginData>() { opd });
                    }
                }
            }

            //using (var plugin = pluginManager.GetInstanceByName("Smartdots - Generic Compo Plugin"))
            //{
            //    plugin.GenerateCompositionText(orderData);
            //}

        }

        private void CloneCompoPreview(CompositionDefinition compoDef)
        {
            if(!compoDef.ClonedFrom.HasValue)
            {
                return;
            }
            var project = projectRepo.GetByID(compoDef.ProjectID);
            if(string.IsNullOrEmpty(project.WizardCompositionPlugin))
            {
                return;
            }
            var opd = new OrderPluginData()
            {
                ProjectID = compoDef.ProjectID,
                OrderGroupID = compoDef.OrderGroupID,
                OrderID = compoDef.OrderID
            };
            using(var plugin = pluginManager.GetInstanceByName(project.WizardCompositionPlugin))
            {
                var compo = plugin.GenerateCompoPreviewData(new List<OrderPluginData>() { opd }, compoDef.ClonedFrom.Value, true);
                if(compo != null)
                {
                    plugin.CloneCompoPreview(opd, compoDef.ClonedFrom.Value, compo.FirstOrDefault().CompoData, new List<int>() { compoDef.ID });
                }
            }


        }

        private void _UpdateWizardProgress(IList<OrderGroupSelectionCompositionDTO> rq)
        {
            var wzStpRepo = factory.GetInstance<IWizardStepRepository>();

            var wzRepo = factory.GetInstance<IWizardRepository>();

            var orderRepo = factory.GetInstance<IOrderRepository>();

            foreach(var sel in rq)
            {
                var selectedOrders = sel.Orders.ToList();
                wzStpRepo.MarkAsCompleteByGroup(sel.WizardStepPosition, selectedOrders);
                wzRepo.UpdateProgressByGroup(selectedOrders);

                foreach(var orderID in selectedOrders)
                {
                    var info = orderRepo.GetProjectInfo(orderID);
                    events.Send(new DefinedCompositionCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
                }
            }

            // TODO: compo order wizard is not exist
        }
    }

    //type: type, 
    //        exception : exception? exception.English : "",
    //        section : section? section.English : "",
    //        fiber : fiber? fiber.English : "",
    //exceptionID : exceptionID,
    //        sectionID : sectionID,
    //        fiberID : fiberID



    public class CompoPreviewRequest
    {
        public int ProjectId { get; set; }
        public int OrderId { get; set; }
        public int OrderGroupId { get; set; }
        public int Id { get; set; }
        public bool IsLoad { get; set; }
        public int FillingWeightId { get; set; }
        public int ExceptionsLocation { get; set; } = 0;
        public string FillingWeightText { get; set; } = "";
        public List<ExceptionComposition> ExceptionsComposition    { get; set; } 
        public bool UsesFreeExceptionComposition { get; set; } = false;  
        public FiberConcatenation FiberConcatenation { get; set; } // TODO: check if this is needed
        public int ArticleID { get; set; } = 0; 
    }


    public class DetailGroupRequest
    {
        public List<OrderGroupSelectionDTO> selection { get; set; }
        public List<string> GroupBy { get; set; }
        public List<string> SortBy { get; set; }

        public DetailGroupRequest()
        {
            // field must be exist in VariableData Catalog - and transfered to OrderDetailDTO
            GroupBy = new List<string>() { "Color" };

            SortBy = new List<string>();

            selection = new List<OrderGroupSelectionDTO>();
        }
    }
}
