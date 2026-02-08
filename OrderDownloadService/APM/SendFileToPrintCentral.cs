using OrderDonwLoadService.Model;
using OrderDonwLoadService.Services;
using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using System.IO;
using System.Threading.Tasks;

namespace OrderDonwLoadService
{
    public class SendFileToPrintCentral : EQEventHandler<FileReceivedEvent>
    {
        private readonly IPrintCentralService central;
        private readonly IConnectionManager db;
        private readonly IAppLog log;
        private readonly IAppConfig appConfig;
        private readonly IEventQueue events;
        private readonly IImageManagementService imageManagementService;

        public SendFileToPrintCentral(
            IPrintCentralService central,
            IAppLog log,
            IConnectionManager db,
            IAppConfig appConfig,
            IEventQueue events,
            IImageManagementService imageManagementService)
        {
            this.central = central;
            this.log = log;
            this.appConfig = appConfig;
            this.db = db;
            this.events = events;
            this.imageManagementService = imageManagementService;
        }

        
        public override EQEventHandlerResult HandleEvent(FileReceivedEvent e)
        {

            var isClearProcessedFiles = this.appConfig.GetValue<bool>("DownloadServices.IsClearProcessedFiles", false);
            if(!imageManagementService.AreOrderImagesReady(e.FilePath))
            {
                log.LogMessage($"Order {e.OrderNumber} is waiting for image validation.");
                events.Send(new NotificationReceivedEvent
                {
                    CompanyID = appConfig.GetValue<int>("DownloadServices.ProjectInfoPrinCentral.CompanyID"),
                    Title = $"Order {e.OrderNumber} en espera",
                    Message = "El pedido está en espera porque existen imágenes pendientes de validar en fuente.",
                    FileName = Path.GetFileName(e.FilePath)
                });
                return EQEventHandlerResult.Delay5;
            }
            if(SendOrderToPrintCentral(e.FilePath, e.ProyectId,string.Join( Path.GetFileName(e.FilePath),"_",e.PluginType)).Result)
            {
                log.LogMessage($"Order {e.OrderNumber} was Sended to PrintCentral.");

                if(isClearProcessedFiles)
                {
                    if(OrderDownloadHelper.ClenerFiles(e.FilePath, appConfig))
                    {
                        log.LogMessage($"Order {e.OrderNumber} was move into history directory.");
                    }
                    else
                    {
                        log.LogMessage($"Error when to tried move {e.OrderNumber}  into history directory the order.");
                    }
                }
                return EQEventHandlerResult.OK;
            }
            else
            {
                log.LogMessage($"Error: When to tried order move {e.OrderNumber}  into history directory.");
            }
            return EQEventHandlerResult.Delay5;
        }

        private async Task<bool> SendOrderToPrintCentral(string filePath, string proyectId, string fileName)
        {

            var companyID = this.appConfig.GetValue<int?>("DownloadServices.ProjectInfoApiPrinCentral.CompanyID", null) ??
                    throw new System.Exception("CompanyID is not configured in app settings.");
            var brandID = appConfig.GetValue<int?>("DownloadServices.ProjectInfoApiPrinCentral.BrandID", null) ??
                    throw new System.Exception("BrandID is not configured in app settings.");

            var userNamePrintCentral = this.appConfig.GetValue<string>("DownloadServices.PrintCentralCredentials.User", null) ??
                throw new System.Exception("UserName is not configured in app settings.");
            var passwordPrintCentral = this.appConfig.GetValue<string>("DownloadServices.PrintCentralCredentials.Password", null) ??
                throw new System.Exception("Password is not configured in app settings.");

            if(string.IsNullOrWhiteSpace(proyectId) || string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(fileName))
            {
                log.LogMessage($"Error: Invalid parameters for sending order to PrintCentral. " +
                                       $"ProyectId: {proyectId}, FilePath: {filePath}, FileName: {fileName}");
                return false;
            }
            if(!File.Exists(filePath))
            {
                log.LogMessage($"Error: File {filePath} does not exist.");
                return false;
            }

            await central.LoginAsync("/", userNamePrintCentral, passwordPrintCentral);

            ProjectInfo project = null;
            using(var conn = db.OpenDB())
            {

                var sql = @"
					SELECT	p.ID, p.Name, p.ProjectCode,
							p.FTPFolder as ProjectFtpFolder,
							b.FTPFolder as BrandFtpFolder,
							b.ID as BrandID
					FROM Projects p
					JOIN Brands b ON p.BrandID = b.ID
					WHERE p.ProjectCode = @season
                    AND p.BrandID = @brandID
                    AND b.CompanyID = @companyID";



                project = conn.SelectOne<ProjectInfo>(sql, proyectId, brandID, companyID);

                if(project == null)
                {

                    var title = $"The seasion missing ({proyectId})";

                    var message = $"Error while procesing file {fileName}, " +
                        $"not found the project ({proyectId}) for CompanyID= {companyID} and CompanyID= {brandID} " +
                        $"to process the order number ({filePath.Substring(0, filePath.Length - 4)}).";

                    events.Send(new NotificationReceivedEvent
                    {
                        CompanyID = companyID,
                        Title = title,
                        Message = message,
                        FileName = fileName
                    });

                    project = new ProjectInfo { BrandID = -1, ID = -1 };
                }

            }

            if(project != null)
            {
                var orderData = new OrderData()
                {
                    CompanyID = companyID,
                    BrandID = project.BrandID,
                    ProjectID = project.ID,
                    FactoryID = 0,
                    ProductionType = (int)ProductionType.IDTLocation,
                    IsBillable = true
                };


                var result = await central.FtpServiceUpload<OrderData, OrderUploadResponse>($"api/intake/ftp", orderData, filePath,filePath);

                if(!result.Success)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            central.Logout();
            return true;

        }


    }
}
