using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Sage;
using WebLink.Services.Sage;

namespace PrintCentral.Controllers
{
    public class SageController : Controller
    {
        private ILocalizationService g;
        private ILogService log;
        private ISageClientService sageClient;
        private ISageSyncService sageSync;
        private IOrderRepository orderRepo;

        public SageController(
            ILocalizationService g,
            ILogService log,
            IOrderRepository orderRepo,
            ISageClientService sageClient,
            ISageSyncService sageSync
            )
        {
            this.g = g;
            this.log = log;
            this.orderRepo = orderRepo;
            this.sageClient = sageClient;
            this.sageSync = sageSync;
        }


        [HttpGet, Route("/ws/sage/getarticle/{id}/{reference}")]
        public async Task<OperationResult> GetArticle(int id, string reference)
        {
            try
            {
                var item = await sageClient.GetItemDetail(reference);

                return new OperationResult(true, g["Sage response OK"], item);
            }catch (Exception _ex)
            {
                log.LogException("Error to get Article info from ERPService", _ex);
                return new OperationResult(false, "Error to get info from ERPService", null);
            }
        }

        [HttpGet, Route("/ws/sage/getarticleimage/{id}/{reference}")]
        public async Task<IActionResult> GetArticleImage(int id, string reference)
        {

            try
            {
                var item = await sageClient.GetItemDetail(reference);

                return new FileContentResult(item.Image, item.MimeType);
            }
            catch (Exception _ex)
            {
                
                log.LogException(_ex);
                return File("~/images/no_preview.png", "image/png", "no_preview.png");
            }
        }

        [HttpGet, Route("/ws/sage/syncarticle/{id}/{reference}")]
        public async Task<OperationResult> SyncArticle(int id, string reference)
        {
            try
            {
                var articleUpdated = await sageSync.SyncItemAsync(id, reference);

                return new OperationResult(true, g["Article was sycn"], articleUpdated);
            }
            catch (Exception _ex)
            {
                log.LogException("Error to sync article with ERP", _ex);
                return new OperationResult(false, "Error to sync Article with ERP", null);
            }
        }

        [HttpGet, Route("/ws/sage/syncartifact/{id}/{reference}")]
        public async Task<OperationResult> SyncArtifact(int id, string reference)
        {
            try
            {
                 var artifactUpdated = await sageSync.SyncArtifact(id, reference);

                return new OperationResult(true, g["Artifact was sycn"], artifactUpdated);
            }
            catch (Exception _ex)
            {
                log.LogException("Error to sync article with ERP", _ex);
                return new OperationResult(false, "Error to sync Article with ERP", null);
            }
        }

        [HttpGet, Route("/ws/sage/getcompany/{id}/{reference}")]
        public async Task<OperationResult> GetCompany(int id, string reference)
        {
            try
            {
                var btc = await sageClient.GetCustomerDetail(reference);

                return new OperationResult(true, g["Sage response OK"], btc);
            }
            catch (Exception _ex)
            {
                log.LogException("Error to get Company info from ERPService", _ex);
                return new OperationResult(false, "Error to get info from ERPService", null);
            }
        }

        [HttpGet, Route("/ws/sage/synccompany/{id}/{reference}")]
        public async Task<OperationResult> SyncCompany(int id, string reference)
        {
            try
            {
                var companyUpdated = await sageSync.SyncCompanyAsync(id, reference);
                return new OperationResult(true, g["Sage response OK"], companyUpdated);
            }
            catch (Exception _ex)
            {
                log.LogException("Error to sync company with ERP", _ex);
                return new OperationResult(false, g["Please Try Again"] , _ex.Message);
            }
        }

        [HttpPost, Route("ws/sage/queryitems")]
        public async Task<OperationResult> GetAllItems([FromBody] GetAllArticlesRq rq) 
        {
            try
            {
                var list = await sageClient.GetAllItemsByTerm(rq.GetConditions, rq.ListSize);

                return new OperationResult(true, "ok", list);
            }catch(Exception _ex)
            {
                log.LogException("Error to execute sage item query", _ex);
                return new OperationResult() { Success = false, Message = g["Error to get items"] };
            }
        }

        [HttpPost, Route("ws/sage/ImportItems")]
        public OperationResult SyncAllItems([FromBody] SyncArticlesRq rq)
        {
            try
            {
                // buscar si ya existe o crear
                // esta logica deberia estar en un servicio
                string identifier = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                // dont await https://stackoverflow.com/a/15523793
                var _ = sageSync.ImportItemsAsync(rq.References, rq.ProjectID, identifier, rq.Family, rq.Brand);

                return new OperationResult() { Success = true, Data = identifier };

            }catch(Exception _ex)
            {
                log.LogException($"Error to sync items selected in ProjectID: {rq.ProjectID}", _ex);
                return new OperationResult() { Success = false, Message = g["Error to sync items"] };
            }
        }

        [HttpGet, Route("ws/sage/checkiforderexist")]
        public async Task<OperationResult> CheckIfOrderExist(int orderID)
        {
            bool orderExist = false;
            var result = new OperationResult() { Success = true};

            try
            {
                var orderInfo = orderRepo.GetByID(orderID);

                orderExist = await sageClient.CheckIfOrderExistAsync(orderInfo.SageReference, orderID);

            }
            catch(Exception _ex)
            {
                log.LogException("Cannot Check if order exist in SAGE", _ex);
                result.Success = false;
                result.Message = g["Try again, Error to ckeck order"];
            }

            result.Data = orderExist;

            return result;

        }


    }

    public class GetAllArticlesRq
    {
        public int ListSize { get; set; }
        public string SearchKey { get; set; }
        public string Status { get; set; }
        public string ItemRef { get; set; }
        public string Family { get; set; }

        public GetAllArticlesRq()
        {
            ListSize = 1;
        }

        public IEnumerable<IWsKey> GetConditions { 
            get {

                var ret = new List<WsKey>();

                if (!string.IsNullOrEmpty(SearchKey))
                {
                    ret.Add(new WsKey() { Key = "SEAKEY", Value = SearchKey });
                }

                if (!string.IsNullOrEmpty(Status))
                {
                    ret.Add(new WsKey() { Key = "ITMSTA", Value = Status });
                }

                if (!string.IsNullOrEmpty(ItemRef))
                {
                    ret.Add(new WsKey() { Key = "ITMREF", Value = ItemRef });
                }

                return ret;
            }
        }
    }

    public class SyncArticlesRq
    {
        public List<string> References { get; set; }
        public int ProjectID { get; set; }
        public string Family { get; set; }
        public string Brand { get; set; }


    }

}