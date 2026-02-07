using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Documents;
using Service.Contracts.WF;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Delivery;
using WebLink.Contracts.Workflows;

namespace WebLink.Controllers
{
    [Authorize]
    public class UploadController : Controller
    {
        private IDataImportService dataImportService;
        private ILogService log;
        private IMappingRepository mappingRepo;
        private ILocalizationService g;
        private IUserData userData;
        private IBrandRepository brandRepo;
        private IProjectRepository projectRepo;
        private IAutomatedProcessManager apm;
        private IFileStoreManager storeManager;
        private IRemoteFileStore tempStore;
        private IRemoteFileStore workflowStore;
        private ICatalogRepository catalogRepo;
        private readonly IWorkflowQueries workflowQueries;
        private readonly IOrderUtilService orderUtilService;
        private readonly IDeliveryRepository deliveryRepo;

        public UploadController(
            IDataImportService dataImportService,
            ILogService log,
            IMappingRepository mappingRepo,
            ILocalizationService g,
            IUserData userData,
            IBrandRepository brandRepo,
            IProjectRepository projectRepo,
            IAutomatedProcessManager apm,
            IFileStoreManager storeManager,
            ICatalogRepository catalogRepo,
            IWorkflowQueries workflowQueries,
            IOrderUtilService orderUtilService,
            IDeliveryRepository deliveryRepo
            )
        {
            this.dataImportService = dataImportService;
            this.log = log;
            this.mappingRepo = mappingRepo;
            this.g = g;
            this.userData = userData;
            this.brandRepo = brandRepo;
            this.projectRepo = projectRepo;
            this.apm = apm;
            this.storeManager = storeManager;
            tempStore = storeManager.OpenStore("TempStore");
            workflowStore = storeManager.OpenStore("WorkflowStore");
            this.catalogRepo = catalogRepo;
            this.workflowQueries = workflowQueries;
            this.orderUtilService = orderUtilService;
            this.deliveryRepo = deliveryRepo;   
        }


        [Route("/upload/order")]
        public async Task<IActionResult> ImportOrder()
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();
            return await ReceiveFile(userData.Principal.Identity.Name, userData.SelectedProjectID, DocumentSource.Web, true,
                async (user, projectid, file) =>
                {
                    var config = mappingRepo.GetDocumentImportConfiguration(user, projectid, "Orders", file);
                    await dataImportService.StartUserJob(user, config);
                    return Content($"{{\"success\":true, \"message\":\"\"}}");
                });
        }

        [Route("/upload/delivery")]
        public async Task<IActionResult> ImportDelivery()
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();

            return await ReceiveFile(userData.Principal.Identity.Name, userData.SelectedProjectID, DocumentSource.Web, true,
                async (user, projectid, file) =>
                {
                    var result= await dataImportService.GetDataFromExcel( file.FileGUID);

                    if (result.Success)
                    {
                        try
                        {
                            deliveryRepo.ImportDeliveryFile(user, result.Data as string);
                            return Content($"{{\"success\":true, \"message\":\"\"}}");
                        }
                        catch (Exception ex)
                        {
                            return Content($"{{\"success\":false, \"message\":\"{ex.Message}\"}}");
                        }
                    }
                    else
                    {
                        return Content($"{{\"success\":false, \"message\":\"{result.Message}\"}}");
                    }
                });
        }

        [Route("/upload/variabledata")]
        public async Task<IActionResult> ImportVariableData()
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();
            return await ReceiveFile(userData.Principal.Identity.Name, userData.SelectedProjectID, DocumentSource.Web, true,
                async (user, projectid, file) =>
                {
                    var defaultCatalogName = Catalog.VARIABLEDATA_CATALOG;
                    var orderCatalog = catalogRepo.GetByName(projectid, Catalog.ORDER_CATALOG);

                    if(userData.IsSysAdmin)
                    {
                        var mappingFound = mappingRepo.GetByProjectID(projectid).FirstOrDefault(w => w.RootCatalog != orderCatalog.ID && w.FileNameMask != null && Regex.IsMatch(file.FileName, w.FileNameMask, RegexOptions.IgnoreCase));

                        if(mappingFound != null)
                        {
                            var ct = catalogRepo.GetByID(mappingFound.RootCatalog);
                            defaultCatalogName = ct.Name;
                        }
                    }

                    var config = mappingRepo.GetDocumentImportConfiguration(user, projectid, defaultCatalogName, file);
                    await dataImportService.StartUserJob(user, config);
                    return Content($"{{\"success\":true, \"message\":\"\"}}");

                });
        }


        [HttpPost, Route("/upload/batchfile")]
        public async Task<IActionResult> ImportBatchFile()
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();
            return await ReceiveFile(userData.Principal.Identity.Name, userData.SelectedProjectID, DocumentSource.Web, true,
                async (user, projectid, physicalPath) =>
                {
                    var config = mappingRepo.GetBatchFileImportConfiguration(user, projectid, physicalPath);
                    await dataImportService.StartUserJob(user, config);
                    return Content($"{{\"success\":true, \"message\":\"\"}}");
                });
        }


        private async Task<IActionResult> ReceiveFile(string username, int projectid, DocumentSource source, bool purgeExistingJobs, Func<string, int, IRemoteFile, Task<ContentResult>> action)
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Content($"{{\"success\":false, \"message\":\"{g["User does not have the required permissions to perform this operation."]}\"}}");
            try
            {
                if(Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    if(await dataImportService.RegisterUserJob(username, projectid, source, purgeExistingJobs))
                    {
                        var srcfile = Request.Form.Files[0];
                        var fileName = srcfile.FileName;

                        var dstfile = await tempStore.CreateFileAsync(fileName);
                        using(var stream = srcfile.OpenReadStream())
                            await dstfile.SetContentAsync(stream);

                        return await action(username, projectid, dstfile);
                    }
                    else return Content($"{{\"success\":false, \"message\":\"{g["User is currently executing a document import operation, cannot start a new one."]}\"}}");
                }
                else return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
            }
            catch(MappingNotFoundException ex1)
            {
                log.LogException(ex1);
                return Content($"{{\"success\":false, \"message\": \"{g["Mapping Configuration Not Found for selected file"]}\"}}");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return Content($"{{\"success\":false, \"message\": \"{g["Unexpected error"]}\"}}");
            }
        }


        [Route("/upload/progress")]
        public DocumentImportProgress GetJobProgress()
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return null;
                var result = dataImportService.GetJobProgress(userData.Principal.Identity.Name);
                return result;
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new DocumentImportProgress() { Progress = -1 };
            }
        }


        [HttpGet, Route("upload/jobresult")]
        public async Task<DocumentImportResult> GetJobResult()
        {
            try
            {
                var result = new DocumentImportResult();
                if(!userData.CanSeeVMenu_UploadMenu)
                {
                    result = new DocumentImportResult() { Success = false };
                    result.Errors.Add(new DocumentImportError("", 0, 0, 0, "User does not have the required permissions to perform this operation."));
                    return result;
                }

                result = await dataImportService.GetJobResult(userData.Principal.Identity.Name);
                return result;
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }


        [HttpGet, Route("upload/importeddata")]
        public async Task<ImportedData> GetImportedData()
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return null;
                var result = await dataImportService.GetImportedDataAsync(userData.Principal.Identity.Name);
                return result;
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return null;
            }
        }

        [HttpPost, Route("upload/variabledata/complete")]
        public async Task<OperationResult> CompleteVariableDataUpload()
        {
            OperationResult result = new OperationResult();
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return OperationResult.Forbid;
                await dataImportService.CompleteUserJob(userData.Principal.Identity.Name, null, null);
                result.Message = null;
                result.Success = true;
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                result.Success = false;
                result.Message = g["There has been an error when trying to complete the upload, please contact customer support."];
            }
            return result;
        }

        [HttpPost, Route("/upload/cancel")]
        public async Task<OperationResult> CancelJob()
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return OperationResult.Forbid;
                await dataImportService.CancelJob(userData.Principal.Identity.Name);
                return new OperationResult(true, "");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


        [HttpPost, Route("/upload/purge")]
        public async Task<OperationResult> PurgeJob()
        {
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return OperationResult.Forbid;
                await dataImportService.PurgeJob(userData.Principal.Identity.Name);
                return new OperationResult(true, "");
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }



        //============================= INTAKE REFACTOR ==============================


        [HttpPost, Route("api/intake/completeupload")]
        public async Task<OperationResult> CompleteOrderUpload2([FromBody] UploadOrderDTO data)
        {
            var timeOut = 120;
#if DEBUG
            timeOut = 600;
#endif
            OperationResult result = new OperationResult();
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return OperationResult.Forbid;
                var userName = userData.Principal.Identity.Name;

                var r = await dataImportService.GetJobResult(userName);
                if(!r.Success)
                    return OperationResult.InternalError;

                var project = await projectRepo.GetByIDAsync(userData.SelectedProjectID);
                var brand = await brandRepo.GetByIDAsync(project.BrandID);

                var job = dataImportService.GetUserJob(userName);
                var src = await storeManager.GetFileAsync(job.FileGUID);
                //var filecontent = src.GetContentAsStream();

                // WARN: if enable orderpool always be upload to the orderpool
                if(project.EnablePoolFile)
                {
                    await orderUtilService.SendToPool(userData.SelectedProjectID, src.FileName, src.GetContentAsStream());
                    result.Success = true;
                    result.Message = $"<p>{g["The order has been uploaded to the order pool and is pending assignment."]} </p>";
                }
                else
                {

                    #region sent to intake


                    var dst = await workflowStore.CreateFileAsync(job.FileName);
                    dst.SetContent(src.GetContentAsBytes());

                    var IsForIDTFactory = data.ProductionType == ProductionType.IDTLocation;


                    var input = new IntakeWorkflowInput()
                    {
                        CompanyID = userData.SelectedCompanyID,
                        BrandID = brand.ID,
                        ProjectID = project.ID,
                        IsBillable = IsForIDTFactory,
                        IsStopped = false,
                        IsTestOrder = false,
                        ProductionType = data.ProductionType,
                        Source = DocumentSource.Web,
                        UserName = userData.Principal.Identity.Name,
                        FileName = dst.FileName,
                        InputFile = dst.FileGUID,
                        WorkflowFileID = dst.FileID/*,
                    CustomerPrinterID = data.PrinterID,
                    CustomerLocationID = IsForIDTFactory ? null : data.FactoryID,
                    ProductionLocationID = IsForIDTFactory ? null : data.FactoryID*/
                    };

                    if(data.PrinterID > 0)
                        input.CustomerPrinterID = data.PrinterID;

                    if(data.FactoryID > 0)
                        if(!IsForIDTFactory)
                            input.CustomerLocationID = data.FactoryID;

                    var wf = await apm.GetWorkflowAsync("OrderIntake");
                    var item = new OrderFileItem(input);
                    item.MaxTries = 1;
                    await wf.InsertItemAsync(item);
                    var i = await wf.WaitForItemStatus(item.ItemID,
                        ItemStatus.Completed | ItemStatus.Rejected | ItemStatus.Waiting,
                        TimeSpan.FromSeconds(timeOut));

                    if(i.ItemStatus == ItemStatus.Rejected || i.ItemStatus == ItemStatus.Waiting)
                    {
                        IEnumerable<WorkItemError> ItemsWithErrors;
                        PagedItemFilter filter = new PagedItemFilter() { ItemID = (int)i.ItemID };
                        ItemsWithErrors = await workflowQueries.GetTaskErrorsAsync(filter);

                        result.Success = false;

                        result.Message = "<h2>" + ItemsWithErrors.FirstOrDefault().LastErrorMessage + "</h2><br>";
                        result.Message += "<p>" + g["If the error persists, please contact customer support and provide the next key [{0}].", i.Name] + "</p>";

                        await wf.CancelAsync(i, "Web upload failed, user will retry upload later.", userData.Principal.Identity);
                    }
                    else
                    {
                        result.Success = true;
                        result.Data = Newtonsoft.Json.JsonConvert.DeserializeObject(await i.GetSavedStateAsync());
                    }

                    #endregion sent to intake
                }

                await dataImportService.PurgeJob(userName);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                result.Success = false;
                result.Message = g["There has been an error when trying to complete the order, please contact customer support."];
            }
            return result;
        }


        [HttpPost, Route("api/intake/printlabels")]
        public async Task<OperationResult> IntakeFromPrintLabels([FromBody] CreateOrderDTO orderData)
        {
            OperationResult result = new OperationResult();
            try
            {
                if(!userData.CanSeeVMenu_UploadMenu)
                    return OperationResult.Forbid;
                var userName = userData.Principal.Identity.Name;

                var project = await projectRepo.GetByIDAsync(userData.SelectedProjectID);
                var brand = await brandRepo.GetByIDAsync(project.BrandID);

                var dst = await workflowStore.CreateFileAsync("printlabelsfromgrid.json");
                await dst.SetContentAsync(Encoding.Unicode.GetBytes(orderData.Data));

                var input = new IntakeWorkflowInput()
                {
                    CompanyID = userData.SelectedCompanyID,
                    BrandID = brand.ID,
                    ProjectID = project.ID,
                    IsBillable = (orderData.ProductionType == ProductionType.IDTLocation),
                    IsStopped = false,
                    IsTestOrder = false,
                    ProductionType = orderData.ProductionType,
                    Source = DocumentSource.Web,
                    UserName = userData.Principal.Identity.Name,
                    FileName = dst.FileName,
                    InputFile = dst.FileGUID,
                    CustomerLocationID = orderData.FactoryID,
                    CustomerPrinterID = orderData.PrinterID,
                    WorkflowFileID = dst.FileID
                };

                var wf = await apm.GetWorkflowAsync("OrderIntake");
                var item = new OrderFileItem(input);
                item.MaxTries = 1;
                await wf.InsertItemAsync(item);
                var i = await wf.WaitForItemStatus(item.ItemID, ItemStatus.Completed | ItemStatus.Rejected, TimeSpan.FromSeconds(120));

                if(i.ItemStatus == ItemStatus.Rejected)
                {
                    result.Success = false;
                    result.Message = g["There has been an error when trying to complete the order, please contact customer support."];
                }
                else
                {
                    var itemState = await i.GetSavedStateAsync<OrderFileItem>();
                    result.Data = itemState.CreatedOrders;
                    result.Success = true;
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                result.Success = false;
                result.Message = g["There has been an error when trying to complete the order, please contact customer support."];
            }
            return result;
        }


        [Route("/api/intake")]
        public async Task<IActionResult> SendOrderToIntake()
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();

            var project = await projectRepo.GetByIDAsync(userData.SelectedProjectID);
            var brand = await brandRepo.GetByIDAsync(project.BrandID);

            var srcfile = Request.Form.Files[0];
            var fileName = srcfile.FileName;

            var dstfile = await workflowStore.CreateFileAsync(fileName);
            using(var stream = srcfile.OpenReadStream())
                await dstfile.SetContentAsync(stream);

            var input = new IntakeWorkflowInput()
            {
                CompanyID = userData.CompanyID,
                BrandID = brand.ID,
                ProjectID = project.ID,
                IsBillable = true,
                IsStopped = false,
                IsTestOrder = false,
                ProductionType = ProductionType.IDTLocation,
                Source = DocumentSource.Web,
                UserName = userData.Principal.Identity.Name,
                FileName = fileName,
                InputFile = dstfile.FileGUID,
                WorkflowFileID = dstfile.FileID
            };

            var wf = await apm.GetWorkflowAsync("OrderIntake");
            await wf.InsertItemAsync(new OrderFileItem(input));

            return Content($"{{\"success\":true, \"message\":\"\"}}");
        }


        [HttpPost, Route("/api/intake/{projectid}")]
        public async Task<IActionResult> IntakeApiUpload(int projectid, IFormFile file)
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Content($"{{\"success\":false, \"message\":\"User does not have the required permissions to perform this operation.\"}}");

            var project = await projectRepo.GetByIDAsync(projectid);
            var brand = await brandRepo.GetByIDAsync(project.BrandID);

            var fileName = Path.GetFileName(file.FileName);
            var dstfile = await workflowStore.CreateFileAsync(fileName);
            using(var stream = file.OpenReadStream())
                await dstfile.SetContentAsync(stream);

            var input = new IntakeWorkflowInput()
            {
                CompanyID = userData.CompanyID,
                BrandID = brand.ID,
                ProjectID = project.ID,
                IsBillable = true,
                IsStopped = false,
                IsTestOrder = false,
                ProductionType = ProductionType.IDTLocation,
                Source = DocumentSource.API,
                UserName = userData.Principal.Identity.Name,
                FileName = fileName,
                InputFile = dstfile.FileGUID,
                WorkflowFileID = dstfile.FileID
            };

            var wf = await apm.GetWorkflowAsync("OrderIntake");
            await wf.InsertItemAsync(new OrderFileItem(input));

            return Content($"{{\"success\":true, \"message\":\"{g["File Uploaded"]}\"}}");
        }

        [Route("api/intake/ftp")]
        public async Task<IActionResult> IntakeFtpUpload([FromForm] string OrderData, IFormFile file)
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();

            try
            {
                log.LogMessage($"Received file from FTPWatcher: {file.FileName}");

                var dto = JsonConvert.DeserializeObject<UploadOrderDTO>(OrderData);

                var project = projectRepo.GetByID(dto.ProjectID, true);

                if(project.EnablePoolFile && project.PoolFileHandler != null)
                {
                    // send to the pool plugin
                    using(var stream = file.OpenReadStream())
                    {
                        //await handler.UploadAsync(project, stream);
                        await orderUtilService.SendToPool(dto.ProjectID, file.Name, stream);
                    }
                }
                else
                {
                    await LoadFiletoIntake(OrderData, file, DocumentSource.FTP);
                }

                return Content("{\"Success\":true, \"Message\":\"File was received\"}");
            }
            catch(Exception ex)
            {
                log.LogException($"Error receiving file {file.FileName}", ex);
                return Content("{\"Success\":false, \"Message\":\"File cannot be received\", \"Data\":{}}");
            }
        }

        [Route("api/intake/manualentry")]
        public async Task<IActionResult> ManualEntryUpload([FromForm] string OrderData, IFormFile file)
        {
            if(!userData.CanSeeVMenu_UploadMenu)
                return Forbid();

            try
            {
                log.LogMessage($"Received file from manual entry: {file.FileName}");

                await LoadFiletoIntake(OrderData, file, DocumentSource.Web);

                return Content("{\"Success\":true, \"Message\":\"File was received\"}");
            }
            catch(Exception ex)
            {
                log.LogException($"Error receiving file {file.FileName}", ex);
                return Content("{\"Success\":false, \"Message\":\"File cannot be received\", \"Data\":{}}");
            }
        }

        private async Task LoadFiletoIntake(string OrderData, IFormFile file, DocumentSource source)
        {
            IRemoteFile dstfile = null;

            dstfile = await workflowStore.CreateFileAsync(file.FileName);
            using(var stream = file.OpenReadStream())
                await dstfile.SetContentAsync(stream);

            var dto = JsonConvert.DeserializeObject<UploadOrderDTO>(OrderData);
            var project = await projectRepo.GetByIDAsync(dto.ProjectID);
            var brand = await brandRepo.GetByIDAsync(project.BrandID);


            var input = new IntakeWorkflowInput()
            {
                CompanyID = dto.CompanyID,
                BrandID = brand.ID,
                ProjectID = project.ID,
                IsBillable = dto.IsBillable,
                IsStopped = dto.IsStopped,
                IsTestOrder = false,
                ProductionType = dto.ProductionType,
                ProductionLocationID = dto.FactoryID > 0 ? (int?)dto.FactoryID : null,
                ERPReference = dto.MDOrderNumber,
                ClientCategory = dto.OrderCategoryClient,
                Source = source,
                UserName = userData.Principal.Identity.Name,
                FileName = file.FileName,
                InputFile = dstfile.FileGUID,
                WorkflowFileID = dstfile.FileID
            };

            var wf = await apm.GetWorkflowAsync("OrderIntake");

            var item = new OrderFileItem(input);

            await wf.InsertItemAsync(item);

        }
    }
}