using Newtonsoft.Json;
using OrderDonwLoadService.Model;
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
        Task<InditexOrderData> GetLabelOrders(string controller, string token, LabelOrderRequest request);
        Task<AuthenticationResult> GetToken(string url, string user, string password, string scope);
    }
    public class ApiCallerService : IApiCallerService
    {
        private HttpClient httpClient;
        private readonly HttpClient tokenClient = new HttpClient
        {
            Timeout = new TimeSpan(0, 20, 0)
        };



        public void Start(string url)
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.Timeout = new TimeSpan(0, 20, 0);
        }


        public async Task<InditexOrderData> GetLabelOrders(string controller, string token, LabelOrderRequest request)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");
            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if(!String.IsNullOrWhiteSpace(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var jsonBody = JsonConvert.SerializeObject(request);
                using(var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                using(HttpResponseMessage response = await httpClient.PostAsync(controller, content))
                {
                    response.EnsureSuccessStatusCode();
                    var value = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<InditexOrderData>(value);
                }
            }
            else
            {
                return default;
            }

        }
        public async Task<AuthenticationResult> GetToken(string url, string user, string password, string scope)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                })
            };

            request.Headers.UserAgent.ParseAdd("BusinessPlatform/1.0");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{user}:{password}")));

            var response = await tokenClient.SendAsync(request);


            response.EnsureSuccessStatusCode();
            var resp = JsonConvert.DeserializeObject<AuthenticationResult>(await response.Content.ReadAsStringAsync());

            return await Task.FromResult(resp);


        }
       
    }


}
