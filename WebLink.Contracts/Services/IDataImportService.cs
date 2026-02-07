using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Service.Contracts;
using Service.Contracts.Documents;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
	public interface IDataImportService
	{
		Task<bool> RegisterUserJob(string username, int projectid, DocumentSource source, bool purgeExisting);
		DataImportJobInfo GetUserJob(string username);
		Task StartUserJob(string username, DocumentImportConfiguration config);
		DocumentImportProgress GetJobProgress(string username);
		Task<DocumentImportResult> GetJobResult(string username);
		Task<ImportedData> GetImportedDataAsync(string username);
		Task CompleteUserJob(string username, object userData, Func<DataImportJobInfo, Task> completeTask);
		Task CancelJob(string username);
		Task PurgeJob(string username);
        Task<OperationResult> CreateExcelFromCSV(ExcelConfigurationRequest request);
        Task<OperationResult> GetDataFromExcel(Guid excelFileID);
    }
}
