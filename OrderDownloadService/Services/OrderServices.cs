using OrderDonwLoadService.Model;
using OrderDonwLoadService.Services;
using Polly;
using Service.Contracts;
using System;
using System.Threading.Tasks;


namespace OrderDonwLoadService.Synchronization
{
    public interface IOrderServices
    {
        Task<string> GetBackOnTheQueue(string number);
    }

    public class OrderServices : IOrderServices
    {
        private readonly IAppConfig appConfig;
        private readonly IAppLog log;
        private readonly IApiCallerService apiCaller;

        public OrderServices(IAppConfig appConfig, IAppLog log, IApiCallerService apiCaller)
        {
            this.appConfig = appConfig;
            this.log = log;
            this.apiCaller = apiCaller;
            var url = this.appConfig.GetValue<string>("DownloadServices.ApiUrl", "https://api2.Inditex.com/");
            this.apiCaller.Start(url);
        }


        public async Task<string> GetBackOnTheQueue(string number)
        {
            string message = $"Order number ({number}) not found in any queue.";

            var credentials = OrderDownloadHelper.LoadInditexCreadentials(log);
            if (credentials == null || credentials.Count == 0)
            {
                log.LogMessage("No credentials found for Inditex API.");
                return "Error No credentials found for Inditex API.";
            }

            foreach (var credential in credentials)
            {

                log.LogMessage($"Searching for Order number ({number}) in {credential.Name} queue.");

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

                var response = await CallGetPurchaseOrderbyNumer(authResult.access_token, number);
                if (response.Type == "S")
                {
                    log.LogMessage($"Order number ({number}) found in {credential.Name} queue.");
                    return $"Order number ({number}) found in {credential.Name} queue.";
                }
                message = string.Concat($"Error retrieving order number from {credential.Name} queue, response: {response.Message}", "\n\r", message);

            }
            log.LogMessage(message);
            return message;
        }

        private async Task<InditexOrderXmlResponse> CallGetPurchaseOrderbyNumer(string token, string orderNumber)
        {

            string controllerOrder = appConfig.GetValue<string>("DownloadServices.ControllerOrder", "prod-p-labels/orders");
            var maxTrys = this.appConfig.GetValue<int>("DownloadServices.MaxTrys", 2);
            var timeToWait = TimeSpan.FromSeconds(this.appConfig.GetValue<double>("DownloadServices.SecondsToWait", 240));
            controllerOrder = string.Concat(controllerOrder, "/", orderNumber);

            var retryPolity = Policy.Handle<Exception>().WaitAndRetryAsync(maxTrys - 1, i => timeToWait);
            var InditexResponse = await retryPolity.ExecuteAsync
            (
                 async () => await apiCaller.GetPurchaseOrder(controllerOrder, token)
            );

            if (InditexResponse == null || string.IsNullOrEmpty(InditexResponse.Type))
            {
                throw new InvalidOperationException($"No order found for number: {orderNumber}.");
            }
            log.LogMessage($"Order number ({orderNumber}) found successfully.");

            return InditexResponse;
        }
    }
}