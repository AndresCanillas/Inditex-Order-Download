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
            var url = this.appConfig.GetValue<string>(DownloadServiceConfig.ApiUrl, "https://api2.Inditex.com/");
            workDirectory = this.appConfig.GetValue<string>(DownloadServiceConfig.WorkDirectory, Directory.GetCurrentDirectory() + "/WorkDirectory");
            this.apiCaller.Start(url);
        }


        public async Task<string> GetOrder(string orderNumber, string campaignCode, string vendorId)
        {
            string message = $"Order number ({orderNumber}) not found in any queue.";

            PublishProgressEvent(orderNumber, "search-order", "in-progress", $"Searching order number ({orderNumber}) in queues.");

            var credentials = OrderDownloadHelper.LoadInditexCredentials(log);
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
                PublishProgressEvent(orderNumber, "search-order", "completed", message);
                PublishProgressEvent(orderNumber, "download-order", "completed", $"Order {order.ProductionOrder.PONumber} downloaded from source queue.");

                log.LogMessage($"Order {order.ProductionOrder.PONumber} in process.");

                var canSendToPrintCentral = true;
                try
                {
                    PublishProgressEvent(orderNumber, "download-images", "in-progress", $"Processing images for order {order.ProductionOrder.PONumber}.");
                    var imageResult = await imageManagementService.ProcessOrderImagesAsync(order);
                    if (imageResult.RequiresApproval)
                    {
                        canSendToPrintCentral = false;
                        PublishProgressEvent(orderNumber, "download-images", "completed", $"Images downloaded for order {order.ProductionOrder.PONumber}.");
                        var pendingImageMessage = $"Order {order.ProductionOrder.PONumber} is waiting image validation for source Indetgroup_Tempe_V6 before sending file to Print Central.";
                        PublishProgressEvent(orderNumber, "send-file-print-central", "pending-validation", pendingImageMessage);
                        log.LogMessage(pendingImageMessage);
                    }
                    else
                    {
                        PublishProgressEvent(orderNumber, "download-images", "completed", $"Images downloaded for order {order.ProductionOrder.PONumber}.");
                    }
                }
                catch (Exception ex)
                {
                    canSendToPrintCentral = false;
                    PublishProgressEvent(orderNumber, "download-images", "failed", ex.Message);
                    log.LogException(ex);
                }

                var filePath = "";
                try
                {
                    PublishProgressEvent(orderNumber, "download-order", "in-progress", $"Saving order {order.ProductionOrder.PONumber} into work directory.");
                    filePath = OrderDownloadHelper.SaveFileIntoWorkDirectory(order, workDirectory);
                    PublishProgressEvent(orderNumber, "download-order", "completed", $"Order {order.ProductionOrder.PONumber} was saved into work directory.");
                }
                catch (Exception ex)
                {
                    PublishProgressEvent(orderNumber, "download-order", "failed", ex.Message);
                    log.LogMessage($"Error: {ex.Message}.");
                    SaveOrderWithError(ex.Message, order);
                    continue;
                }
                log.LogMessage($"Order {order.ProductionOrder.PONumber} was saved into work directory.");


                if (string.IsNullOrEmpty(order.ProductionOrder.Campaign))
                    throw new Exception("Campaign property is null or empty.");
                if (string.IsNullOrEmpty(order.ProductionOrder.Section_Text))
                    throw new Exception("SectionRfid property is null or empty.");
                if (string.IsNullOrEmpty(order.ProductionOrder.Brand_Text))
                    throw new Exception("BrandRfid property is null or empty.");
                if (string.IsNullOrEmpty(order.ProductionOrder.ProductType_Text))
                    throw new Exception("ProductTypeRfid property is null or empty.");
                if (order.ProductionOrder.QualityRfid==0)
                    throw new Exception("QualityRfid property can`t be zero.");
                if (order.ProductionOrder.ModelRfid == 0)
                    throw new Exception("ModelRfid property can`t be zero.");


                if (canSendToPrintCentral)
                {
                    foreach (var label in order.labels)
                    {
                        if (string.IsNullOrEmpty(label.Reference))
                            throw new Exception("Label reference property is null or empty.");
                        
                        var pluginType = label.Reference.Substring(0,3);
                        PublishProgressEvent(orderNumber, "send-qr-print-central", "in-progress", $"Sending QRs to Print Central for order {order.ProductionOrder.PONumber}.");
                        events.Send(new FileReceivedEvent
                        {
                            FilePath = filePath,
                            OrderNumber = order.ProductionOrder.PONumber.ToString(),
                            ProyectCode = order.ProductionOrder.Campaign,
                            PluginType = pluginType
                        });

                        PublishProgressEvent(orderNumber, "send-qr-print-central", "completed", $"QRs sent to Print Central for order {order.ProductionOrder.PONumber}.");
                        PublishProgressEvent(orderNumber, "send-file-print-central", "completed", $"File sent to Print Central for order {order.ProductionOrder.PONumber}.");
                        log.LogMessage($"File received event sent for order {order.ProductionOrder.PONumber}, with label reference{pluginType} ");
                    }
                }

                break;
            }
            if (message.IndexOf("not found in any queue", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                PublishProgressEvent(orderNumber, "search-order", "failed", message);
            }

            log.LogMessage(message);
            return message;
        }

        private void PublishProgressEvent(string orderNumber, string stepId, string status, string message)
        {
            events.Send(new OrderGetProgressEvent
            {
                OrderNumber = orderNumber,
                StepId = stepId,
                Status = status,
                Message = message
            });
        }

        protected virtual async Task<InditexOrderData> FetchOrderAsync(
            Credential credential,
            string orderNumber,
            string campaignCode,
            string vendorId)
        {
#if DEBUG
            var rootDirectory = Directory.GetCurrentDirectory();
            var orderPath = Path.Combine(rootDirectory, "TestOrders", "14313_14801_V26.json");
            var orderText = File.ReadAllText(orderPath);
            return JsonConvert.DeserializeObject<InditexOrderData>(orderText);
#else
            AuthenticationResult authResult = null;
            try
            {
                authResult = await OrderDownloadHelper.CallGetToken(appConfig, credential.User, credential.Password, apiCaller);
            }
            catch (Exception ex)
            {
                log.LogMessage($"Error: {ex.Message}.");
                return null;
            }

            return await CallGetOrderByNumber(authResult.id_token, orderNumber, campaignCode, vendorId);
#endif
        }

        protected virtual async Task<InditexOrderData> CallGetOrderByNumber(string token, string orderNumber, string campaignCode, string vendorId)
        {
            var controller = appConfig.GetValue<string>(DownloadServiceConfig.ControllerLabels, "api/v3/label-printing/supplier-data/search");
            var request = CreateLabelOrderRequest(orderNumber, campaignCode, vendorId);

            return await apiCaller.GetLabelOrders(controller, token, vendorId, request);
        }

        private static LabelOrderRequest CreateLabelOrderRequest(string orderNumber, string campaignCode, string vendorId)
        {
            if (!long.TryParse(orderNumber, out _))
                throw new FormatException("Order number must be numeric.");

            return new LabelOrderRequest
            {
                ProductionOrderNumber = orderNumber,
                Campaign = campaignCode,
                SupplierCode = vendorId
            };
        }

        private void SaveOrderWithError(string message, InditexOrderData order)
        {
            var title = $"Can't to load order for Inditex client.";

            var historyDirectory = appConfig.GetValue<string>(DownloadServiceConfig.HistoryDirectory);
            var fileName = string.Concat(DateTime.Now.ToString("ddMMyyyyhhmmss", CultureInfo.InvariantCulture), ".json");
            if (!Directory.Exists(historyDirectory))
                Directory.CreateDirectory(historyDirectory);

            var orderText = JsonConvert.SerializeObject(order, Formatting.Indented);

            File.WriteAllText(Path.Combine(historyDirectory, fileName), orderText);

            events.Send(new NotificationReceivedEvent
            {
                CompanyID = appConfig.GetValue<int>(DownloadServiceConfig.ProjectCompanyId),
                JsonData = orderText,
                Title = title,
                Message = message

            });
        }

    }
}
