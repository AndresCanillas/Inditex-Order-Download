using Newtonsoft.Json;
using OrderDonwLoadService.Model;
using Service.Contracts;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services
{
    public interface IApiCallerService
    {
        void Start(string url);
        Task<InditexOrderData> GetLabelOrders(string controller, string token, string vendorId, LabelOrderRequest request);
        Task<AuthenticationResult> GetToken(string url, string user, string password, string scope);
    }

    public class ApiCallerService : BaseServiceClient, IApiCallerService
    {
        private const string BusinessPlatformUserAgent = "BusinessPlatform/1.0";

        private readonly HttpClient tokenClient = new HttpClient
        {
            Timeout = new TimeSpan(0, 20, 0)
        };

        public void Start(string url)
        {
            Url = url;
        }

        public async Task<InditexOrderData> GetLabelOrders(string controller, string token, string vendorId, LabelOrderRequest request)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(request == null)
                throw new Exception("request argument cannot be null");


            if(String.IsNullOrWhiteSpace(token))
                return default;

            Token = token;
            var headers = new Dictionary<string, string>
            {
                ["User-Agent"] = BusinessPlatformUserAgent
            };

            //if (!String.IsNullOrWhiteSpace(vendorId))
            //    headers["x-vendorid"] = vendorId;

            return await PostAsync<LabelOrderRequest, InditexOrderData>(controller, request, headers);
        }

        public async Task<AuthenticationResult> GetToken(string url, string user, string password, string scope)
        {
            var content = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            };

            using(var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(content)
            })
            {
                request.Headers.UserAgent.ParseAdd(BusinessPlatformUserAgent);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));

                using(var response = await tokenClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var serializedResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<AuthenticationResult>(serializedResponse);
                }
            }
        }
    }
}
