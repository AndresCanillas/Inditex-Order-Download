using Service.Contracts;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OrderDownloadWebApi.Services
{
    public interface IPrintCentralClient
    {
        string Url { get; set; }
        string Token { get; }
        bool Authenticated { get; }
        DateTime ExpirationDate { get; }
        void Login(string loginUrl, string userName, string password);
        Task LoginAsync(string loginUrl, string userName, string password);
        string Logout();
        void Connect();
        void Disconnect();


    }

    public class CentralClient : BaseServiceClient, IPrintCentralClient
    {
        private IAppConfig cfg;
        private IAppLog log;
        private Guid RandomGUI;

        public CentralClient(IAppConfig cfg, IAppLog log)
        {
            this.cfg = cfg;
            this.log = log.GetSection("CentralClient");
            Url = cfg["PrintEntryWeb.PrintCentralUrl"];
            RandomGUI = Guid.NewGuid();
        }

        public void Connect()
        {
            if(!Authenticated)
            {
                var user = cfg.GetValue<string>("PrintEntryWeb.Credentials.User");
                var password = cfg.GetValue<string>("PrintEntryWeb.Credentials.Password");
                Login("/", user, password);
                log.LogWarning($"Token Connect: {Token} - Guid {RandomGUI}");

            }
        }

        public void Disconnect()
        {
            Logout();
        }

        public string Logout()
        {
            string controller = "/Account/Logout";

            if(controller.StartsWith("/"))
            {
                controller = controller.Substring(1);
            }

            if(!String.IsNullOrWhiteSpace(Token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            }

            using(HttpResponseMessage response = client.GetAsync(Url + controller).Result)
            {
                log.LogWarning($"Token Disconnect: '{Token}' - Guid {RandomGUI}");

                if(response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }
    }
}


