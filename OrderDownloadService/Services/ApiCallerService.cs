using Newtonsoft.Json;
using OrderDonwLoadService.Model;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OrderDonwLoadService.Services
{

    public interface IApiCallerService
    {
        void Start(string url);
        Task<InditexOrderData> GetLabelOrders(string controller, string token, string vendorId);
        Task<AutenticationResult> GetToken(string url, string user, string password);
        Task<InditexOrderXmlResponse> GetPurchaseOrder(string relativeUrl, string bearerToken);
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


        public async Task<InditexOrderData> GetLabelOrders(string controller, string token, string vendorId)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");
            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            httpClient.DefaultRequestHeaders.Clear();
            if(!String.IsNullOrWhiteSpace(token))
            {
                httpClient.DefaultRequestHeaders.Add("x-vendorid", vendorId);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                using(HttpResponseMessage response = await httpClient.GetAsync(controller))
                {
                    response.EnsureSuccessStatusCode();
                    var value = await response.Content.ReadAsStringAsync();
                    if(!string.IsNullOrEmpty(value) && value != "No hay mensajes para recoger." && value != "There is no messages to retrieve.")
                    {
                        var resp = JsonConvert.DeserializeObject<InditexOrderData>(await response.Content.ReadAsStringAsync());
                        return await Task.FromResult(resp);

                    }
                    else
                    {
                        return default;
                    }
                }
            }
            else
            {
                return default;
            }

        }
        public async Task<AutenticationResult> GetToken(string url, string user, string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", user),
                    new KeyValuePair<string, string>("client_secret", password),
                    new KeyValuePair<string, string>("scope", "inditex")
                })
            };

            var response = await tokenClient.SendAsync(request);


            response.EnsureSuccessStatusCode();
            var resp = JsonConvert.DeserializeObject<AutenticationResult>(await response.Content.ReadAsStringAsync());

            return await Task.FromResult(resp);


        }
        public async Task<InditexOrderXmlResponse> GetPurchaseOrder(string fullUrl, string bearerToken)
        {
            if(string.IsNullOrWhiteSpace(fullUrl))
                throw new ArgumentException("URL cannot be null or empty.", nameof(fullUrl));

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using(var response = await httpClient.GetAsync(fullUrl))
            {
                response.EnsureSuccessStatusCode();
                var xmlContent = await response.Content.ReadAsStringAsync();

                var serializer = new XmlSerializer(typeof(InditexOrderXmlResponse));
                using(var reader = new System.IO.StringReader(xmlContent))
                {
                    var result = (InditexOrderXmlResponse)serializer.Deserialize(reader);
                    return result;
                }
            }
        }

    }


}
