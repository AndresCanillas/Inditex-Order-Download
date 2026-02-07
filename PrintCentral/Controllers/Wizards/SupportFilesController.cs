using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers.Wizards
{
    public class SupportFilesController : Controller
    {
        private IFactory factory;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;
        private IEventQueue events;
        private IOrderGroupRepository repo;

        public SupportFilesController(
            IFactory factory,
            IUserData userData,
            ILocalizationService g,
            ILogService log,
            IEventQueue events,
            IOrderGroupRepository repo
            )
        {
            this.factory = factory;
            this.userData = userData;
            this.g = g;
            this.log = log;
            this.events = events;
            this.repo = repo;
        }

        [HttpPost, Route("/supportfiles/getordersdetails")]
        public OperationResult GetOrderArticlesDetailed([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                var repo = factory.GetInstance<IOrderRepository>();

                // TODO: articles that have validation enable - Wizard RelationShip is not null - no Extras
                var filter = new OrderArticlesFilter() { ArticleType = ArticleTypeFilter.Label, ActiveFilter = OrderActiveFilter.Active, Source = OrderSourceFilter.NotFromValidation };
                var result = repo.GetArticleDetailSelection(selection, filter);
                return new OperationResult(true, null, result);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/supportfiles/savesupportfiles")]
        [DisableRequestSizeLimit]
        public OperationResult SaveState([FromForm]List<OrderGroupQuantitiesDTO> rq)
        {
            try
            {
                //_SaveState(rq);

                return new OperationResult(true, g["State Saved"]);

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Can't save state, try again"]);
            }
        }

        [HttpPost, Route("/supportfiles/validate/solvesupportfiles")]
        [DisableRequestSizeLimit]
        public OperationResult Solve([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                _UpdateWizardProgress(selection);

                //_RegisterOrderLogs(selection);

                return new OperationResult(true, null, selection);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }


        [HttpPost, Route("/supportfiles/validate/createsupportfileslog")]
        [DisableRequestSizeLimit]
        public OperationResult CreateSupportFilesLog([FromBody] List<OrderGroupSelectionDTO> selection)
        {
            try
            {
                _RegisterOrderLogs(selection);

                return new OperationResult(true, null, selection);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Something is wrong. Please, try again"]);
            }
        }

        [HttpPost, Route("/supportfiles/upload/{ordergroupid}/{category}")]
        [RequestSizeLimit(10000000000)]
        public IActionResult upload(int ordergroupid,string category)
        {
            try
            {
                if (Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    if (".png,.jpg".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                        return Content($"{{\"success\":false, \"message\":\"{g["Can only accept .png and .jpg files"]}\"}}");
                    using (var src = file.OpenReadStream())
                    {
                        repo.SetOrderGroupAttachment(ordergroupid, category, file.FileName, src);


                    }
                    return Content($"{{\"success\":true, \"message\":\"\", \"FileID\":{ordergroupid}}}");
                }
                else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                //return new OperationResult(false, g["Something is wrong. Please, try again"]);
                return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading file."]}\"}}");
            }
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

                    events.Send(new SupportFilesStepCompletedEvent(orderID, info.OrderNumber, info.CompanyID, info.BrandID, info.ProjectID));
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
                    orderLog.InfoAsync(orderID, "Support Files Validation Completed");

                }
            }
        }

    }
}