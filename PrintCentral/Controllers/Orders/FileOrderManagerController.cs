using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Services.Automated;

namespace PrintCentral.Controllers
{
    public class FileOrderManagerController : Controller
    {
        private const string PROJECT_CONTAINER = "ProjectContainer";
        private ILocalizationService g;
        private IAppLog log;
        private IFileStoreManager storeManager;
        private IRemoteFileStore store;
        private IProjectRepository projectRepo;
        private readonly IEventQueue events;
        private IFactory factory;

        public FileOrderManagerController(

            ILocalizationService g,
            IAppLog log,
            IFileStoreManager storeManager,
            IProjectRepository projectRepo,
            IEventQueue events,
            IFactory factory)
        {

            this.g = g;
            this.log = log;
            this.storeManager = storeManager;
            store = storeManager.OpenStore("ProjectStore");
            this.projectRepo = projectRepo;
            this.events = events;
            this.factory = factory;
        }

       


        [HttpPost, Route("/fileordermanager/uploadorderfile/{projectid}")]
        public async Task<IActionResult> UploadOrderFile(int projectid)
        {
            try
            {
                log.LogWarning("Armand Thiery - Controller call");
                if(Request.Form.Files != null && Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    log.LogWarning($"Armand Thiery, controller has received file {file.FileName}");
                    await SaveFileProjectStore(file, projectid);
                    return Content($"{{\"success\":true, \"message\":\"\", \"Data\":null}}"); ;
                }
                else
                {
                    log.LogWarning($"Armand Thiery, controller has not received any file ");
                    return Content($"{{\"success\":false, \"message\":\"{g["Invalid Request. Was expecting a single file."]}\"}}");
                }
            }
            catch(Exception ex)
            {
                log.LogException(ex);
                return Content($"{{\"success\":false, \"message\":\"{g["Unexpected error while uploading image file."]}\"}}");
            }
        }

        private async Task SaveFileProjectStore(IFormFile file, int projectid)
        {
            
            var project = projectRepo.GetByID(projectid);  // NOTE: Done just to verify that the calling user has permissions to access the specified project
            var fileName = file.FileName;
            if(!store.TryGetFile(projectid, out var container))
                container = store.GetOrCreateFile(projectid, PROJECT_CONTAINER);

            if(fileName.Contains("\\"))
                fileName = Path.GetFileName(fileName);

            var images = container.GetAttachmentCategory("Images");
            var dstfile = images.GetOrCreateAttachment(fileName);

            using(var stream = file.OpenReadStream())
                dstfile.SetContent(stream);

            var result = dstfile.GetMetadata<ImageMetadata>();
            if(String.IsNullOrWhiteSpace(result.CreateUser))
            {
                result.CreateUser = "SYSTEM";
                result.CreateDate = DateTime.Now;
            }
            result.ProjectID = projectid;
            result.FileName = fileName;
            result.SizeBytes = dstfile.FileSize;
            result.UpdateUser = "SYSTEM";
            result.UpdateDate = DateTime.Now;
            dstfile.SetMetadata(result);
           
            events.Send(new FileOrdersManagerEvent(projectid, fileName)); 

        }
		[HttpGet, Route("/fileordermanager/getsytemchangedorders")]
		public async Task<OperationResult> GetSystemChangedOrders()
        //[FromQuery] SystemChangedOrderFilterDTO filter 
        {
            try
            {
                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    var list = ctx.SystemChangedOrdersLog.ToList();
                    return new OperationResult(true, "", list); 
                }
            }
            catch(Exception)
            {

                throw;
            }

			
		}
    }
    public class SystemChangedOrderFilterDTO 
    {
        public string OrderNumber { get; set; } 
        public string BatchNumber { get; set; }  
        public SystemOrderAction SystemOrderAction { get; set; }     
        public DateTime? CreatedDate { get; set; }   
    }
}
