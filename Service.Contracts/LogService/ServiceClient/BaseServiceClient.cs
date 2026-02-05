using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Services.Core
{
	public class BaseServiceClient
	{
		protected static readonly HttpClient httpClient;

		public static bool AcceptAllCertificates { get; set; }

		static BaseServiceClient()
		{
			var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = ValidateRemoteCertificate };
			httpClient = new HttpClient(handler)
			{
				Timeout = TimeSpan.FromMinutes(15),
			};
		}

		private static bool ValidateRemoteCertificate(HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors policyErrors)
		{
			if(policyErrors == SslPolicyErrors.None)
				return true;
#if DEBUG
			return true;
#else
            if(AcceptAllCertificates)
                return true;

            if(cert == null || chain == null || chain.ChainStatus == null || chain.ChainElements.Count == 0)
			    return false;

            // Ensure certificate chain has a trusted root...
            foreach(var status in chain.ChainStatus)
            {
                if(status.Status != X509ChainStatusFlags.NoError)
                    return false;
            }
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            if(!chain.Build(cert))
                return false;

            // Ensure subject is for indetgroup.com
            if(!cert.Subject.EndsWith(".indetgroup.com", StringComparison.OrdinalIgnoreCase))
			    return false;

            return true;
#endif
		}


		private string server;
		private string controller;
		private string baseUri;
		private NetworkCredential credential;
		private string bearerToken;
		private string apikey;
		private AuthenticationHeaderValue authenticationHeader;
		private readonly ConcurrentDictionary<string, string> defaultHeaders = new ConcurrentDictionary<string, string>();

		public BaseServiceClient()
		{
		}

		public BaseServiceClient(string server, string controller)
		{
			this.server = server;
			this.controller = controller;
			UpdateBaseUri();
		}

		public string Server
		{
			get => server;
			set
			{
				server = value;
				UpdateBaseUri();
			}
		}

		public string Controller
		{
			get => controller;
			set
			{
				controller = value;
				UpdateBaseUri();
			}
		}

		public string BaseUri => baseUri;

		private void UpdateBaseUri()
		{
			//Server is not configured, BaseUri will remain set to null
			if(string.IsNullOrWhiteSpace(server))
			{
				baseUri = null;
				return;
			}

			if(!Uri.TryCreate(server, UriKind.Absolute, out var serverUri) || string.Compare(serverUri.Scheme, "https", true) != 0)
			{
				server = $"https://{server}";
				serverUri = new Uri(server);
			}

			if(controller != null)
				controller = controller.TrimEnd('/');

			if(string.IsNullOrWhiteSpace(controller))
			{
				baseUri = $"{serverUri.ToString().TrimEnd('/')}/";
			}
			else
			{
				baseUri = $"{serverUri.ToString().TrimEnd('/')}/{controller}/";
			}
		}

		public ConcurrentDictionary<string, string> DefaultHeaders => defaultHeaders;

		public NetworkCredential Credential => credential;

		public string Token => bearerToken;

		public string ApiKey => apikey;

		public virtual void SetCredential(NetworkCredential credential)
		{
			this.credential = credential;
			if(credential != null)
			{
				var byteArray = Encoding.UTF8.GetBytes($"{credential.UserName}:{credential.Password}:{credential.Domain}");
				authenticationHeader = new AuthenticationHeaderValue("basic", Convert.ToBase64String(byteArray));
			}
			else
			{
				authenticationHeader = null;
			}
		}

		public virtual void SetBearerToken(string bearerToken)
		{
			this.bearerToken = bearerToken;
			if(bearerToken != null)
				authenticationHeader = new AuthenticationHeaderValue("bearer", bearerToken);
			else
				authenticationHeader = null;
		}

		public virtual void SetApiKey(string apikey)
		{
			this.apikey = apikey;
			if(apikey != null)
				authenticationHeader = new AuthenticationHeaderValue("api-key", apikey);
			else
				authenticationHeader = null;
		}

		protected Output Get<Output>(string method = null, object parameters = null, Dictionary<string, string> headers = null)
		{
			return WithRetry(() =>
			{
				var request = PrepareGetRequest(method, parameters, headers);
				var response = SendRequest(request);
				return JsonConvert.DeserializeObject<Output>(response);
			});
		}

		protected async Task<Output> GetAsync<Output>(string method = null, object parameters = null, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
				var request = PrepareGetRequest(method, parameters, headers);
				var response = await SendRequestAsync(request);
				return JsonConvert.DeserializeObject<Output>(response);
			});
		}

		protected string GetString(string method = null, object parameters = null, Dictionary<string, string> headers = null)
		{
			return WithRetry(() =>
			{
				var request = PrepareGetRequest(method, parameters, headers);
				var response = SendRequest(request);
				return response;
			});
		}

		protected async Task<string> GetStringAsync(string method = null, object parameters = null, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
				var request = PrepareGetRequest(method, parameters, headers);
				var response = await SendRequestAsync(request);
				return response;
			});
		}

		protected void Post(string method, Dictionary<string, string> headers = null)
		{
			WithRetry(() =>
			{
				var request = PreparePostRequest(method, headers);
				SendRequest(request);
			});
		}

		protected async Task PostAsync(string method, Dictionary<string, string> headers = null)
		{
			await WithRetryAsync(async () =>
			{
				var request = PreparePostRequest(method, headers);
				await SendRequestAsync(request);
			});
		}

		protected void Post<Input>(string method, Input input, Dictionary<string, string> headers = null)
		{
			WithRetry(() =>
			{
				var request = PreparePostRequest(method, input, headers);
				SendRequest(request);
			});
		}

		protected async Task PostAsync<Input>(string method, Input input, Dictionary<string, string> headers = null)
		{
			await WithRetryAsync(async () =>
			{
				var request = PreparePostRequest(method, input, headers);
				await SendRequestAsync(request);
			});
		}

		protected Output PostWithResult<Output>(string method, object input, Dictionary<string, string> headers = null)
		{
			return WithRetry(() =>
			{
				var request = PreparePostRequest(method, input, headers);
				var response = SendRequest(request);
                var result = JsonConvert.DeserializeObject<Output>(response);
                if(result == null)
					throw new InvalidDataException();
                return result;
			});
		}

		protected async Task<Output> PostWithResultAsync<Output>(string method, object input, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
				var request = PreparePostRequest(method, input, headers);
				var response = await SendRequestAsync(request);
                var result = JsonConvert.DeserializeObject<Output>(response);
                if(result == null)
                    throw new InvalidDataException();
                return result;
            });
		}

		protected async Task<FileResponse> GetFileAsync(string method, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
                using(var request = PrepareGetRequest(method, headers))
                {
                    return await SendFileGetRequestAsync(request);
                }
			});
		}

		protected async Task<FileResponse> GetFileAsync<Input>(string method, Input input, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
                using(var request = PrepareGetRequest(method, input, headers))
                {
                    return await SendFileGetRequestAsync(request);
                }
			});
		}

		protected async Task PostFileAsync(string method, string filePath, Dictionary<string, string> headers = null)
		{
			await WithRetryAsync(async () =>
			{
                using(var request = PrepareFileUploadRequest(method, null, filePath, headers))
                {
                    await SendRequestAsync(request);
                }
			});
		}

		protected async Task PostFileAsync<Input>(string method, Input input, string filePath, Dictionary<string, string> headers = null)
		{
			await WithRetryAsync(async () =>
			{
                using(var request = PrepareFileUploadRequest(method, input, filePath, headers))
                {
                    await SendRequestAsync(request);
                }
			});
		}

		protected async Task PostFileAsync<Input>(string method, Input input, Stream stream, Dictionary<string, string> headers = null)
		{
			await WithRetryAsync(async () =>
			{
                using(var request = PrepareFileUploadRequest(method, input, stream, headers))
                {
                    await SendRequestAsync(request);
                }
			});
		}

		protected async Task<Output> PostFileWithResultAsync<Output>(string method, string filePath, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
                using(var request = PrepareFileUploadRequest(method, null, filePath, headers))
                {
                    var response = await SendRequestAsync(request);
                    var result = JsonConvert.DeserializeObject<Output>(response);
                    if(result == null)
                        throw new InvalidDataException();
                    return result;
                }
            });
		}

		protected async Task<Output> PostFileWithResultAsync<Input, Output>(string method, Input input, string filePath, Dictionary<string, string> headers = null)
		{
			return await WithRetryAsync(async () =>
			{
                using(var request = PrepareFileUploadRequest(method, input, filePath, headers))
                {
                    var response = await SendRequestAsync(request);
                    var result = JsonConvert.DeserializeObject<Output>(response);
                    if(result == null)
                        throw new InvalidDataException();
                    return result;
                }
            });
		}

		protected string SendRequest(HttpRequestMessage request)
		{
            using(HttpResponseMessage response = httpClient.SendAsync(request).Result)
            {
                if(response.IsSuccessStatusCode)
                {
                    string message = response.Content.ReadAsStringAsync().Result;
                    if(response.Headers.TryGetValues("Bearer-Token", out var values))
                        GetBearerToken(values);
                    return message;
                }

                throw HandleResposeError(request, response);
            }
		}

		protected async Task<string> SendRequestAsync(HttpRequestMessage request)
		{
            using(HttpResponseMessage response = await httpClient.SendAsync(request))
            {
                if(response.IsSuccessStatusCode)
                {
                    string message = await response.Content.ReadAsStringAsync();
                    if(response.Headers.TryGetValues("Bearer-Token", out var values))
                        GetBearerToken(values);
                    return message;
                }

                throw HandleResposeError(request, response);
            }
		}

		private async Task<FileResponse> SendFileGetRequestAsync(HttpRequestMessage request)
		{
            using(HttpResponseMessage response = await httpClient.SendAsync(request))
            {
                if(response.IsSuccessStatusCode)
                {
                    var tmpFile = Path.GetTempFileName();
                    var fileName = response.Content.Headers?.ContentDisposition?.FileName ?? tmpFile;

                    // NOTE: Not disposing dst is intended! The caller will be responsible
                    // of disposing not only the stream, but the entire FileRequest object.
                    var dst = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

                    using(var src = await response.Content.ReadAsStreamAsync())
                    {
                        await src.CopyToAsync(dst);
                    }

                    dst.Seek(0L, SeekOrigin.Begin);

                    if(response.Headers.TryGetValues("Bearer-Token", out var values))
                        GetBearerToken(values);

                    return new FileResponse(tmpFile, fileName, dst);
                }

                throw HandleResposeError(request, response);
            }
		}

		protected virtual Exception HandleResposeError(HttpRequestMessage request, HttpResponseMessage response)
		{
			var reason = response.Headers.TryGetValues("Reason-Phrase", out var values) ? values.ElementAt(0) : response.ReasonPhrase;

			if(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
			{
				var authResult = response.Headers.TryGetValues("Authenticate-Result", out var authResultValues) ? authResultValues.ElementAt(0) : null;
				if(authResult == "MustChangePassword")
					return new MustChangePasswordException(reason);
				else if(authResult != null && authResult.StartsWith("PasswordComplexityPolicy:"))
					return new InvalidCredentialException(ValidationHelper.PasswordComplexityPolicy);
				else if(authResult != null && authResult.StartsWith("SamePasswordPolicy:"))
					return new InvalidCredentialException(ValidationHelper.SamePasswordPolicy);
				else if(authResult == "ExpiredToken")
					return new ExpiredTokenException();

				return new AuthorizationException(authResult ?? reason);
			}

			return new Exception($"Operation can't be performed: {request.RequestUri} : {reason}");
		}

		protected virtual void AddRequestHeaders(HttpRequestMessage request, Dictionary<string, string> headers = null)
		{
			request.Headers.Authorization = authenticationHeader;
			foreach(var kvp in defaultHeaders)
			{
				request.Headers.Remove(kvp.Key);
				request.Headers.Add(kvp.Key, kvp.Value);
			}

			// Add custom request headers
			if(headers != null)
			{
				foreach(var kvp in headers)
				{
					request.Headers.Remove(kvp.Key);
					request.Headers.Add(kvp.Key, kvp.Value);
				}
			}
		}

		protected HttpRequestMessage PrepareGetRequest(string method = null, object parameters = null, Dictionary<string, string> headers = null)
		{
			var uri = BuildRequestUri(method, parameters);

			var request = new HttpRequestMessage
			{
				RequestUri = new Uri(uri),
				Method = HttpMethod.Get,
			};

			AddRequestHeaders(request, headers);

			return request;
		}

		private string BuildRequestUri(string method, object parameters)
		{
			if(string.IsNullOrWhiteSpace(BaseUri))
				throw new InvalidOperationException("BaseUri is not valid");

			var sb = new StringBuilder(BaseUri);

			if(method != null)
				sb.Append(method);

			if(parameters == null)
				return sb.ToString();

			sb.Append('?');
			foreach(var property in parameters.GetType().GetProperties())
			{
				var value = property.GetValue(parameters);
				if(value == null)
				{
					sb.Append($"{property.Name}=&");
				}
				else
				{
					if(!(value is string) && value is IEnumerable enumerable)
						value = FormatForQueryString(enumerable);
					else
						value = FormatForQueryString(value);

					value = HttpUtility.UrlEncode((string)value);
					sb.Append($"{property.Name}={value}&");
				}
			}
			sb.Remove(sb.Length - 1, 1);
			return sb.ToString();
		}

		private static string FormatForQueryString(IEnumerable value)
		{
			var sb = new StringBuilder();

			foreach(var e in value)
				sb.Append($"{FormatForQueryString(e)}~");

			if(sb.Length > 0)
				sb.Remove(sb.Length - 1, 1);

			return sb.ToString();
		}

		private static string FormatForQueryString(object value)
		{
			var t = value.GetType();

			if(t.IsValueType)
			{
				if(value is DateTime dt)
					return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
				return value.ToString() ?? "";
			}
			else if(value is string str)
			{
				return str;
			}
			throw new NotImplementedException("Wait a minute, you should not be adding complex data types in the query string... Consider changing your WebApi method to POST and pass a formal -well defined- data contract.");
		}


		protected HttpRequestMessage PreparePostRequest(string method, Dictionary<string, string> headers = null)
		{
			if(string.IsNullOrWhiteSpace(BaseUri))
				throw new InvalidOperationException("BaseUri is not valid");

			var uri = BaseUri;

			if(method != null)
				uri += method;

			var request = new HttpRequestMessage
			{
				RequestUri = new Uri(uri),
				Method = HttpMethod.Post,
			};

			AddRequestHeaders(request, headers);

			return request;
		}


		protected HttpRequestMessage PreparePostRequest<Input>(string method, Input input, Dictionary<string, string> headers = null)
		{
			if(string.IsNullOrWhiteSpace(BaseUri))
				throw new InvalidOperationException("BaseUri is not valid");

			var uri = BaseUri;

			if(method != null)
				uri += method;

			var request = new HttpRequestMessage
			{
				RequestUri = new Uri(uri),
				Method = HttpMethod.Post,
				Content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json")
			};

			AddRequestHeaders(request, headers);

			return request;
		}


		protected HttpRequestMessage PrepareFileUploadRequest(string method, object input, string filePath, Dictionary<string, string> headers = null)
		{
			if(string.IsNullOrWhiteSpace(BaseUri))
				throw new InvalidOperationException("BaseUri is not valid");

			if(!File.Exists(filePath))
				throw new InvalidOperationException($"File {filePath} not found");

			var stream = File.OpenRead(filePath);

			return PrepareFileUploadRequest(method, input, stream, headers);
		}

		protected HttpRequestMessage PrepareFileUploadRequest(string method, object input, Stream stream, Dictionary<string, string> headers = null)
		{
			if(string.IsNullOrWhiteSpace(BaseUri))
				throw new InvalidOperationException("BaseUri is not valid");

			var uri = BaseUri;

			if(method != null)
				uri += method;

			var content = new MultipartFormDataContent("---multipart-form-boundary---");

			MultipartRequestAddInput(content, input);
			MultipartRequestAddFile(content, stream);

			content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
			content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", "---multipart-form-boundary---"));

			var request = new HttpRequestMessage
			{
				RequestUri = new Uri(uri),
				Method = HttpMethod.Post,
				Content = content
			};

			AddRequestHeaders(request, headers);

			return request;
		}

		protected static void MultipartRequestAddInput(MultipartFormDataContent content, object input)
		{
			if(input != null)
			{
				content.Add(new StringContent(JsonConvert.SerializeObject(input)), "input");
				content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			}
		}

		protected static void MultipartRequestAddFile(MultipartFormDataContent content, Stream stream)
		{
			var streamContent = new StreamContent(stream);
			streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
			content.Add(streamContent, "file", "fileName");
		}

		private void GetBearerToken(IEnumerable<string> values)
		{
			if(values != null && values.Any())
				SetBearerToken(Uri.UnescapeDataString(values.ElementAt(0)));
		}

		private T WithRetry<T>(Func<T> caller)
		{
			int retryCount = 1;
			do
			{
				try
				{
					return caller();
				}
				catch(ExpiredTokenException)
				{
					Console.WriteLine($"Expired token, retrying with credential...");
					SetCredential(credential);
				}
			} while(retryCount++ < 2);

			throw new Exception($"Operation can't be performed");
		}

		private async Task<T> WithRetryAsync<T>(Func<Task<T>> caller)
		{
			int retryCount = 1;
			do
			{
				try
				{
					return await caller();
				}
				catch(ExpiredTokenException)
				{
					Console.WriteLine($"Expired token, retrying with credential...");
					SetCredential(credential);
				}
			} while(retryCount++ < 2);

			throw new Exception($"Operation can't be performed");
		}

		private void WithRetry(Action caller)
		{
			int retryCount = 1;
			do
			{
				try
				{
					caller();
					return;
				}
				catch(ExpiredTokenException)
				{
					Console.WriteLine($"Expired token, retrying with credential...");
					SetCredential(credential);
				}
			} while(retryCount++ < 2);

			throw new Exception($"Operation can't be performed");
		}

		private async Task WithRetryAsync(Func<Task> caller)
		{
			int retryCount = 1;
			do
			{
				try
				{
					await caller();
					return;
				}
				catch(ExpiredTokenException)
				{
					Console.WriteLine($"Expired token, retrying with credential...");
					SetCredential(credential);
				}
			} while(retryCount++ < 2);

			throw new Exception($"Operation can't be performed");
		}
	}
}
