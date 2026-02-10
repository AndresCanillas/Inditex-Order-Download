using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderDonwLoadService.Model;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderDonwLoadService.Services
{

    public class PrintCentralService : BaseServiceClient, IPrintCentralService
    {
        private const string ProjectImageExistsEndpointTemplate = "api/images/projects/{0}/barcode/{1}/exists";
        private const string ProjectImageUploadEndpointTemplate = "api/images/projects/{0}/barcode/{1}";
        public PrintCentralService(IAppConfig cfg)
        {
            Url = cfg["DownloadServices.PrintCentralUrl"];
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

        public async Task<string> LogoutAsync()
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

            using(HttpResponseMessage response = await client.GetAsync(Url + controller))
            {
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

        public async Task<User> GetUserAsync(string username)
        {
            return await GetAsync<User>($"/users/getbyname/{username}");
        }

        public async Task<List<Brand>> GetBrandAsync(int companyId)
        {
            return await GetAsync<List<Brand>>($"/brands/getbycompanyid/{companyId}");
        }

        public async Task<List<Project>> GetByBrandID(int brandId)
        {
            var projects = await GetAsync<List<Project>>($"/projects/getbybrand/{brandId}/{false}");
            return projects.Where(x => x.EnableFtpFolder).ToList();
        }

        public async Task<List<Catalog>> GetCatalogsByProjectID(int projectId)
        {
            return await GetAsync<List<Catalog>>($"/catalogs/getbyproject/{projectId}/{true}");
        }

        public async Task<List<CatalogData>> GetCatalogDataByID(int catalogid)
        {
            var data = await GetAsync<List<Object>>($"/catalogdata/getbycatalog/{catalogid}");
            return new List<CatalogData>();
        }

        public async Task<Output> FtpServiceUpload<Input, Output>(string controller, Input input, string filePath, string fileName)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");
            if(controller.StartsWith("/"))
                controller = controller.Substring(1);
            if(!String.IsNullOrWhiteSpace(Token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            var fileInfo = new FileInfo(filePath);
            FileStream fileStream = File.OpenRead(filePath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
            streamContent.Headers.ContentDisposition.Name = $"\"file\"";
            streamContent.Headers.ContentDisposition.FileName = "\"" + fileName + "\"";
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            string boundary = Guid.NewGuid().ToString();
            var content = new MultipartFormDataContent(boundary);
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
            content.Add(streamContent);


            var str = JsonConvert.SerializeObject(input);
            var dataContent = new StringContent(str, Encoding.UTF8, "application/json");
            content.Add(dataContent, "OrderData");


            var cts = new CancellationTokenSource(new TimeSpan(0, 120, 0));
            using(HttpResponseMessage response = await client.PostAsync(Url + controller, content, cts.Token))
            {
                if(response.IsSuccessStatusCode)
                {
                    string message = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Output>(message);
                }
                else
                {
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }

        public async Task<List<CompanyOrderDTO>> GetOrder(int companyId, int projectId, string orderNumber)
        {

            var request = new OrderReportFilter()
            {
                ProjectId = projectId,
                OrderNumber = orderNumber,
                OrderDate = DateTime.MinValue,
                OrderDateTo = DateTime.Now,
                CompanyID = companyId
            };

            var orders = await InvokeAsync<OrderReportFilter, OperationResult>("groups/orders/getreport", request);

            if(!orders.Success)
                return null;

            var data = (JObject)orders.Data;
            var records = data.Properties().FirstOrDefault(x => x.Name.Equals("Records")).First();

            return JsonConvert.DeserializeObject<List<CompanyOrderDTO>>(records.ToString());
        }

        public async Task<bool> CreateFile(string storeName, int fileid, string filename)
        {

            var result = await InvokeAsync<OperationResult>($"/fsm/{storeName}/files/{fileid}/create/{filename}");

            return result.Success;
        }

        public async Task<int> CreateAttachment(string storeName, int fileid, string category, string filename)
        {

            var attachment = await InvokeAsync<OperationResult>($"/fsm/{storeName}/files/{fileid}/{category}/create/{filename}");
            var data = (JObject)attachment.Data;

            return data.Properties().Any(x => x.Name == "AttachmentID")
                    ? (int)data.Properties().FirstOrDefault(x => x.Name == "AttachmentID").Value
                    : 0;
        }

        public async Task SetAttachmentContent(string storeName, int fileid, string category, int attachmentId, string filePath)
        {
            var attachment = await UploadFileAsync<OperationResult>($"/fsm/{storeName}/files/{fileid}/{category}/{attachmentId}", filePath);
        }


        public async Task<bool> ProjectImageExistsAsync(int projectId, string barcode)
        {
            if (projectId <= 0)
                throw new ArgumentException("projectId must be greater than zero.", nameof(projectId));
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("barcode cannot be null or empty.", nameof(barcode));

            var endpoint = string.Format(ProjectImageExistsEndpointTemplate, projectId, barcode);
            var result = await InvokeAsync<OperationResult>(endpoint);
            if (!result.Success || result.Data == null)
                return false;

            if (result.Data is bool exists)
                return exists;

            if (bool.TryParse(result.Data.ToString(), out var parsed))
                return parsed;

            return false;
        }

        public async Task UploadProjectImageAsync(int projectId, string barcode, byte[] content, string fileName)
        {
            if (projectId <= 0)
                throw new ArgumentException("projectId must be greater than zero.", nameof(projectId));
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("barcode cannot be null or empty.", nameof(barcode));
            if (content == null || content.Length == 0)
                throw new ArgumentException("content cannot be null or empty.", nameof(content));

            if (!String.IsNullOrWhiteSpace(Token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

            var controller = string.Format(ProjectImageUploadEndpointTemplate, projectId, barcode);
            var boundary = Guid.NewGuid().ToString();
            var multipart = new MultipartFormDataContent(boundary);
            multipart.Headers.Remove("Content-Type");
            multipart.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

            var stream = new MemoryStream(content);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = "\"" + (string.IsNullOrWhiteSpace(fileName) ? barcode + ".svg" : fileName) + "\""
            };
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(streamContent);

            var response = await client.PostAsync(Url + controller, multipart);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
        }

        Task IPrintCentralService.LoginAsync(string loginUrl, string userName, string password)
        {
            return base.LoginAsync(loginUrl, userName, password);
        }
    }
}
