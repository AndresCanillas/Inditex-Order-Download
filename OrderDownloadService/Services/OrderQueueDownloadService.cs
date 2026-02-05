using Newtonsoft.Json;
using OrderDonwLoadService.Model;
using Polly;
using Service.Contracts;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services
{

    public interface IOrderQueueDownloadService
    {
        bool Start();
        bool Stop();
    }
    public class OrderQueueDownloadService : IOrderQueueDownloadService
    {
        private readonly IAppConfig appConfig;
        private readonly IApiCallerService apiCaller;
        private readonly IEventQueue events;
        private readonly IAppLog log;

        private bool wait = false;
        private System.Timers.Timer timerService = new System.Timers.Timer();
        private double timerPlayInterval = 0;
        private Dictionary<string, TokenInfo> tokens = new Dictionary<string, TokenInfo>();
        private int deviceID = 0;
        private string url = null;
        private bool onApiCaller = false;
        private string workDirectory = null;
        private string historyDirectory = null;
        private class TokenInfo
        {
            public string Token { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
        public OrderQueueDownloadService(IApiCallerService apiCaller,
            IAppConfig appConfig,
            IAppLog log,
            IAppInfo appInfo,
            IEventQueue events

            )
        {
            this.apiCaller = apiCaller;
            this.appConfig = appConfig;
            this.log = log;
            this.events = events;

            timerPlayInterval = this.appConfig.GetValue<double>("DownloadServices.TimerPlayInterval", 20);
            workDirectory = this.appConfig.GetValue<string>("DownloadServices.WorkDirectory", Directory.GetCurrentDirectory() + "/WorkDirectory");
            historyDirectory = this.appConfig.GetValue<string>("DownloadServices.HistoryDirectory", Directory.GetCurrentDirectory() + "/HistoryDirectory");
            log.InitializeLogFile(Path.Combine(appInfo.SystemLogDir, "OrderDonwLoadService.log"), this.appConfig.GetValue<int>("MaxLogSize", 4194304));
        }

        public bool Start()
        {
            try
            {
                if(onApiCaller)
                    return true;
                url = appConfig.GetValue<string>("DownloadServices.ApiUrl", "https://api2.Inditex.com/");
                timerService.Enabled = true;
                timerService.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
                timerService.Interval = timerPlayInterval;
                apiCaller.Start(url);
                return onApiCaller = true;

            }
            catch(Exception)
            {
                return false;
            }
        }
        public bool Stop()
        {
            try
            {
                timerService.Enabled = false;
                timerService.Elapsed -= new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
                log.LogMessage("Service stopped");
                onApiCaller = false;
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
        public void Dispose()
        {
            this.Stop();
        }
        private async void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {


            if(!onApiCaller) return;
            if(wait) return;
            try
            {
                wait = true;
                var maxTrys = appConfig.GetValue<int>("DownloadServices.MaxTrys", 2);
                var timeToWait = TimeSpan.FromSeconds(appConfig.GetValue<double>("DownloadServices.SecondsToWait", 240));
                foreach(var credential in OrderDownloadHelper.LoadInditexCreadentials(log))
                {
                    log.LogMessage($"Beging of Timer  RequestsServices with this configuration for credential ({credential.Name}):");

                    if(!onApiCaller) return;
#if DEBUG
                    var token = "test";
#else
                    var token = string.Empty;
                    try
                    {
                        token = await GetInditexToken(credential);
                    }
                    catch(Exception ex)
                    {
                        log.LogMessage($"Error: {ex.Message}.");
                        continue;
                    }
#endif
                    do
                    {
                        if(!onApiCaller) return;
#if DEBUG
                        var rootDirectory = Directory.GetCurrentDirectory();
                        rootDirectory = rootDirectory.Replace("OrderDownloadWebApi", "OrderDownloadService");
                        var _order = File.ReadAllText(rootDirectory + @"\TestOrders\4500577163.json");
                        var order = JsonConvert.DeserializeObject<InditexOrderData>(_order);
#else
                        InditexOrderData order = null;
                        try
                        {
                            order = await CallGetLabelOrders(token, credential.Vendorid);


                            if(order == null)
                            {
                                log.LogMessage($"Not found more orders into Inditex Web API for {credential.Name}.");
                                break;
                            }
                        }
                        catch(Exception ex)
                        {
                            log.LogMessage($"Error: {ex.Message}.");
                            break;
                        }
#endif

                        log.LogMessage($"Order {order.POInformation.productionOrderNumber} in process.");

                        var filePath = "";
                        try
                        {
                            filePath = OrderDownloadHelper.SaveFileIntoWorkDirectory(order, workDirectory);
                        }
                        catch(Exception ex)
                        {
                            log.LogMessage($"Error: {ex.Message}.");
                            SaveOrderWithError(ex.Message, order);
                            continue;
                        }


                        log.LogMessage($"Order {order.POInformation.productionOrderNumber} was saved into work directory.");

                        //if(order.StyleColor == null || !order.StyleColor.Any())
                        //{
                        //    var message = $"Error: Order {order.POInformation.productionOrderNumber} not have StyleColor property.";
                        //    log.LogMessage(message);
                        //    SaveOrderWithError(message, order);
                        //    continue;
                        //}

                        //var referenceID = order.StyleColor.First().ReferenceID;

                        //if(string.IsNullOrEmpty(referenceID))
                        //{
                        //    var message = $"ERROR: Order {order.LabelOrder.OrderNumber} not have ReferenceID property.";
                        //    log.LogMessage(message);
                        //    SaveOrderWithError(message, order);
                        //    continue;
                        //}

                        events.Send(new FileReceivedEvent
                        {
                            FilePath = filePath,
                            OrderNumber = order.POInformation.productionOrderNumber.ToString(),
                            SeasonId = order.POInformation.campaign,
                        });

                        Task.Delay(15000).Wait();
#if DEBUG
                        break;
#endif

                    } while(true);

                }
                wait = false;
            }
            catch(Exception ex)
            {
                var message = "Finish whit mistake: \n\r Message: " + ex.Message + "; \n\r InnerException: " + ex.InnerException +
                   "; \n\r Source: " + ex.Source + "; \n\r StackTrace: " + ex.StackTrace + "; \n\r TargetSite:" + ex.TargetSite;
                log.LogMessage(message);
                wait = false;
            }

        }

        private void SaveOrderWithError(string message, InditexOrderData order)
        {
            var title = $"Can't to load order for Inditex client.";

            var historyDitectory = appConfig.GetValue<string>("DownloadServices.HistoryDirectory");
            var fileName = string.Concat(DateTime.Now.ToString("ddMMyyyyhhmmss", CultureInfo.InvariantCulture), ".json");
            if(!Directory.Exists(historyDitectory))
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

        private async Task<string> GetInditexToken(Credential credencial)
        {
            if(!tokens.TryGetValue(credencial.Vendorid, out TokenInfo info))
            {
                if(info.ExpiresAt > DateTime.Now)
                    return await Task.FromResult(info.Token);

                log.LogMessage($" token expired.");
            }
            else
            {
                log.LogMessage($" token is null.");
            }
            return await GetToken();

            async Task<string> GetToken()
            {
                log.LogMessage($" Get try token");
                string controllerToken = appConfig.GetValue<string>("DownloadServices.ControllerToken", "oauth2/default/v1/token");

                var tokeResult = await OrderDownloadHelper.CallGetToken(appConfig, credencial.User, credencial.Password, apiCaller);


                if(string.IsNullOrWhiteSpace(tokeResult.access_token))
                {
                    tokens.Remove(credencial.Vendorid);
                    throw new NullReferenceException($" token not found of Url ({controllerToken})");
                }

                log.LogMessage($"the token got succes of Url ({controllerToken})");

                var expiresAt = DateTime.Now.AddSeconds(tokeResult.expires_in - 1600);

                log.LogMessage($" token expries date ({expiresAt})");

                tokens[credencial.Vendorid] = new TokenInfo
                {
                    Token = tokeResult.access_token,
                    ExpiresAt = expiresAt
                };

                return await Task.FromResult(tokeResult.access_token);
            }

        }

        private async Task<InditexOrderData> CallGetLabelOrders(string token, string vendorId)
        {
            var controllerLabels = appConfig.GetValue<string>("DownloadServices.ControllerLabels", "prod-p-labels/labels");
            var maxTrys = appConfig.GetValue<int>("DownloadServices.MaxTrys", 2);
            var timeToWait = TimeSpan.FromSeconds(appConfig.GetValue<double>("DownloadServices.SecondsToWait", 240));

            if(!onApiCaller)
                return default;
            var retryPolity = Policy.Handle<Exception>().WaitAndRetryAsync(maxTrys - 1, i => timeToWait);
            var authenticationresult = await retryPolity.ExecuteAsync
            (
                 async () => await apiCaller.GetLabelOrders(controllerLabels, token, vendorId)
            );

            return await Task.FromResult(authenticationresult);
        }
    }
}
