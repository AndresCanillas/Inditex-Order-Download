using Newtonsoft.Json;
using OrderDonwLoadService.Model;
using OrderDonwLoadService.Services;
using OrderDonwLoadService.Services.ImageManagement;
using Service.Contracts;
using StructureInditexOrderFile;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;


namespace OrderDonwLoadService.Synchronization
{
    public interface IOrderServices
    {
        Task<string> GetOrder(string orderNumber, string campaignCode, string vendorId);
    }

    public class OrderServices : IOrderServices
    {
        private readonly IAppConfig appConfig;
        private readonly IAppLog log;
        private readonly IEventQueue events;
        private readonly IApiCallerService apiCaller;
        private readonly IImageManagementService imageManagementService;
        private string workDirectory = null;

        public OrderServices(
            IAppConfig appConfig,
            IAppLog log,
            IApiCallerService apiCaller,
            IEventQueue events,
            IImageManagementService imageManagementService)
        {
            this.appConfig = appConfig;
            this.log = log;
            this.apiCaller = apiCaller;
            this.events = events;
            this.imageManagementService = imageManagementService;
            var url = this.appConfig.GetValue<string>("DownloadServices.ApiUrl", "https://api2.Inditex.com/");
            workDirectory = this.appConfig.GetValue<string>("DownloadServices.WorkDirectory", Directory.GetCurrentDirectory() + "/WorkDirectory");
            this.apiCaller.Start(url);
        }


        public async Task<string> GetOrder(string orderNumber, string campaignCode, string vendorId)
        {
            string message = $"Order number ({orderNumber}) not found in any queue.";


            var credentials = OrderDownloadHelper.LoadInditexCreadentials(log);
            if (credentials == null || credentials.Count == 0)
            {
                log.LogMessage("No credentials found for Inditex API.");
                return "Error No credentials found for Inditex API.";
            }

            foreach (var credential in credentials)
            {

                log.LogMessage($"Searching for Order number ({orderNumber}) in {credential.Name} queue.");

                var order = await FetchOrderAsync(credential, orderNumber, campaignCode, vendorId);
                if (order == null)
                {
                    log.LogMessage($"Order number ({orderNumber}) not found in {credential.Name} queue.");
                    continue;
                }

                message = $"Order number ({orderNumber}) found successfully in {credential.Name} queue.";

                log.LogMessage($"Order {order.POInformation.productionOrderNumber} in process.");

                try
                {
                    var imageResult = await imageManagementService.ProcessOrderImagesAsync(order);
                    if (imageResult.RequiresApproval)
                        log.LogMessage($"Order {order.POInformation.productionOrderNumber} has pending images to validate.");
                }
                catch (Exception ex)
                {
                    log.LogException(ex);
                }

                var filePath = "";
                try
                {
                    filePath = OrderDownloadHelper.SaveFileIntoWorkDirectory(order, workDirectory);
                }
                catch (Exception ex)
                {
                    log.LogMessage($"Error: {ex.Message}.");
                    SaveOrderWithError(ex.Message, order);
                    continue;
                }
                log.LogMessage($"Order {order.POInformation.productionOrderNumber} was saved into work directory.");


                if (string.IsNullOrEmpty(order.POInformation.section))
                    throw new Exception("Section property is null or empty.");
                
                
                foreach (var label in order.labels)
                {
                    if (string.IsNullOrEmpty(label.reference))
                        throw new Exception("Label reference property is null or empty.");
                    
                    var pluginType = label.reference.Substring(0,3);
                    events.Send(new FileReceivedEvent
                    {
                        FilePath = filePath,
                        OrderNumber = order.POInformation.productionOrderNumber.ToString(),
                        ProyectId = order.POInformation.campaign,
                        PluginType = pluginType
                    });

                    log.LogMessage($"File received event sent for order {order.POInformation.productionOrderNumber}, with label reference{pluginType} ");
                }

                break;
            }
            log.LogMessage(message);
            return message;
        }

        protected virtual async Task<InditexOrderData> FetchOrderAsync(
            Credential credential,
            string orderNumber,
            string campaignCode,
            string vendorId)
        {
#if DEBUG
            var rootDirectory = Directory.GetCurrentDirectory();
            var orderPath = Path.Combine(rootDirectory, "TestOrders", "15536_05987_I25_NNO_ZARANORTE.json");
            var orderText = File.ReadAllText(orderPath);
            return JsonConvert.DeserializeObject<InditexOrderData>(orderText);
#else
            AutenticationResult authResult = null;
            try
            {
                authResult = await OrderDownloadHelper.CallGetToken(appConfig, credential.User, credential.Password, apiCaller);
            }
            catch (Exception ex)
            {
                log.LogMessage($"Error: {ex.Message}.");
                return null;
            }

            return await CallGetOrderbyNumer(authResult.access_token, orderNumber, campaignCode, vendorId);
#endif
        }

        private void SaveOrderWithError(string message, InditexOrderData order)
        {
            var title = $"Can't to load order for Inditex client.";

            var historyDitectory = appConfig.GetValue<string>("DownloadServices.HistoryDirectory");
            var fileName = string.Concat(DateTime.Now.ToString("ddMMyyyyhhmmss", CultureInfo.InvariantCulture), ".json");
            if (!Directory.Exists(historyDitectory))
                Directory.CreateDirectory(historyDitectory);

            var orderText = JsonConvert.SerializeObject(order, Formatting.Indented);

            File.WriteAllText(Path.Combine(historyDitectory, fileName), orderText);

            events.Send(new NotificationReceivedEvent
            {
                CompanyID = appConfig.GetValue<int>("DownloadServices.ProjectInfoPrinCentral.CompanyID"),
                JsonData = orderText,
                Title = title,
                Message = message

            });
        }

    }
}
