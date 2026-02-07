using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Repositories.OrderPool;
using WebLink.Contracts.Services;


namespace PrintCentral.Controllers
{
    public class ManualOrderEntryController : Controller
    {
        private static readonly HttpClient client = new HttpClient();

        private ILocalizationService g;
        private ILogService log;
        private IOrderPoolRepository repo;
        private IManualEntryServiceSelector manualEntryServiceSelector;

        private IOrderRepository orderRepository;
        private readonly PrintDB printDB;
        private IFactory factory;
        private ITempeOrderXmlHandler tempeOrderXmlHelper;
        private IProviderRepository providerRepo;
        private IBarçaCatalogHandler barçaCatalogHandler;
        private IPDFZaraExtractorService zaraExtractorService;


        public ManualOrderEntryController(IOrderPoolRepository repo,
                                          ILocalizationService g,
                                          ILogService log,
                                          IManualEntryServiceSelector manualEntryServiceSelector,
                                          IOrderRepository orderRepository,

                                          IFactory factory,
                                          ITempeOrderXmlHandler tempeOrderXmlHelper,
                                          IBarçaCatalogHandler barçaCatalogHandler,
                                          IProviderRepository providerRepo,
                                          IPDFZaraExtractorService zaraExtractorService)
        {
            this.repo = repo;
            this.g = g;
            this.log = log;
            this.manualEntryServiceSelector = manualEntryServiceSelector;
            this.orderRepository = orderRepository;
            this.factory = factory;
            printDB = factory.GetInstance<PrintDB>();
            this.tempeOrderXmlHelper = tempeOrderXmlHelper;
            this.barçaCatalogHandler = barçaCatalogHandler;
            this.providerRepo = providerRepo;
            this.zaraExtractorService = zaraExtractorService;
        }
        [HttpGet, Route("/manualorderentry/getordersbyproject/{projectid}")]
        public OperationResult GetOrdersByProject(int projectid)
        {
            try
            {
                var result = repo.GetOrdersByProject(projectid);
                return new OperationResult(success: true, data: result, message: string.Empty);

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(success: false, data: null, message: g["Something was wrong"]);
            }
        }


        [HttpGet, Route("/manualorderentry/GetOrdersByOrderNumber/{ordernumber}/{projectid}")]
        public OperationResult GetOrdersByOrderNumber(string orderNumber, int projectid)
        {
            try
            {
                var result = repo.GetOrderByOrderNumber(orderNumber, projectid);
                return new OperationResult(success: true, data: result, message: string.Empty);

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(success: false, data: null, message: g["Something was wrong"]);
            }
        }

        [HttpGet, Route("/manualorderentry/GetOrdersByCompany/{Companyid}/{projectid}")]
        public OperationResult GetOrdersByCompany(int companyid, int projectid)
        {
            try
            {
                var result = repo.GetOrdersByCompanyId(companyid, projectid);
                return new OperationResult(success: true, data: result, message: string.Empty);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(success: false, data: null, message: g["Something was wrong"]);
            }
        }

        //[HttpGet, Route("manualorderentry/getproviderlocation2/{provider}/{companyid}")]
        //public async Task<OperationResult> GetProviderLocation2(string provider, int companyid)
        //{
        //    try
        //    {
               
        //        return new OperationResult(success: true, data: "MORIOXO", message: string.Empty);
        //    }
        //    catch(Exception ex)
        //    {

        //        log.LogException(ex);
        //        return new OperationResult(success: false, data: null, message: g["No provider location found!"]);
        //    }
        //}

        [HttpPost, Route("/manualentry/getordersbyfilter")]
        public async Task<OperationResult> GetOrdersByFilter([FromBody] OrderPoolFilter filter)
        {
            var service = this.manualEntryServiceSelector.GetFilterService(filter.ManualEntryFilterService);
            if(service == null)
            {
                return new OperationResult(false, g["Manual Entry filter service not registered"]);
            }
            var result = await service.GetOrdersFromFilter(filter);
            return result;
        }
        [HttpGet, Route("manualorderentry/getproviderlocation/{provider}/{companyid}")]
        public async Task<OperationResult> GetProviderLocation(string provider, int companyid)
        {
            try
            {
                var result = providerRepo.GetProviderLocationName(provider, companyid);
                return new OperationResult(success: true, data: result, message: string.Empty); 
            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(success: false, data: null, message: g["No provider location found!"]);
            }
        }

        [HttpPost, Route("/manualorderentry/groupordersbypattern")]
        public async Task<OperationResult> GroupOrdersByPattern([FromBody] OrderPoolGrouping groupOrders)
        {
            try
            {
                var service = this.manualEntryServiceSelector.GetGrouppingService(groupOrders.ManualEntryGroupingService);
                if(service == null)
                {
                    return new OperationResult(false, g["Manual Entry groupping service not registered"]);
                }
                return await service.GroupOrders(groupOrders);
                
            }
            catch(Exception ex) 
            {

                return new OperationResult(false, g["Operation coudnt be completed"]); 
            }
        }

        [HttpPost, Route("/manualorderentry/save")]
        public async Task<OperationResult> Save([FromBody] DataEntryRq rq)
        {
            try
            {

                var service = this.manualEntryServiceSelector.GetService(rq.ManualEntryService);
                if(service == null)
                {
                    return new OperationResult(false, g["Manual Entry service not registered"]);
                }
                var result = await service.SaveOrder(rq, User.Identity.Name);
                return result;

            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }


        [HttpPost, Route("/manualorderentry/deleteorder")]
        public async Task<OperationResult> DeleteOrder([FromBody] DeleteOrderDTO rq)
        {
            try
            {

                var service = this.manualEntryServiceSelector.GetService(rq.ManualEntryService);
                if(service == null)
                {
                    return new OperationResult(false, g["Manual Entry service not registered"]);
                }

                var deletedOrder = new DeleteOrderPoolDTO()
                {
                    OrderNumber = rq.OrderNumber,
                    ArticleCode = rq.ArticleCode,
                    ProjectID = rq.ProjectID
                };
                await service.DeleteOrderPool(deletedOrder);
                var result = new OperationResult(true, g["Order deleted"]);
                return result;

            }
            catch(Exception ex)
            {

                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        [HttpPost, Route("manualorderentry/uploadorderfile/{brandId}/{companyId}/{projectId}/{serviceName}")]
        public async Task<OperationResult> UploadFile(int brandId, int companyId, int projectId, string serviceName, IFormFile file)
        {
            try
            {


                if(file == null || file.Length == 0)
                {
                    return new OperationResult()
                    {
                        Success = false,
                        Message = g["No files selected"]
                    };
                }
                if(!(".json".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0 || ".xml".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0))
                {
                    return new OperationResult() { Success = false, Message = g["Can only accept .json/ .xml files "] };
                }

                var service = manualEntryServiceSelector.GetService(serviceName);
                if(service == null)
                {
                    return new OperationResult(false, g["Not upload order file service registered"]);
                }

                using(var src = file.OpenReadStream())
                {
                    var manualEntryOrderfileDTO = new ManualEntryOrderFileDTO()
                    {
                        ProjectID = projectId,
                        CompanyID = companyId,
                        BrandID = brandId
                    };
                    var result = await service.UploadFileOrder(src, manualEntryOrderfileDTO);
                    return new OperationResult() { Success = true, Data = result };
                }

            }
            catch(Exception ex)
            {


                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        [HttpPost, Route("manualorderentry/getinditexdata")]
        public async Task<OperationResult> GetInditexData([FromBody] APIInditexDataRq rq)
        {

            if(rq == null)
            {
                return await Task.FromResult(new OperationResult(false, g["Invalid request"]));
            }

            if(string.IsNullOrEmpty(rq.purchaseOrder) || rq.model == 0 || rq.quality == 0)
            {
                return await Task.FromResult(new OperationResult(false, g["Invalid request"]));
            }


            try
            {
                var result = await tempeOrderXmlHelper.GetInditexAPIData(rq);
                return await Task.FromResult(new OperationResult(true, g["Order found"], data: result));
            }
            catch(Exception ex)
            {
                return await Task.FromResult(new OperationResult(false, g["Could not complete the operation"]));
            }
        }

        [HttpGet, Route("manualorderentry/getsavedorder/{orderID}/{projectid}")]
        public async Task<OperationResult> GetSavedOrder(int orderID, int projectId)
        {
            try
            {
                var result = printDB.CompanyOrders.Where(o => o.ID == orderID && o.ProjectID == projectId).OrderByDescending(o => o.ID).ToList();
                if(result.Count == 0)
                {
                    return await Task.FromResult(new OperationResult(false, g["Order not found"], data: null));
                }
                return await Task.FromResult(new OperationResult(true, g["Order  found"], data: result));

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return await Task.FromResult(new OperationResult(false, g["Could not complete the operation"]));
            }
        }

        [HttpGet, Route("manualorderentry/getImporters/{projectID}/{providerReference}")]
        public  OperationResult GetImportersData(int projectId, string providerReference)
        {
            try
            {
                var result =  barçaCatalogHandler.GetImporters(projectId, providerReference);
                if (result == null)
                {
                    result = new Importers()
                    {
                        MadeIn = string.Empty,
                        ImporterData = string.Empty,
                        IsNew = true
                    };
                }
                return new OperationResult(true,"",  result); 
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);

            }
        }

        [HttpPost, Route("manualorderentry/saveImporters/{projectId}")] 
        public OperationResult SaveImportersData(int projectId, [FromBody] Importers importers) 
        {
            try
            {
                barçaCatalogHandler.UpdateImporters(projectId, importers); 
                return new OperationResult(true, g["Importers data saved"]); 
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        [HttpGet, Route("manualorderentry/getVendors/{projectID}/{companyID}")] 
        public OperationResult GetVendors (int projectID, int companyID)
        {
            try
            {
                var result = new List<VendorDTO>(); 
                var vendors =  providerRepo.GetByCompanyIDME(companyID);
                foreach(var vendor in vendors) 
                {
                    var importer = barçaCatalogHandler.GetImporters(projectID, vendor.ClientReference);
                    result.Add(new VendorDTO
                    {
                        ImporterData = importer?.ImporterData ?? string.Empty,
                        MadeIn = importer?.MadeIn ?? string.Empty,
                        ClientReference = vendor.ClientReference,
                        CompanyName = vendor.CompanyName,
                        ID = vendor.ID
                    });
                }
                return new OperationResult(true, "", result);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        [HttpGet, Route("manualorderentry/getmadeindata/{projectid}")]
        public OperationResult GetMadeInData(int projectid)
        {
            try
            {
                var result = barçaCatalogHandler.GetImportersMadeIn(projectid);
                return new OperationResult(true, "", result);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        [HttpPost, Route("manualorderentry/processZaraPdf")]
        public async Task<IActionResult> ProcessZaraPdf(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = g["No files selected"] });
                }

                // Preparar la solicitud a la API externa
                using (var formData = new MultipartFormDataContent())
                using (var fileStream = file.OpenReadStream())
                {
                    var fileContent = new StreamContent(fileStream);
                    formData.Add(fileContent, "pdf_file", file.FileName);
                
                    //var response = await client.PostAsync("https://label-extractor-service3-580501422882.europe-west1.run.app/process-document", formData);
                    var response = await client.PostAsync("https://label-extractor-service2-580501422882.europe-west1.run.app/process-document", formData);
                    // var response = await client.PostAsync("http://localhost:8080/process-document", formData);

                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, new { message = $"Error de API: {response.StatusCode}" });
                    }

                    var result = await response.Content.ReadAsStringAsync();
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                return StatusCode(500, new { message = g["Could not complete the operation"] });
            }
        }

        [HttpPost, Route("manualorderentry/savePDFZaraData")]
        public async Task<OperationResult> SavePDFZaraData([FromBody] PDFZaraExtractor pdfZaraData)
        {
            try
            {
                if(pdfZaraData == null)
                {
                    return new OperationResult(false, g["Invalid request data"]);
                }

                // Validar datos mínimos requeridos
                if(pdfZaraData.Main == null ||
                    string.IsNullOrEmpty(pdfZaraData.Main.Empresa) ||
                    string.IsNullOrEmpty(pdfZaraData.Main.Proveedor) ||
                    pdfZaraData.Pedido?.Articulos == null ||
                    !pdfZaraData.Pedido.Articulos.Any())
                {
                    return new OperationResult(false, g["Missing required data"]);
                }

                // Aquí puedes agregar la lógica para procesar/guardar el objeto
                // Por ejemplo, guardar en base de datos o realizar otras operaciones
                await zaraExtractorService.SendOrder(pdfZaraData); 

                return new OperationResult(true, g["Data processed successfully"], pdfZaraData);
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Could not complete the operation"]);
            }
        }

        public class VendorDTO
        {
            public int ID { get; set; }
            public string CompanyName { get; set; } 
            public string ClientReference { get; set; } 
            public string ImporterData { get; set; } 
            public string MadeIn { get; set; }
        }

        public class DeleteOrderDTO
        {
            public  int  ProjectID { get; set; }
            public string OrderNumber { get; set; }
            public string ArticleCode { get; set; } 
            public string ManualEntryService { get; set; }   
        }
    }

}
