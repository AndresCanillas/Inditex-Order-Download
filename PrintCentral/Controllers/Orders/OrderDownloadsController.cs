using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Service.Contracts.Documents;
using Services.Core;
using System;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    public class OrderDownloadsController : Controller
    {
        private ILogService log;
        private IOrderRepository orderRepo;
        private ILocalizationService g;
        private IRemoteFileStore tempStore;
        private readonly IDataImportService documentService;
        private IFileStoreManager storeManager;

        public OrderDownloadsController(
            ILogService log,
            IOrderRepository orderRepo,
            ILocalizationService g,
            IFileStoreManager storeManager,
            IDataImportService documentService
            )
        {
            this.log = log;
            this.orderRepo = orderRepo;
            this.g = g;
            this.documentService = documentService;
            tempStore = storeManager.OpenStore("TempStore");
            this.storeManager = storeManager;
        }

        [HttpPost("/orders/FileCsvReport")]
        public async Task<OperationResult> Index([FromBody]OrderReportFilter filter)
        {
            try
            {
                filter.CSV = true;

                string fileName = "PrintWebOrderReport_" + DateTime.Now.Ticks + ".csv";
                byte[] content = (await orderRepo.GetOrderFileReport(filter)).ToArray();

                var dstfile = await tempStore.CreateFileAsync(fileName);
                await dstfile.SetContentAsync(content);

                var fileid = dstfile.FileGUID;

                fileid = ExportToExcel(fileid);
                var excelFile = storeManager.GetFile(fileid);

                return new OperationResult() { Success = true, Data = ((IRemoteFile)(excelFile)).FileID, Message = g[$"Report was generated"] };

                ///*
                //return File(fileContents: content,
                //    contentType: "application/octet-stream",
                //    enableRangeProcessing: false,
                //    fileDownloadName: "PrintWebOrderReport_" + DateTime.Now.Ticks + ".csv");
                //*/

                //var cd = new System.Net.Mime.ContentDisposition
                //{
                //    // for example foo.bak
                //    FileName = "PrintWebOrderReport_" + DateTime.Now.Ticks + ".xslx",

                //    // always prompt the user for downloading, set to true if you want 
                //    // the browser to try to show the file inline
                //    Inline = false,
                //};
                ////Response.Headers.Add("Content-Type", "text/csv");
                //Response.Headers.Add("Content-Disposition", "attachment;filename="+ fileName);
                //Response.Headers.Add("Cache-Control", "must-revalidate, post-check=0, pre-check=0");
                //Response.Headers.Add("Pragma", "public");
                //Response.Headers.Add("Content-Transfer-Encoding", "binary");
                //var contentType = cd.ToString();
                //return File(content, "application/octet-stream", fileName, true);
            }
            catch (Exception ex)
            {
                log.LogException(ex);
                //throw;
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        [HttpPost("/orders/FileCsvCustomReport")]
        public async Task<OperationResult> GetCustomReport([FromBody]OrderReportFilter filter)
        {
            try
            {
                filter.CSV = true;

                string fileName = "PrintWebCustomReport_" + DateTime.Now.Ticks + ".csv";
                byte[] content = orderRepo.GetOrderFileCustomReport(filter).ToArray();

                var dstfile = await tempStore.CreateFileAsync(fileName);
                await dstfile.SetContentAsync(content);
                
                var fileid = dstfile.FileGUID;

                fileid =  ExportToExcel(fileid) ;

                
                var excelFile = storeManager.GetFile(fileid);

                return new OperationResult() { Success = true, Data = ((IRemoteFile)(excelFile)).FileID, Message = g[$"Report was generated"] };

            }
            catch (Exception ex)
            {
                log.LogException(ex);
                //throw;
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }

        private Guid ExportToExcel (Guid fileGuid)
        {
            var task = documentService.CreateExcelFromCSV(new Service.Contracts.Documents.ExcelConfigurationRequest() { FromCSVFileID = fileGuid });
            task.Wait();

            if(!task.Result.Success)
            {
                throw new Exception(g["Delivery Report File cannot be created"]);
            }

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<DocumentImportCreateExcelResponse>(task.Result.Data.ToString());

            return response.ExcelFileID;
        }

        [HttpPost("/orders/DeliveryReport")]
        public async Task<OperationResult> GetDeliveryReport([FromBody] OrderReportFilter filter)
        {
            try
            {
                filter.CSV = true;
                byte[] content = orderRepo.GetDeliveryReport(filter).ToArray();

                string fileName = "DeliveryReport_" + DateTime.Now.Ticks + ".csv";
                var datafile = await tempStore.CreateFileAsync(fileName);
                await datafile.SetContentAsync(content);

                var fileid = datafile.FileGUID;

                fileid =  ExportToExcel(fileid) ;

                
                var excelFile = storeManager.GetFile(fileid);

                return new OperationResult() { Success = true, Data = ((IRemoteFile)(excelFile)).FileID, Message = g[$"Report was generated"] };

            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return new OperationResult(false, g["Operation could not be completed."]);
            }
        }


    }
}