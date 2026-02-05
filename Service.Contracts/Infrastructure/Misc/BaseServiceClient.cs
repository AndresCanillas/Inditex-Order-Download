using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts
{
    public abstract class BaseServiceClient
    {
        public static HttpClient client;

        public static string AcceptThumbprint { get; set; }

        static BaseServiceClient()
        {
#if NET461
			var handler = new WebRequestHandler();
			handler.ServerCertificateValidationCallback += ValidateRemoteCertificate;
#else
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = ValidateRemoteCertificate;
#endif
            client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMinutes(30);
        }


#if NET461
		protected static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
#else
        protected static bool ValidateRemoteCertificate(HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors policyErrors)
#endif
        {
            if(policyErrors == SslPolicyErrors.None)
                return true;
            if(String.IsNullOrWhiteSpace(AcceptThumbprint))
                return true;
            else if(String.Compare((cert as X509Certificate2).Thumbprint, AcceptThumbprint, true) == 0)
                return true;
            return false;
        }


        private string token;
        private string url;
        private DateTime expDate = DateTime.MinValue;
        private JsonSerializerSettings camelCaseSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
        };

        public virtual string Url
        {
            get { return url; }
            set
            {
                url = value;
                if(url == null) return;
                if(!url.EndsWith("/"))
                    url += "/";
            }
        }

        public bool UseCamelCase { get; set; }

        public string Token
        {
            get => token;
            set => token = value;
        }

        public DateTime ExpirationDate
        {
            get => expDate;
        }

        public bool Authenticated { get => token != null && expDate > DateTime.Now; }

        public virtual void Login(string controller, string userName, string password)
        {
            token = null;
            expDate = DateTime.MinValue;
            byte[] authData = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", userName, password));
            string authParam = Convert.ToBase64String(authData);

            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Headers =
                {
                    { "Authorization", new AuthenticationHeaderValue("Basic", authParam).ToString() }
                }
            };

            using(HttpResponseMessage response = client.SendAsync(rq).Result)
            {
                if(response.IsSuccessStatusCode)
                {
                    var returnedToken = response.Content.ReadAsStringAsync().Result;
                    if(String.IsNullOrEmpty(returnedToken))
                        throw new Exception("Invalid user name or password");
                    token = returnedToken;
                    expDate = DateTime.Now.AddMinutes(30);
                }
                else
                {
                    throw new Exception($"Operation can't be performed. Reason: {response.ReasonPhrase}");
                }
            }
        }


        public virtual async Task LoginAsync(string controller, string userName, string password)
        {
            token = null;
            expDate = DateTime.MinValue;
            byte[] authData = Encoding.ASCII.GetBytes(String.Format("{0}:{1}", userName, password));
            string authParam = Convert.ToBase64String(authData);

            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Headers =
                {
                    { "Authorization", new AuthenticationHeaderValue("Basic", authParam).ToString() }
                }
            };

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    var returnedToken = await response.Content.ReadAsStringAsync();
                    if(String.IsNullOrEmpty(returnedToken))
                        throw new Exception("Invalid user name or password");
                    token = returnedToken;
                    expDate = DateTime.Now.AddMinutes(30);
                }
                else
                {
                    throw new Exception($"Operation can't be performed. Reason: {response.ReasonPhrase}");
                }
            }
        }


        protected virtual Output Get<Output>(string controller)
        {
            return Get<Output>(controller, null);
        }


        protected virtual Output Get<Output>(string controller, Dictionary<string, string> headers)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Get
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            if(headers != null)
            {
                foreach(var key in headers.Keys)
                    rq.Headers.Add(key, headers[key]);
            }

            using(HttpResponseMessage response = client.SendAsync(rq).Result)
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }

                    string content = null;
                    try { content = response.Content.ReadAsStringAsync().Result; }
                    catch { }

                    if(content != null && content.Length > 1000)
                        content = content.Substring(0, 1000) + "...";

                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}\r\n{content}");
                }
            }
        }


        protected virtual async Task<Output> GetAsync<Output>(string controller)
        {
            return await GetAsync<Output>(controller, null);
        }


        protected virtual async Task<Output> GetAsync<Output>(string controller, Dictionary<string, string> headers)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Get
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            if(headers != null)
            {
                foreach(var key in headers.Keys)
                    rq.Headers.Add(key, headers[key]);
            }

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }

                    string content = null;
                    try { content = await response.Content.ReadAsStringAsync(); }
                    catch { }

                    if(content != null && content.Length > 1000)
                        content = content.Substring(0, 1000) + "...";

                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}\r\n{content}");
                }
            }
        }


        protected virtual Output Invoke<Input, Output>(string controller, Input input)
        {
            return Post<Input, Output>(controller, input, null);
        }


        protected virtual Output Post<Input, Output>(string controller, Input input, Dictionary<string, string> headers)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(input, UseCamelCase ? camelCaseSettings : null), Encoding.UTF8, "application/json")
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            if(headers != null)
            {
                foreach(var key in headers.Keys)
                    rq.Headers.Add(key, headers[key]);
            }

            using(HttpResponseMessage response = client.SendAsync(rq).Result)
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = response.Content.ReadAsStringAsync().Result;
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }

                    string content = null;
                    try { content = response.Content.ReadAsStringAsync().Result; }
                    catch { }

                    if(content != null && content.Length > 1000)
                        content = content.Substring(0, 1000) + "...";

                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}\r\n{content}");
                }
            }
        }


        protected virtual async Task<Output> InvokeAsync<Input, Output>(string controller, Input input)
        {
            return await PostAsync<Input, Output>(controller, input, null);
        }


        protected virtual async Task<Output> PostAsync<Input, Output>(string controller, Input input, Dictionary<string, string> headers)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(input, UseCamelCase ? camelCaseSettings : null), Encoding.UTF8, "application/json")
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            if(headers != null)
            {
                foreach(var key in headers.Keys)
                    rq.Headers.Add(key, headers[key]);
            }

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }

                    string content = null;
                    try { content = await response.Content.ReadAsStringAsync(); }
                    catch { }

                    if(content != null && content.Length > 1000)
                        content = content.Substring(0, 1000) + "...";

                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}\r\n{content}");
                }
            }
        }


        protected virtual async Task InvokeAsync<Input>(string controller, Input input)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(input, UseCamelCase ? camelCaseSettings : null), Encoding.UTF8, "application/json")
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }


        protected virtual async Task<Output> InvokeAsync<Output>(string controller)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = new StringContent("")
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }


        protected virtual async Task<Output> UploadFileAsync<Output>(string controller, string filePath)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var content = new MultipartFormDataContent();
            var binaryContent = new ByteArrayContent(File.ReadAllBytes(filePath));
            binaryContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            content.Add(binaryContent, "file", Path.GetFileName(filePath));

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = content
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }


        protected virtual async Task DownloadFileAsync(string controller, string filePath)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = new StringContent("")
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    using(var content = await response.Content.ReadAsStreamAsync())
                    {
                        using(var file = File.OpenWrite(filePath))
                        {
                            file.SetLength(0L);
                            content.CopyTo(file, 4096);
                        }
                    }
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }



        protected virtual void DownloadFile<Input>(string controller, Input input, string filePath)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(input, UseCamelCase ? camelCaseSettings : null), Encoding.UTF8, "application/json")
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = client.SendAsync(rq).Result)
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    using(var result = response.Content.ReadAsStreamAsync().Result)
                    {
                        using(var file = File.OpenWrite(filePath))
                        {
                            file.SetLength(0L);
                            result.CopyTo(file, 4096);
                        }
                    }
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }


        protected virtual async Task HttpGetFileAsync(string controller, string filePath)
        {
            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Get
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    using(var content = await response.Content.ReadAsStreamAsync())
                    {
                        using(var file = File.OpenWrite(filePath))
                        {
                            file.SetLength(0L);
                            content.CopyTo(file, 4096);
                        }
                    }
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }



        protected virtual async Task<Output> UploadFileAsync<Input, Output>(string controller, Input input, Stream stream,string filename)
        {

            if(controller == null)
                throw new Exception("controller argument cannot be null");

            if(controller.StartsWith("/"))
                controller = controller.Substring(1);

            var content = new MultipartFormDataContent();
            MultipartRequestAddInput(content, input);
            MultipartRequestAddFile(content, stream,filename);

            var rq = new HttpRequestMessage
            {
                RequestUri = new Uri(Url + controller),
                Method = HttpMethod.Post,
                Content = content
            };

            if(!String.IsNullOrWhiteSpace(token))
                rq.Headers.Add("Authorization", new AuthenticationHeaderValue("Bearer", token).ToString());

            using(HttpResponseMessage response = await client.SendAsync(rq))
            {
                if(response.IsSuccessStatusCode)
                {
                    if(!String.IsNullOrWhiteSpace(token))
                        expDate = DateTime.Now.AddMinutes(30);
                    string message = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Output>(message, UseCamelCase ? camelCaseSettings : null);
                }
                else
                {
                    if(response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        token = null;
                        expDate = DateTime.MinValue;
                    }
                    throw new Exception($"Operation can't be performed: {Url + controller} : {response.ReasonPhrase}");
                }
            }
        }

        protected static void MultipartRequestAddInput(MultipartFormDataContent content, object input)
        {
            if(input != null)
            {
                foreach(var prop in input.GetType().GetProperties())
                {
                    var value = prop.GetValue(input)?.ToString() ?? string.Empty;
                    content.Add(new StringContent(value), prop.Name);
                }
            }
        }

        protected static void MultipartRequestAddFile(MultipartFormDataContent content, Stream stream,string filename)
        {
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", filename);
        }
    }
}
