using Newtonsoft.Json;
using Service.Contracts;
using System.Net.Http;
using System.Text;
using WebLink.Contracts.Services.Ship24;

namespace WebLink.Services.Ship24
{
    public class Ship24ClientService : IShip24ClientService
    {
        private string api_key;
        private bool isActive;
       
        public Ship24ClientService(IAppConfig config)
        {
            api_key = config.GetValue<string>("WebLink.Ship24.ApiKey");
            isActive = config.GetValue<bool>("WebLink.Ship24.IsActive");  
        }

        public string CreateTrackerAndGetTrackingResults(Ship24TrackingInfo trackingInfo)
        {
            if (!isActive || string.IsNullOrEmpty(api_key))
            {
                return null;
            }   

            using(var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {api_key}");

                string url = "https://api.ship24.com/public/v1/trackers/track";

                var json = trackingInfo != null ? JsonConvert.SerializeObject(trackingInfo) : "{}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(url, content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public string CreateTracker(Ship24TrackingInfo trackingInfo)
        {
            if(!isActive || string.IsNullOrEmpty(api_key))
            {
                return null;
            }

            using(var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {api_key}");

                string url = "https://api.ship24.com/public/v1/trackers";

                var json = trackingInfo != null ? JsonConvert.SerializeObject(trackingInfo) : "{}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(url, content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
