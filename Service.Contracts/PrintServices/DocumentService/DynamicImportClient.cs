using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Service.Contracts.Documents
{
	public interface IDynamicImportClient
	{
		string Url { get; set; }
		Task StartJobAsync(DocumentImportConfiguration config);
		Task<DocumentImportProgress> GetJobProgressAsync(string id);
		Task<DocumentImportResult> GetJobResultAsync(string id);
		Task<ImportedData> GetImportedDataAsync(string id);
		Task CompleteJobAsync(string id);
		Task CancelJobAsync(string id);
		Task PurgeJobAsync(string id);
        Task<OperationResult> CreateExcelFromCSV(ExcelConfigurationRequest request);

        Task<OperationResult> GetDataFromExcel(ExcelConfigurationRequest request);
       
    }


	class DynamicImportClient : BaseServiceClient, IDynamicImportClient
	{
		public Task StartJobAsync(DocumentImportConfiguration config)
		{
			return base.InvokeAsync<DocumentImportConfiguration>("api/dynamicimport/import", config);
		}

		public async Task<DocumentImportProgress> GetJobProgressAsync(string id)
		{
			return await base.InvokeAsync<string, DocumentImportProgress>("api/dynamicimport/progress", id);
		}

		public async Task<DocumentImportResult> GetJobResultAsync(string id)
		{
			return await base.InvokeAsync<string, DocumentImportResult>("api/dynamicimport/result", id);
		}

		public async Task<ImportedData> GetImportedDataAsync(string id)
		{
			return await base.InvokeAsync<string, ImportedData>("api/dynamicimport/data", id);
		}

		public async Task CompleteJobAsync(string id)
		{
			await base.InvokeAsync<string>("api/dynamicimport/complete", id);
		}

		public async Task CancelJobAsync(string id)
		{
			await base.InvokeAsync<string>("api/dynamicimport/cancel", id);
		}

		public async Task PurgeJobAsync(string id)
		{
			await base.InvokeAsync<string>("api/dynamicimport/purge", id);
		}

        public async Task<OperationResult> CreateExcelFromCSV(ExcelConfigurationRequest request)
        {
            return await base.InvokeAsync<ExcelConfigurationRequest, OperationResult>("api/dynamicexport/excelfromcsv", request);
        }

        

        public async Task<OperationResult> GetDataFromExcel(ExcelConfigurationRequest request)
        {
            

            return await base.InvokeAsync<ExcelConfigurationRequest, OperationResult>("api/dynamicexport/getdatafromexcel", request);
        }
    }
}
