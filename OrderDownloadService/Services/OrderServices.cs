using Newtonsoft.Json;
using OrderDonwLoadService.Services;
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
        private string workDirectory = null;

        public OrderServices(IAppConfig appConfig, IAppLog log, IApiCallerService apiCaller, IEventQueue events)
        {
            this.appConfig = appConfig;
            this.log = log;
            this.apiCaller = apiCaller;
            this.events = events;
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

#if DEBUG
                var rootDirectory = Directory.GetCurrentDirectory();
                var _order = File.ReadAllText(rootDirectory + @"\TestOrders\15536_05987_I25_NNO_ZARANORTE.json");
                var order = JsonConvert.DeserializeObject<InditexOrderData>(_order);


#else

                AutenticationResult authResult = null;
                try
                {
                    authResult = await OrderDownloadHelper.CallGetToken(appConfig, credential.User, credential.Password, apiCaller);
                }
                catch (Exception ex)
                {
                    log.LogMessage($"Error: {ex.Message}.");
                    continue;
                }


                var order = await CallGetOrderbyNumer(authResult.access_token, orderNumber, campaignCode, vendorId);
#endif

                if (order != null)
                {
                    message = $"Order number ({orderNumber}) found successfully in {credential.Name} queue.";
                    break;
                }

                log.LogMessage($"Order {order.POInformation.productionOrderNumber} in process.");

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
            }
            log.LogMessage(message);
            return message;
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