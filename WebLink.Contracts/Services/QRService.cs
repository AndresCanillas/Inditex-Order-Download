using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class QRService
    {
        private readonly IOrderImageRepository _orderImageRepository;

        public QRService(IOrderImageRepository orderImageRepository)
        {
            _orderImageRepository = orderImageRepository;
        }

        public async Task GenerateQrAsync(string text)
        {
            int orderID = 0; 
            int.TryParse(text, out orderID);     

            string name = GenerateNameForQr(text);
            var response = await CreateQrAsync(text, name);
            var image = await DownloadQrAsync(response.Id);
            var result = await UploadQrAsync(image,  orderID);
        }

        private async Task<bool> UploadQrAsync(QRRequestImage image, int orderId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var imageData = await client.GetByteArrayAsync(image.Urls.Png);
                    var imageContent = new ByteArrayContent(imageData);

                    using (var stream = new MemoryStream(await imageContent.ReadAsByteArrayAsync()))
                    {
                        using (FileStream fileStream = System.IO.File.Create("qr.jpg"))
                        {
                            var result = _orderImageRepository
                                          .UploadImage(orderId, $"{image.Name}.png", stream);

                            _orderImageRepository.UpdateImageMetadata(result);

                        }
                    }
                    return true;
                }
            }
            catch { return false; }
        }
        private string GenerateNameForQr(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            return $"{text}_";
        }

        private async Task<QRResponse> CreateQrAsync(string text, string name)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(text));
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "43a3e83c91fd921868db3954a2ec357cbc6eda9f");
                StringContent body = CreateBodyRequestForGenerateQr(text, name);

                var response = await client.PostAsync($"https://api.beaconstac.com/api/2.0/qrcodes/", body);
                if (!response.IsSuccessStatusCode && response.Content != null)
                    throw new HttpRequestException(
                        $"Error de petición en la API\nCódigo de error: {response.StatusCode}\nMensaje: {response.RequestMessage}"
                    );

                var message = await response.Content.ReadAsStringAsync();
                var deserialize = JsonConvert.DeserializeObject<QRResponse>(message);

                return deserialize;
            }
        }

        private async Task<QRRequestImage> DownloadQrAsync(long id)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "43a3e83c91fd921868db3954a2ec357cbc6eda9f");

                    var response = await client.GetAsync(
                        $"https://api.beaconstac.com/api/2.0/qrcodes/{id}/download/?size=1080&error_correction_level=5&canvas_type=png");

                    if (!response.IsSuccessStatusCode && response.Content != null)
                        throw new HttpRequestException(
                            $"Error de petición en la API\nCódigo de error: {response.StatusCode}\nMensaje: {response.RequestMessage}"
                        );

                    var message = await response.Content.ReadAsStringAsync();
                    var deserialize = JsonConvert.DeserializeObject<QRRequestImage>(message);

                    return deserialize;
                }
            }
            catch (HttpRequestException ex) { throw ex; }
            catch (Exception ex) { throw ex; }
        }

        private StringContent CreateBodyRequestForGenerateQr(string text, string name)
        {
            var body = new QRRequestBody
            {
                name = name,
                organization = 396365,
                qr_type = 1,
                fields_data = new FieldsData
                {
                    qr_type = 1,
                    // url = $"{"https://www.zara.com/qr/"}{text}"
                    url = $"{text}"
                },
                attributes = new Attributes
                {
                    color = "#000",
                    colorDark = "#000",
                    logoImage = "https://i.ibb.co/VgZFXsS/LOGO-QR.png",
                    logoScale = 0.2,
                    frameColor = "#000",
                    dataPattern = "kite",
                    eyeBallShape = "square",
                    eyeFrameColor = "#000"
                }
            };

            var serialize = JsonConvert.SerializeObject(body);

            return new StringContent(serialize, Encoding.UTF8, "application/json");
        }

        public class QRResponse
        {
            public long Id { get; set; }
        }
        public class QRRequestBody
        {
            public string name { get; set; }
            public int organization { get; set; }
            public int qr_type { get; set; }
            public FieldsData fields_data { get; set; }
            public Attributes attributes { get; set; }
        }
        public class FieldsData
        {
            public int qr_type { get; set; }
            public string url { get; set; }
        }

        public class Attributes
        {
            public string color { get; set; }
            public string colorDark { get; set; }
            public string logoImage { get; set; }
            public double logoScale { get; set; }
            public string frameColor { get; set; }
            public string dataPattern { get; set; }
            public string eyeBallShape { get; set; }
            public string eyeFrameColor { get; set; }
            public string eyeFrameShape { get; set; }
        }
        public partial class QRRequestImage
        {
            public Urls Urls { get; set; }
            public string Name { get; set; }
        }
        public partial class Urls
        {
            public Uri Png { get; set; }
        }
    }



}
