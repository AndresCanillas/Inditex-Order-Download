using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Documents
{
	public interface IDocumentImportClient
	{
		string Url { get; set; }
		Task ImportAsync(DocumentImportConfiguration config);
		Task<int> GetJobProgressAsync(string id);
		Task<DocumentImportResult> GetJobResultAsync(string id);
	}


	class DocumentImportClient : BaseServiceClient, IDocumentImportClient
	{
		public Task ImportAsync(DocumentImportConfiguration config)
		{
			return base.InvokeAsync<DocumentImportConfiguration>("api/directimport/import", config);
		}

		public async Task<int> GetJobProgressAsync(string id)
		{
			return await base.InvokeAsync<string, int>("api/directimport/progress", id);
		}

		public async Task<DocumentImportResult> GetJobResultAsync(string id)
		{
			return await base.InvokeAsync<string, DocumentImportResult>("api/directimport/result", id);
		}
	}
}
