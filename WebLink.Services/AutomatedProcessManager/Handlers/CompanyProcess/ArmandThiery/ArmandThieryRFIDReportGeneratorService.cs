using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;
using WebLink.Contracts.Services;

namespace WebLink.Services
{
    public class ArmandThieryRFIDReportGeneratorService : IArmandThieryRFIDReportGeneratorService
    {
        private readonly ArmandThieryRifConfig config;
        private readonly IAppLog log;
        private readonly IFactory factory;
        private readonly IEncodedLabelRepository encodeLabelRepo;
        private readonly IOrderRepository orderRepo;

        public ArmandThieryRFIDReportGeneratorService(IAppConfig config, IAppLog log, IFactory factory, IEncodedLabelRepository encodeLabelRepo, IOrderRepository orderRepo)
        {
            this.config = config.Bind<ArmandThieryRifConfig>("CustomSettings.ArmandThiery");
            this.log = log;
            this.factory = factory;
            this.encodeLabelRepo = encodeLabelRepo;
            this.orderRepo = orderRepo;
        }

        public void SendReport(int companyID)
        {
            GetEpcs(companyID);
        }

        private void GetEpcs(int companyID)
        {
            DateTime now = DateTime.Now;
            DateTime yesterday = now.AddDays(-1);
            List<EPCReport> epcs = new List<EPCReport>();

            var closedOrdersIds = encodeLabelRepo.GetOrderIDEncodeBetweenDates(companyID, yesterday, now).ToList();

            closedOrdersIds.ToList().ForEach(orderID =>
            {
                IEnumerable<IEncodedLabel> found = new List<IEncodedLabel>();
                long lastEpcID = 0;
                var co = orderRepo.GetByID(orderID, true);
                do
                {

                    found = encodeLabelRepo.GetForPendingReverseFlowSortedByID(co.ID, 1000, lastEpcID);
                    if(found.Any())
                        lastEpcID = found.Last().ID;

                    //Log($"Current Order {co.ID} found : '{found.Count()}'");

                    found.ToList()
                    .ForEach(ean =>
                    {
                        var epcReport = new EPCReport
                        {
                            OrderNumber = co.OrderNumber,
                            Barcode = ean.Barcode,
                            Epc = ean.EPC,
                            Date = ean.Date.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        epcs.Add(epcReport);
                    });
                } while(found.Any());

            });

            if(epcs.Count > 0)
            {
                SendToAPI(epcs);
                //Close Orders
                closedOrdersIds.ToList().ForEach(orderID => encodeLabelRepo.MarkAsProcessedInReport(orderID));
            }
        }

        private async void SendToAPI(List<EPCReport> epcs)
        {
            var responseContent = string.Empty;
            var token = string.Empty;
            using(HttpClient client = new HttpClient())
            {
                var succesfulLogin = await LoginAPI(client);

                if(succesfulLogin.IsSuccessStatusCode)
                {
                    responseContent = await succesfulLogin.Content.ReadAsStringAsync();
                    var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
                    token = loginResponse?.Data?.Token;
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var epcsGrouped = GetBodyRequest(epcs);

                foreach(var order in epcsGrouped)
                {
                    string serialize = JsonConvert.SerializeObject(order);
                    var bodyJson = new StringContent(serialize, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{config.SendReportUrl}", bodyJson);

                    if(!response.IsSuccessStatusCode && response.Content != null)
                        throw new HttpRequestException($"Armand Thiery Report EPCs: Error de petición en la API\nCódigo de error: {response.StatusCode}\nMensaje: {response.RequestMessage}");
                    else
                        log.LogMessage($"Armand Thiery Report EPCs Sent to {config.SendReportUrl} successful!");
                }
            }
        }

        private List<OrderRequest> GetBodyRequest(List<EPCReport> epcs)
        {
            var grouped = epcs
                .GroupBy(e => e.OrderNumber)
                .Select(g => new OrderRequest
                {
                    num_cde = g.Key,
                    data = g.Select(ean => new EPCData
                    {
                        date_encodage = ean.Date,
                        epc = ean.Epc,
                        ean = ean.Barcode
                    }).ToList()
                })
                .ToList();

            return grouped;
        }

        private async Task<HttpResponseMessage> LoginAPI(HttpClient client)
        {
            var request = new
            {
                username = config.UsernameAPI,
                password = config.PasswordAPI
            };

            var serialize = JsonConvert.SerializeObject(request);
            var body = new StringContent(serialize, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{config.LoginApi}", body);

            return response;
        }
    }

    internal class ArmandThieryRifConfig
    {
        public bool Enabled;
        public int CompanyID;
        public string FileExtension;
        public string LoginApi;
        public string SendReportUrl;
        public string UsernameAPI;
        public string PasswordAPI;
    }

    public class LoginResponse
    {
        public bool Error { get; set; }
        public Data Data { get; set; }
        public User User { get; set; }
    }

    public class Data
    {
        public string Token { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Telephone { get; set; }
        public List<string> Roles { get; set; }
    }
    public class EPCReport
    {
        public string OrderNumber { get; set; }
        public string Barcode { get; set; }
        public string Epc { get; set; }
        public string Date { get; set; }

    }
    public class OrderRequest
    {
        public string num_cde { get; set; }
        public List<EPCData> data { get; set; }
    }

    public class EPCData
    {
        public string date_encodage { get; set; }
        public string epc { get; set; }
        public string ean { get; set; }
    }
}
