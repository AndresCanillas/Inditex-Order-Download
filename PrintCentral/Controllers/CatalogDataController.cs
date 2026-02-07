using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Controllers
{
    [Authorize]
    public class CatalogDataController : Controller
    {
        private IFactory factory;
        private ICatalogDataRepository repo;
        private ILocalizationService g;
        private ILogService log;
        private IAppInfo appInfo;
        private IMappingRepository mappingRepo;
        private IDataImportService importService;
        private IUserData userData;
        private ICatalogLogRepository catalogLogRepository;
        private IOrderRepository orderRepository;

        public CatalogDataController(
            IFactory factory,
            ICatalogDataRepository repo,
            ILocalizationService g,
            ILogService log,
            IAppInfo appInfo,
            IMappingRepository mappingRepo,
            IDataImportService importService,
            IUserData userData,
            IOrderRepository orderRepository,
            ICatalogLogRepository catalogLogRepository)
        {
            this.factory = factory;
            this.repo = repo;
            this.g = g;
            this.log = log;
            this.appInfo = appInfo;
            this.mappingRepo = mappingRepo;
            this.importService = importService;
            this.userData = userData;
            this.orderRepository = orderRepository;
            this.catalogLogRepository = catalogLogRepository;
        }

        [HttpPost, Route("/catalogdata/insert")]
        public OperationResult Insert([FromBody] CatalogData data)
        {
            try
            {
                if(!userData.CanSeeVMenu_AdminBrands)
                    return OperationResult.Forbid;
                return new OperationResult(true, g["Record Created!"], repo.Insert(data.CatalogID, data.Data));
            }
            catch(CatalogDataException ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, ex.Column);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/catalogdata/update")]
        public OperationResult Update([FromBody] CatalogData data)
        {
            try
            {
                if(!userData.CanSeeVMenu_AdminBrands)
                    return OperationResult.Forbid;
                var recordID = GetObjectID(data.Data);
                var oldValue = repo.GetByID(data.CatalogID, recordID);
                var updateResult = repo.Update(data.CatalogID, data.Data);
                CatalogLog catalogLog = new CatalogLog()
                {
                    CatalogID = data.CatalogID,
                    RecordID = recordID,
                    Action = "Update",
                    OldData = oldValue,
                    NewData = data.Data,
                    User = userData.Principal.Identity.Name,
                    Date = DateTime.Now
                };
                catalogLogRepository.Insert(catalogLog);
                return new OperationResult(true, g["Record saved!"], updateResult);
            }
            catch(CatalogDataException ex)
            {
                log.LogException(ex);
                return new OperationResult(false, ex.Message, ex.Column);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        public int GetObjectID(string json)
        {
            var o = JObject.Parse(json);

            if(o["ID"] != null)
            {
                return Convert.ToInt32(o["ID"]);
            }

            return -1;

        }

        [HttpPost, Route("/catalogdata/delete")]
        public OperationResult Delete([FromBody] CatalogRow data)
        {
            try
            {
                if(!userData.CanSeeVMenu_AdminBrands)
                    return OperationResult.Forbid;
                repo.Delete(data.CatalogID, data.RowID, data.LeftCatalogID, data.ParentRowID);
                return new OperationResult(true, g["Record Deleted!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost, Route("/catalogdata/deleteall/{catalogid}")]
        public OperationResult DeleteAll(int catalogid)
        {
            try
            {
                if(!userData.CanSeeVMenu_AdminBrands)
                    return OperationResult.Forbid;
                repo.DeleteAll(catalogid);
                return new OperationResult(true, g["All Catalog data was deleted!"]);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpGet, Route("/catalogdata/getbyid/{catalogid}/{id}")]
        public string GetByID(int catalogid, int id)
        {
            try
            {
                return repo.GetByID(catalogid, id);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/catalogdata/getbycatalog/{catalogid}")]
        public string GetByCatalogID(int catalogid)
        {
            try
            {
                return repo.GetList(catalogid);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpGet, Route("/catalogdata/getcountbycatalog/{catalogid}")]
        public int GetCountByCatalogID(int catalogid)
        {
            try
            {
                var result = repo.GetListCount(catalogid);
                return result;
            }
            catch(Exception)
            {

                throw;
            }
        }



        [HttpGet, Route("/catalogdata/getpagebycatalog/{catalogid}/{pagenumber}/{pagesize}")]

        public string GetPageByCatalogID(int catalogid, int pagenumber, int pagesize)
        {
            try
            {
                return repo.GetListByPage(catalogid, pagenumber, pagesize);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpGet, Route("/catalogdata/searchfirst/{catalogid}/{fieldName}/{search}")]
        public string SearchFirst(int catalogid, string fieldName, string search)
        {
            return repo.SearchFirst(catalogid, fieldName, search);
        }

        [HttpPost, Route("/catalogdata/searchcatalog/{catalogid}")]
        public OperationResult FreeTextSearch(int catalogid, [FromBody] TextSearchFilter[] filter)
        {
            try
            {
                return new OperationResult(true, null, repo.FreeTextSearch(catalogid, filter));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpPost, Route("/catalogdata/searchcatalogcount/{catalogid}")]
        public OperationResult FreeTextSearchCount(int catalogid, [FromBody] TextSearchFilter[] filter)
        {
            try
            {
                return new OperationResult(true, null, repo.GetListCount(catalogid, filter));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpPost, Route("/catalogdata/searchcatalogpaging/{catalogid}/{pagenumber}/{pagesize}")]
        public OperationResult FreeTextSearchPaging(int catalogid, int pagenumber, int pagesize, [FromBody] TextSearchFilter[] filter)
        {
            try
            {
                return new OperationResult(true, null, repo.FreeTextSearch(catalogid, pagenumber, pagesize, filter));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }
        [HttpGet, Route("/catalogdata/getbasedatabyorderid/{projectid}/{orderid}")]
        public OperationResult GetBaseDataFromOrderId(int projectid, string orderid)
        {
            try
            {
                int.TryParse(orderid, out int orderID);
                var order = orderRepository.GetByID(orderID);
                return new OperationResult(true, null, repo.GetBaseDataFromOrderId(projectid, order));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        [HttpPost, Route("/catalogdata/searchsubset/{catalogid}/{id}/{fieldName}")]
        public OperationResult SubsetFreeTextSearch(int catalogid, int id, string fieldName, [FromBody] TextSearchFilter[] filter)
        {
            try
            {
                return new OperationResult(true, null, repo.SubsetFreeTextSearch(catalogid, id, fieldName, filter));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }

        /*
         OrderDetail_XXX
         {
    "ID": 21,
    "ArticleCode": "000000",
    "PackCode": null,
    "Quantity": 201,
    "Product": 21,
    "_Product_DISP": "32351"
  },
    */
        [HttpGet, Route("/catalogdata/getsubset/{catalogid}/{id}/{fieldName}")]
        public string GetSubset(int catalogid, int id, string fieldName)
        {
            try
            {
                return repo.GetSubset(catalogid, id, fieldName);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpGet, Route("/catalogdata/getfullsubset/{catalogid}/{fieldName}")]
        public string GetFullSubset(int catalogid, string fieldName)
        {
            try
            {
                return repo.GetFullSubset(catalogid, fieldName);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("/catalogdata/addset")]
        public OperationResult AddSet([FromBody] CatalogRow data)
        {
            try
            {
                if(!userData.CanSeeVMenu_AdminBrands)
                    return OperationResult.Forbid;
                repo.AddSet(data.CatalogID, data.RowID, data.LeftCatalogID, data.ParentRowID);
                return new OperationResult(true, g["Record saved!"], data.RowID);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


        [HttpPost, Route("/catalogdata/searchmultiple/{catalogid}")]
        public OperationResult SearchMultiple(int catalogid, [FromBody] List<string> barcodes)
        {
            try
            {
                return new OperationResult(true, null, repo.SearchMultiple(catalogid, barcodes));
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."], null);
            }
        }


        [Route("/catalogdata/importdata/{catalogid}")]
        public async Task<ContentResult> ImportData(int catalogID)
        {
            var dataImportService = factory.GetInstance<IDataImportService>();

            return await ReceiveDataFile(userData.Principal.Identity.Name, userData.SelectedProjectID, DocumentSource.Web, true,
                async (username, projectID, physicalPath) =>
                {
                    var config = mappingRepo.GetCatalogImportConfiguration(username, catalogID, null);
                    await dataImportService.StartUserJob(username, config);
                    return Content($"{{\"success\":true, \"message\":\"\"}}");
                });

        }

        [HttpGet, Route("/catalogdata/export/{catalogid}")]
        public IActionResult ExportCatalog(int catalogId)
        {
            string result = repo.GetList(catalogId);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(result);
            var csvBuilder = new StringBuilder();


            var headers = string.Join(",", data[0].Keys);
            csvBuilder.AppendLine(headers);


            foreach(var row in data)
            {
                var rowValues = string.Join(",", row.Values);
                csvBuilder.AppendLine(rowValues);
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var csvFileName = $"catalog_export_{catalogId.ToString()}{DateTime.Now:yyyyMMddHHmmss}.csv";

            // Devolver el archivo como una respuesta de tipo File
            return File(csvBytes, "text/csv", csvFileName);


        }

        private async Task<ContentResult> ReceiveDataFile(string username, int projectid, DocumentSource source, bool purgeExistingJobs, Func<string, int, string, Task<ContentResult>> action)
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Content($"{{\"success\":false, \"message\":\"{g["User does not have the required permissions to perform this operation."]}\"}}");
            try
            {
                var dataImportService = factory.GetInstance<IDataImportService>();

                if(Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    if(await dataImportService.RegisterUserJob(username, projectid, source, purgeExistingJobs))
                    {
                        var file = Request.Form.Files[0];
                        var fileName = file.FileName;
                        var physicalPath = Path.Combine(appInfo.SystemDownloadsDir, userData.Principal.Identity.Name, fileName);
                        var dir = Path.GetDirectoryName(physicalPath);
                        if(!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        using(var fileStream = new FileStream(physicalPath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        return await action(username, projectid, physicalPath);
                    }
                    else return Content($"{{\"success\":false, \"message\":\"{g["User is currently executing a document import operation, cannot start a new one."]}\"}}");
                }
                else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return Content($"{{\"success\":false, \"message\": \"{g["Unexpected error"]}\"}}");
            }
        }
    }

    public class CatalogData
    {
        public int CatalogID;
        public string Data;
    }

    public class CatalogRow
    {
        public int CatalogID;
        public int RowID;
        public int LeftCatalogID;
        public int ParentRowID;

    }
}