using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Service.Contracts.Database;
using Microsoft.Extensions.Configuration;
using Service.Contracts;
using Service.Contracts.Documents;
using WebLink.Contracts;
using System.Data;
using System.Security.Principal;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Transactions;
using WebLink.Contracts.Models;
using System.Runtime.Serialization;
using Services.Core;

namespace WebLink.Services
{
	public class DataImportService : IDataImportService
	{
		private IFactory factory;
		private IDynamicImportClient importClient;
		private INotificationRepository notificationRepo;
		private IEventQueue events;
		private IFileStoreManager storeManager;
		private IAppConfig config;
		private ILogService log;

		private ConcurrentDictionary<string, DataImportJobInfo> jobs = new ConcurrentDictionary<string, DataImportJobInfo>();

		public DataImportService(
			IFactory factory,
			IDynamicImportClient importClient,
			INotificationRepository notificationRepo,
			IEventQueue events,
			IFileStoreManager storeManager,
			IAppConfig config,
			ILogService log
			)
		{
			this.factory = factory;
			this.importClient = importClient;
			this.importClient.Url = config.GetValue<string>("WebLink:DocumentService");
			this.notificationRepo = notificationRepo;
			this.events = events;
			this.storeManager = storeManager;
			this.config = config;
			this.log = log;
			CheckJobStatuses();
		}


		public async Task<bool> RegisterUserJob(string username, int projectid, DocumentSource source, bool purgeExisting)
		{
			DataImportJobInfo existingJob;
			var job = new DataImportJobInfo(username, projectid, source);
			if (jobs.TryGetValue(username, out existingJob))
			{
				if (purgeExisting)
					await PurgeJob(existingJob.User);
				else
					return false;
			}
			return jobs.TryAdd(username, job);
		}


		public DataImportJobInfo GetUserJob(string username)
		{
			if (jobs.TryGetValue(username, out var existingJob))
				return existingJob;
			else
				return null;
		}


		public async Task StartUserJob(string username, DocumentImportConfiguration config)
		{
			if (!jobs.TryGetValue(username, out var job))
				throw new Exception("There is no job registered for the specified user.");
			if (job.Started)
				throw new Exception("Job is already started.");
			if(config.FileGUID == Guid.Empty)
				throw new Exception("Specified file is invalid (FileGUID is empty).");

			try
			{
				job.Config = config;
				config.JobID = job.JobID;
				job.FileName = config.FileName;
				job.FileGUID = config.FileGUID;
				job.Progress = new DocumentImportProgress();
				await importClient.StartJobAsync(config);
				job.Started = true;
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				await PurgeJob(job.User);
				throw;
			}
		}


		public DocumentImportProgress GetJobProgress(string username)
		{
			DataImportJobInfo job = null;
			if (!jobs.TryGetValue(username, out job))
				throw new GetJopProgressException("There is no job registered for the specified user.");
			return job.Progress;
		}


		public async Task<DocumentImportResult> GetJobResult(string username)
		{
			DataImportJobInfo job = null;
			if (!jobs.TryGetValue(username, out job))
				throw new Exception("There is no job registered for the specified user.");
			if (job.Result == null)
				job.Result = await importClient.GetJobResultAsync(job.JobID);
			return job.Result;
		}


		public async Task<ImportedData> GetImportedDataAsync(string username)
		{
			if (!jobs.TryGetValue(username, out var job))
				throw new Exception("There is not job registered for the specified user.");
			if (job.ImportedData == null)
				job.ImportedData = await importClient.GetImportedDataAsync(job.JobID);
			return job.ImportedData;
		}


		public async Task CompleteUserJob(string username, object userData, Func<DataImportJobInfo, Task> completeCallback)
		{
			if (!jobs.TryGetValue(username, out var job))
				throw new Exception("There is no job registered for the specified user.");
			lock (job)
			{
				if (job.IsRunning)
					throw new Exception("Complete process is already running.");
				if (job.Completed)
					throw new Exception("Complete process already finished.");
				job.IsRunning = true;
			}
			job.UserData = userData;
			job.CompleteCallback = completeCallback;
			if (job.Result == null)
				job.Result = await importClient.GetJobResultAsync(job.JobID);
			if (job.ImportedData == null)
				job.ImportedData = await importClient.GetImportedDataAsync(job.JobID);

			//try
			//{
				await ImportToDB(job);
			//}
			//catch (Exception ex)
			//{
			//	RemoveOrphanRows(job);
			//	throw ex;
			//}
		}


		private async Task ImportToDB(DataImportJobInfo job)
		{
			try
			{
				var connStr = config["Databases.CatalogDB.ConnStr"];
				if (job.ImportedData.Rows.Count > 0)
				{
					using (var db = factory.GetInstance<DynamicDB>())
					{
						db.Open(connStr);

						// TODO: split rows here to register orders by article always for all clients
						// xxx: HOW TO GET/DEFINE COLUMN TO GROUP
						//job.ImportedData.Rows.GroupBy(g=> g.Data.)

						var result = db.ImportData(
							job.Config.Output.CatalogID,
							new ImportMappings(job.Config.Output.Mappings),
							job.ImportedData,
							() => job.KeepAlive,
							(progress) =>
							{
								job.Progress.WriteProgress = progress - 1;
								job.Progress.Progress = (progress / 2) - 1 + 50;
							});

						if (!result.Success)
						{
							StringBuilder sb = new StringBuilder(1000);
							foreach (var error in result.Errors)
							{
								log.LogWarning(error.ErrorMessage);
								sb.AppendLine(error.ErrorMessage);
							}
							throw new Exception($"Found errors while trying to import data from document: {sb.ToString()}");
						}
					}
					await InvokeCompleteCallback(job);
				}
			}
			catch (OperationCanceledException opcancel)
			{
				log.LogMessage("ImportToDB opertation was cancelled by the user. See below for error details...");
				log.LogException(opcancel);
				job.Result.Errors.Add(new DocumentImportError("", -1, -1,
						DocumentImportErrors.OPERATION_CANCELLED, "Operation cancelled by the user."));
			}
			catch (DataImportLookupException lookupEx)
			{
				var forRole = job.Config.User != "SysAdmin" ? string.Empty : "SysAdmin";
				var forUser = forRole != "SysAdmin" ? job.Config.User : string.Empty;
				log.LogException(lookupEx);
				job.Result.Errors.Add(new DocumentImportError("", -1, -1,
						DocumentImportErrors.UNEXPECTED_ERROR, "Lookup Catalog key not found"));

				throw; // up same execption
				
			}
			catch (Exception ex)
			{
				log.LogException(ex);
				FSFileReference fileRef = FSFileReference.FromGuid(job.FileGUID);
				var file = storeManager.GetFile(job.FileGUID);
				log.LogMessage("ImportToDB opertation completed with error. See below for error details... FileGuid: {0}, File: {1}",
					(job != null ? job.FileGUID.ToString() : "nulljob"),
					(file != null ? file.FileName : $"nullfile StoreID: {fileRef.StoreID}, RefType: {fileRef.Type}, FileID: {fileRef.FileID}"));
				log.LogMessage($"ImportToDB File: [{job.FileGUID}]");
				job.Result.Errors.Add(new DocumentImportError("", -1, -1,
						DocumentImportErrors.INDETERMINATE_VALUE, "Error while writing data to the database."));
			}
			finally
			{
				job.Progress.Progress = 100;
				job.Progress.WriteProgress = 100;
				job.Result.WriteCompleted = true;
				job.Result.Success = (job.Result.Errors.Count == 0 && job.KeepAlive);
				job.IsRunning = false;
				job.Completed = true;
				try { await importClient.PurgeJobAsync(job.JobID); }
				catch (Exception) { }
			}
		}


		private async Task InvokeCompleteCallback(DataImportJobInfo job)
		{
			// TODO: las notificaciones se intentaran enviar desde un lugar centralizado, es solucion temporal
			Action<DataImportJobInfo,Exception> registerException = (jobLog, exLog) =>
			{
				log.LogException("Error while trying to invoke Job callback from DataImportService...", exLog);
				jobLog.Result.Errors.Add(new DocumentImportError("", -1, -1,
						DocumentImportErrors.UNEXPECTED_ERROR, "Callback process generated an error."));
				jobLog.Result.Success = false;
			};

			Action<DataImportJobInfo, Exception> registerNotification = (jobLog, exLog) =>
			{
				string forRole = jobLog.Config.User != "SysAdmin" ? null : "SysAdmin";
				string forUser = forRole != "SysAdmin" ? jobLog.Config.User : null;

				string r5;
				var r0 = job == null ? "job null" : "ok";
				var r1 = jobLog == null ? "jobLog null" : "ok";
				var r2 = exLog == null ? "exLog null" : "ok";
				var r3 = forRole == null ? "forRole null" : "ok";
				var r4 = forUser == null ? "forUser null" : "ok";
				if (jobLog != null)
					r5 = jobLog.Config == null ? "jobLog.Config null" : "ok";
				else
					r5 = "jobLog.Config null";

				log.LogMessage($"Registering Notification... {r0}, {r1}, {r2}, {r3}, {r4}, {r5}");

				notificationRepo.AddNotification(
				1,
				NotificationType.OrderImportError,
				forRole,
				forUser,
				$"OrderImport{job.FileName}",
				NotificationSources.OrderProcessingStage1,
				"Error To Import Order",
				"Received order cannot be processed",
				new { exLog.Message, exLog.StackTrace, JobConfig = job.Config },
				false,
				null,
				jobLog.Config.ProjectID,
				"DefaultNotificationView"
				);
			};

			try
			{
				if (job.CompleteCallback != null)
				{
					await job.CompleteCallback(job);
				}
			}
			catch (CompanyCodeNotFoundException _ex1)
			{// never execute on inner task
				registerNotification(job, _ex1);
				registerException(job, _ex1);
			}
			catch (AggregateException _ex2)
			{
				// TODO: cuando este metodo es invocado en un task y hay algun problema
				// la excepcion lanzada es un System.AggregateException
				// https://stackoverflow.com/questions/6755541/aggregateexception-c-sharp-example
				registerNotification(job, _ex2.InnerException);
				registerException(job, _ex2.InnerException);

			}
			catch (Exception ex)
			{
				registerNotification(job, ex);
				registerException(job, ex);
			}
		}


		public async Task CancelJob(string username)
		{
			if (!jobs.TryGetValue(username, out var job))
				throw new Exception("There is no job registered for the specified user.");
			if (job.IsRunning) job.KeepAlive = false;
			else await importClient.CancelJobAsync(job.JobID);
		}


		public async Task PurgeJob(string username)
		{
			try
			{
				if (jobs.TryRemove(username, out var job))
				{
					await importClient.PurgeJobAsync(job.JobID);
					if (job.PurgeFile)
						await storeManager.DeleteFileAsync(job.FileGUID);
				}
			}
			catch (Exception ex)
			{
				log.LogException(ex);
			}
		}


		private async void CheckJobStatuses()
		{
			do
			{
				foreach (var job in jobs.Values)
				{
					try
					{
						if (job.Date.AddMinutes(15) < DateTime.Now)
							await PurgeJob(job.User);
						if (job.Started && job.Progress.ReadProgress < 100)
						{
							var progress = await importClient.GetJobProgressAsync(job.JobID);
							job.Progress.ReadProgress = progress.ReadProgress;
						}
					}
					catch (Exception ex)
					{
						log.LogException(ex);
						await PurgeJob(job.User);
					}
				}
				int timeoutInSeconds = 1;
				if (jobs.Values.Count == 0)
					timeoutInSeconds = 5;
				await Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds)).ConfigureAwait(false);
			} while (true);
		}

        public async Task<OperationResult> CreateExcelFromCSV(ExcelConfigurationRequest request )
        {
            return await importClient.CreateExcelFromCSV(request);

            
        }

        public async Task<OperationResult> GetDataFromExcel(Guid excelFileID)
        {
            var request = new ExcelConfigurationRequest() { FromCSVFileID = excelFileID };  

            return await importClient.GetDataFromExcel(request);
        }
    }

	[Serializable]
	public class GetJopProgressException : Exception
	{
		public GetJopProgressException()
		{
		}

		public GetJopProgressException(string message) : base(message)
		{
		}

		public GetJopProgressException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected GetJopProgressException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
