using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;
using WebLink.Contracts.Workflows;

namespace WebLink.Controllers
{
    [Authorize]
    public class PacksController : Controller
    {
        private IPackRepository repo;
        private IUserData userData;
        private ILocalizationService g;
        private ILogService log;
        private IExpandPackService expandPackService;

        public PacksController(
            IPackRepository repo,
            IUserData userData,
            ILocalizationService g,
            ILogService log,
            IExpandPackService expandPackService)
        {
            this.log = log;
            this.repo = repo;
            this.userData = userData;
            this.g = g;
            this.expandPackService = expandPackService;
        }

        [HttpPost, Route("/packs/insert")]
        public OperationResult Insert([FromBody] Pack data)
        {
            try
            {
                if (!userData.Admin_Packs_CanAdd)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Pack Created!"], repo.Insert(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/update")]
        public OperationResult Update([FromBody] Pack data)
        {
            try
            {
                if (!userData.Admin_Packs_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Pack saved!"], repo.Update(data));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/delete/{id}")]
        public OperationResult Delete(int id)
        {
            try
            {
                if (!userData.Admin_Packs_CanDelete)
                    return OperationResult.Forbid;
                repo.Delete(id);
                return new OperationResult(true, g["Pack Deleted!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/rename/{id}/{name}")]
        public OperationResult Rename(int id, string name)
        {
            try
            {
                if (!userData.Admin_Articles_CanRename)
                    return OperationResult.Forbid;
                repo.Rename(id, name);
                return new OperationResult(true, g["Pack Renamed!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                if (ex.IsNameIndexException())
                    return new OperationResult(false, g["There is already an item with that name."]);
                return new OperationResult(false, g["Unexpected error while renaming Pack."]);
            }
        }

        [HttpGet, Route("/packs/getbyid/{id}")]
        public IPack GetByID(int id)
        {
            try
            {
                return repo.GetByID(id);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/packs/getlist")]
        public List<IPack> GetList()
        {
            try
            {
                return repo.GetList();
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/packs/getbyprojectid/{projectid}")]
        public List<IPack> GetByProjectID(int projectid)
        {
            try
            {
                return repo.GetByProjectID(projectid);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/packs/addarticletopack/{packid}/{articleid}")]
        public OperationResult AddArticleToPack(int packid, int articleid)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Article added to pack!"], repo.AddArticleToPack(packid, articleid));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/addarticlebydata")]
        public OperationResult AddArticleByData([FromBody] PackArticleDTO data)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Article added to pack!"], repo.AddArticleByData(data.ID, data.PackID, data.ProjectID, data.Field, data.Mapping, data.AllowEmptyValues));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/addarticlebyplugin")]
        public OperationResult AddArticleByPlugin([FromBody] PackArticleDTO data)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Article added to pack!"], repo.AddArticleByPlugin(data.PackID, data.ProjectID, data.PluginName));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/packs/getpackarticle/{packid}/{articleid}")]
        public OperationResult GetPackArticle(int packid, int articleid)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Success!"], repo.GetPackArticle(packid, articleid));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/packs/getpackarticlebyid/{id}")]
        public OperationResult GetPackArticleById(int id)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Success!"], repo.GetPackArticleById(id));
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/removearticlefrompack/{packid}/{articleid}/{id}")]
        public OperationResult RemoveArticleFromPack(int packid, int articleid, int id)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                repo.RemoveArticleFromPack(packid, articleid, id);
                return new OperationResult(true, g["Article removed from pack!"]);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/updatepackarticle/{packId}/{articleId}/{quantity}/{id}")]
        public OperationResult UpdatePackArticle(int packId, int articleId, int quantity, int id)
        {
            try
            {
                if (!userData.Admin_Articles_CanEdit)
                    return OperationResult.Forbid;
                repo.UpadtePackArticle(packId, articleId, quantity, id);
                return new OperationResult(true, g["Record saved!"]);
            }
            catch (CatalogDataException ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, ex.Column);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/packs/expandpack/{projectID}")]
        public OperationResult ExpandPack([FromBody] OrderInfo order, int projectID, CancellationToken cancellationToken)
        {
            try
            {
                var ListOfOrderInfo = expandPackService.Execute(order, projectID, cancellationToken);
                return new OperationResult(true, g["Expanded order!"], data: ListOfOrderInfo);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not completed."]);
            }
        }
        [HttpPost, Route("/packs/expandpackbyorderid/{orderId}/{projectId}/{packCode}")]
        public OperationResult ExpandPackByOrderId(int orderId, int projectId, string packCode, CancellationToken cancellationToken)
        {
            try
            {
                var orderInfo = expandPackService.GenerateOrderInfo(orderId, projectId, packCode);
                var listOfOrderInfo = expandPackService.Execute(orderInfo, projectId, cancellationToken);
                return new OperationResult(true, g["Expanded order!"], data: listOfOrderInfo);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not completed."]);
            }
        }
    }



    public class PackArticleDTO
    {
        public int ID { get; set; }
        public int PackID { get; set; }
        public int ProjectID { get; set; }
        public string Field { get; set; }
        public string Mapping { get; set; }
        public string PluginName { get; set; }
        public bool AllowEmptyValues { get; set; }
    }
}