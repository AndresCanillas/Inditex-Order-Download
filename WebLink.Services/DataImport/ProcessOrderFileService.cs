using Microsoft.Extensions.DependencyInjection;
using Service.Contracts;
using Service.Contracts.Authentication;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
 //   public class ProcessOrderFileService : IProcessOrderFileService
 //   {
	//	private IFactory factory;
	//	private IDataImportService dataImportService;
	//	private IOrderImportService orderImportService;
	//	private IMappingRepository mappingRepo;
	//	private IFtpFileReceivedRepository ftpFileRepo;
	//	private INotificationRepository notifications;
	//	private IEventQueue events;
	//	private ILogService log;
	//	private IRemoteFileStore store;


	//	public ProcessOrderFileService(
	//		IFactory factory,
	//		IDataImportService dataImportService,
	//		IOrderImportService orderImportService,
	//		IMappingRepository mappingRepo,
	//		INotificationRepository notifications,
	//		IEventQueue events,
	//		ILogService log,
	//		IFtpFileReceivedRepository ftpFileRepo,
	//		IFileStoreManager storeManager
	//		)
 //       {
	//		this.factory = factory;
	//		this.dataImportService = dataImportService;
	//		this.orderImportService = orderImportService;
	//		this.mappingRepo = mappingRepo;
	//		this.notifications = notifications;
	//		this.events = events;
	//		this.log = log;
	//		this.ftpFileRepo = ftpFileRepo;
	//		store = storeManager.OpenStore("FtpFileStore");
	//	}


	//	// register file into db,  and send event to process order later
	//	public void RegisterFile(string fileName, string filePath, UploadOrderDTO dto)
	//	{
	//		// register file into db
	//		IFTPFileReceived dbFile = ftpFileRepo.Create();
	//		dbFile.FileName = fileName;
	//		dbFile.ProjectID = dto.ProjectID;
	//		dbFile.FactoryID = dto.FactoryID;
	//		dbFile.UploadOrderDTO = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
	//		dbFile.IsProcessed = false;

	//		var inserted = ftpFileRepo.Insert(dbFile);

	//		var container = store.GetOrCreateFile(inserted.ID, inserted.FileName);
	//		container.SetContent(filePath);

	//		events.Send(new FtpFileReceivedEvent() { FtpFileReceivedID = inserted.ID });
	//	}


	//	// copied from FTPFileWatcher
	//	public async Task<DocumentImportResult> ProcessFile(int ftpFileReceivedId)
	//	{
	//		using (var ctx = factory.GetInstance<PrintDB>())
	//		{
	//			const string ftpUser = "FtpFileWatcherService";
	//			var ftpFile = ftpFileRepo.GetByID(ctx, ftpFileReceivedId);

	//			var dto = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadOrderDTO>(ftpFile.UploadOrderDTO);

	//			if (!store.TryGetFile(ftpFile.ID, out var file))
	//			{
	//				throw new ProcessOrderFileServiceException($"File ID [{ftpFile.ID}] not found in store");
	//			}

	//			var notificationData = new NotificationData
	//			{
	//				FileGUID = file.FileGUID,
	//				FileName = file.FileName,
	//				ProjectID = dto.ProjectID,
	//				FactoryID = dto.FactoryID,
	//				ProductionType = dto.ProductionType,
	//				CompanyID = dto.CompanyID,
	//				BrandID = dto.BrandID
	//			};

	//			string nkey = $"ProcessFtpFile/{file.FileName}";
	//			DocumentImportResult result = new DocumentImportResult()
	//			{
	//				Success = false,
	//				Errors = new List<DocumentImportError>()
	//				{
	//					new DocumentImportError()
	//					{
	//						ErrorCode = DocumentImportErrors.OPERATION_CANCELLED,
	//						ErrorMessage = "Cancel Import Process for this order"
	//					}
	//				}
	//			};
	//			try
	//			{
	//				var config = mappingRepo.GetDocumentImportConfiguration(ctx, ftpUser, dto.ProjectID, "Orders", file);
	//				if (await dataImportService.RegisterUserJob(ftpUser, dto.ProjectID, DocumentSource.FTP, true))
	//				{
	//					await dataImportService.StartUserJob(ftpUser, config);
	//					DocumentImportProgress progress;

	//					do
	//					{
	//						await Task.Delay(1000).ConfigureAwait(false);
	//						progress = dataImportService.GetJobProgress(ftpUser);
	//					} while (progress.ReadProgress < 100);

	//					result = await dataImportService.GetJobResult(ftpUser);
	//					if (result.Success)
	//					{
	//						await dataImportService.CompleteUserJob(ftpUser, dto, null);
	//						do
	//						{
	//							await Task.Delay(1000).ConfigureAwait(false);
	//							progress = dataImportService.GetJobProgress(ftpUser);
	//						} while (progress.WriteProgress < 100);

	//						result = await dataImportService.GetJobResult(ftpUser);
	//						if (result.Success)
	//						{
	//							if(result.Errors != null && result.Errors.Count > 0)
	//								log.LogMessage($"DocumentService returned Success = true, but Error collection contains {result.Errors.Count} errors.");
	//							else
	//								log.LogMessage($"DocumentService returned Success = true, returned {result.TotalRows} rows.");

	//							await orderImportService.CompleteOrderUpload(ctx, dataImportService.GetUserJob(ftpUser));
	//							notifications.DismissKey(ctx, nkey);
	//							ftpFile.IsProcessed = true;
	//							ftpFileRepo.Update(ctx, ftpFile);
	//						}
	//						else
	//						{
	//							throw new Exception($"FileWatcher found error(s) while processing file {notificationData.FileName}.");
	//						}
	//					}
	//					else
	//					{
	//						notificationData.ParseErros = result.Errors;
	//						StringBuilder sb = new StringBuilder(1000);
	//						if(result.Errors != null && result.Errors.Count > 0)
	//						{
	//							foreach (var e in result.Errors)
	//								sb.AppendLine($"\t\tRow: {e.Row}, Col: {e.Column}, Field: {e.FieldName}, ErrCode: {e.ErrorCode}, ErrMsg: {e.ErrorMessage}");
	//						}
	//						else
	//						{
	//							sb.Append("Errors is null or empty");
	//						}
	//						throw new Exception($"Document Service cannot process the file {notificationData.FileName} due to the following errors:\r\n{sb.ToString()}");
	//					}
	//				}
	//			}
	//			catch (Exception ex)
	//			{
	//				HandleException(ex, nkey, notificationData, ftpFileReceivedId);
	//				result.Success = false;
	//			}
	//			finally
	//			{
	//				await dataImportService.PurgeJob(ftpUser);
	//			}

	//			return result;
	//		}
	//	}

	//	private void HandleException(Exception ex, string nkey, NotificationData file, int ftpFileReceivedId)
	//	{
	//		var apmNtf = new APMErrorNotification();
			

	//		var stackTrace = ex.StackTrace;
	//		var message = ex.Message;
	//		var inner = ex.InnerException;
	//		var shortFileName = file.FileName.Split($"{Path.DirectorySeparatorChar}FileStore").Length == 2 ? file.FileName.Split($"{Path.DirectorySeparatorChar}FileStore").ElementAt(1) : file.FileName;


	//		while (inner != null)
	//		{
	//			stackTrace += Environment.NewLine + inner.StackTrace;
	//			message += " / " + inner.Message;

	//			inner = inner.InnerException;
	//		}

	//		apmNtf.Message = message;
	//		apmNtf.StackTrace = stackTrace;
	//		apmNtf.NotificationKey = nkey;


	//		// register exception in log
	//		log.LogException("FtpFileWatcher Process", ex);

	//		if (ex is ArticleCodeNotFoundException)
	//		{
	//			var articleCode = (ex as ArticleCodeNotFoundException).ArticleCode;

	//			apmNtf.Type = Roles.IDTCostumerService;
	//			apmNtf.Data = new NotificationDataEventDTO()
	//			{
	//				Notification = new Notification
	//				{
	//					CompanyID = 1, // TODO: revisar esto,
	//					Type = NotificationType.OrderImportError,
	//					IntendedRole = Roles.IDTCostumerService,
	//					IntendedUser = null,
	//					Source = NotificationSources.OrderProcessingStage2,
	//					Title = "Configuration Error",
	//					Message = $"FileWatcher Import Process cannot found Article code '{articleCode}' to process file {shortFileName} in project {file.ProjectID}, brand {file.BrandID} for company {file.CompanyID} ",
	//					LocationID = file.FactoryID,
	//					ProjectID = file.ProjectID

	//				},
	//				CompanyID = file.CompanyID,
	//				BrandID = file.CompanyID,
	//				ProjectID = file.ProjectID,
	//				ErrorType = ErrorNotificationType.ArticleNotFound,

	//			};

	//		}
	//		else if (ex is CompanyCodeNotFoundException)
	//		{
	//			var companyCode = (ex as CompanyCodeNotFoundException).CompanyCode;

	//			apmNtf.Type = Roles.IDTCostumerService;
	//			apmNtf.Data = new NotificationDataEventDTO()
	//			{
	//				Notification = new Notification()
	//				{
	//					CompanyID = 1, // TODO: revisar esto,
	//					Type = NotificationType.OrderImportError,
	//					IntendedRole = Roles.IDTCostumerService,
	//					IntendedUser = null,
	//					Source = NotificationSources.OrderProcessingStage2,
	//					Title = "Provider is missing",
	//					Message = $"Import Process cannot found Company code '{companyCode}' to process file '{Path.GetFileName(shortFileName)}'",
	//					LocationID = file.FactoryID,
	//					ProjectID = file.ProjectID

	//				},
	//				CompanyID = file.CompanyID,
	//				BrandID = file.CompanyID,
	//				ProjectID = file.ProjectID,
	//				ErrorType = ErrorNotificationType.CompanyNotFound
	//			};

	//		}
	//		else if (ex is MappingNotFoundException)
	//		{
	//			apmNtf.Type = Roles.IDTCostumerService;
	//			apmNtf.Data = new NotificationDataEventDTO()
	//			{
	//				Notification = new Notification
	//				{
	//					CompanyID = 1, // TODO: revisar esto,
	//					Type = NotificationType.OrderImportError,
	//					IntendedRole = Roles.IDTCostumerService,
	//					IntendedUser = null,
	//					Source = NotificationSources.OrderProcessingStage1,
	//					Title = "Mapping is missing",
	//					Message = $"Import Process cannot found Mapping to handle {shortFileName} ",
	//					LocationID = file.FactoryID,
	//					ProjectID = file.ProjectID
	//				},
	//				CompanyID = file.CompanyID,
	//				BrandID = file.CompanyID,
	//				ProjectID = file.ProjectID,
	//				ErrorType = ErrorNotificationType.MappingNotFound
	//			};

	//		}else if ( ex is DataImportLookupException)
	//		{
	//			var lookupEx = ex as DataImportLookupException;
	//			apmNtf.Type = Roles.IDTCostumerService;
	//			apmNtf.Data = new NotificationDataEventDTO()
	//			{
	//				Notification = new Notification
	//				{
	//					CompanyID = 1, // TODO: revisar esto,
	//					Type = NotificationType.OrderImportError,
	//					IntendedRole = Roles.IDTCostumerService,
	//					IntendedUser = null,
	//					Source = NotificationSources.OrderProcessingStage1,
	//					Title = "Error To Import Order by lookup",
	//					Message = $" {lookupEx.Message} in file {shortFileName}. Catalog: {lookupEx.Catalog}, Columns: {lookupEx.Columns}",
	//					LocationID = file.FactoryID,
	//					ProjectID = file.ProjectID
	//				},
	//				CompanyID = file.CompanyID,
	//				BrandID = file.CompanyID,
	//				ProjectID = file.ProjectID,
	//				ErrorType = ErrorNotificationType.LookUpKeyNotFound,

	//			};

	//		}
	//		else
	//		{
	//			apmNtf.Type = Roles.SysAdmin;
	//			apmNtf.Data = new NotificationDataEventDTO()
	//			{
	//				Notification = new Notification
	//				{
	//					CompanyID = 1, // TODO: revisar esto,
	//					Type = NotificationType.FTPFileWhatcher,
	//					IntendedRole = Roles.SysAdmin,
	//					IntendedUser = null,
	//					Source = NotificationSources.ProcessManager,
	//					Title = "System Error",
	//					Message = message,
	//					LocationID = file.FactoryID,
	//					ProjectID = file.ProjectID
	//				},

	//				CompanyID = file.CompanyID,
	//				BrandID = file.CompanyID,
	//				ProjectID = file.ProjectID,
	//				ErrorType = ErrorNotificationType.SystemError
	//			};
	//		}


	//		((NotificationDataEventDTO)apmNtf.Data).Key = nkey +'/'+ ((NotificationDataEventDTO)apmNtf.Data).ErrorType;
	//		events.Send(apmNtf);
	//		//RegisterEmail(file, emailType, apmNtf.Data as Notification);
	//	}


	//	public bool FileIsPending(int ftpFileReceivedID)
	//	{
	//		var dbFile = ftpFileRepo.GetByID(ftpFileReceivedID, true);
	//		return dbFile != null && dbFile.IsProcessed == false;
	//	}

	//	#region Pending Methods for better experience
	//	public void CancelPendingEventFor(int ftpFileReceivedId)
	//	{

	//	}


	//	public void CancelAllEvents()
	//	{

	//	}


	//	public void ResetEventFor(int ftpFileReceivedId)
	//	{

	//	}
	//	#endregion Pending Methods for better experience
	//}


	//// new { FileName = container.FullPath, orderDTO.ProjectID, orderDTO.FactoryID, orderDTO.ProductionType };
	//internal class NotificationData
	//{
	//	public Guid FileGUID { get; set; }
	//	public string FileName { get; set; }
	//	public int ProjectID { get; set; }
	//	public int FactoryID { get; set; }
	//	public ProductionType ProductionType { get; set; }
	//	public int CompanyID { get; set; }
	//	public int BrandID { get; set; }
	//	public List<DocumentImportError> ParseErros { get; set; }
	//}

	//[Serializable]
	//internal class ProcessOrderFileServiceException : Exception
	//{
	//	public ProcessOrderFileServiceException()
	//	{
	//	}

	//	public ProcessOrderFileServiceException(string message) : base(message)
	//	{
	//	}

	//	public ProcessOrderFileServiceException(string message, Exception innerException) : base(message, innerException)
	//	{
	//	}

	//	protected ProcessOrderFileServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
	//	{
	//	}
	//}
}
