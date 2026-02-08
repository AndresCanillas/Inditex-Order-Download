using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services.ImageManagement
{
    public class HttpImageDownloader : IImageDownloader
    {
        private readonly HttpClient client;

        public HttpImageDownloader()
        {
            client = new HttpClient();
        }

        public async Task<DownloadedImage> DownloadAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Url cannot be null or empty.", nameof(url));

            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsByteArrayAsync();
                return new DownloadedImage
                {
                    Content = content,
                    ContentType = response.Content.Headers.ContentType?.MediaType
                };
            }
        }
    }
}
