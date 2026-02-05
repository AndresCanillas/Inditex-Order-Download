using Service.Contracts.Authentication;
using Service.Contracts.CLSMiddleware;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using Service.Contracts.Infrastructure.Encoding.SerialSequences;
using Service.Contracts.Infrastructure.Encoding.Tempe;
using Service.Contracts.LabelService;
using Service.Contracts.Logging;
using Service.Contracts.PDFDocumentService;
using Service.Contracts.PrintCentral;
using Service.Contracts.PrintServices.PDFService;
using Service.Contracts.WF;
using Services.Core;
using System;

namespace Service.Contracts
{
    partial class ServiceFactory
    {
        private void Setup(Action<IFactory> configure)
        {
            RegisterSingleton<IFactory>(this);
            RegisterSingleton<IAppConfig, AppConfig>();
            RegisterSingleton<IAppInfo, AppInfo>();
            RegisterSingleton<IAppLog, AppLog>();
            RegisterSingleton<ILogService, LogService>();
            RegisterSingleton<IEncryptionService, EncryptionService>();
            RegisterSingleton<ITempFileService, TempFileService>();
            RegisterSingleton<IMetadataStore, MetadataStore>();
            RegisterSingleton<IConfigurationContext, ConfigurationContext>();
            RegisterSingleton<IEventQueue, EventQueue>();
            RegisterSingleton<ILocalizationService, LocalizationService>();
			RegisterSingleton<IAutomatedProcessManager, AutomatedProcessManager>();
			RegisterSingleton<IWorkflowQueries, WorkflowQueries>();
			RegisterSingleton<IMailService, MailService>();
            RegisterSingleton<IMemorySequence, MemorySequence>();
            RegisterSingleton<IExportTool, ExportTool>();
            RegisterSingleton(typeof(PluginManager<>), typeof(PluginManager<>));
            RegisterSingleton(typeof(IPluginManager<>), typeof(PluginManager<>));
            RegisterSingleton<IEpcRepositoryTempe, EpcRepositoryTempe>();

            RegisterTransient<IAppLogFile, AppLogFile>();
            RegisterTransient<IDBConfiguration, DBConfiguration>();
            RegisterTransient<IConnectionManager, ConnectionManager>();
            RegisterTransient<IEventSyncStore, EventSyncStore>();
            RegisterTransient<IEventSyncClient, EventSyncClient>();

            RegisterSingleton<IFileStoreMttoService, FileStoreMttoService>();
            RegisterTransient<IFileStore, LocalFileStore>();
            RegisterTransient<ILocalFileStore, LocalFileStore>();
            RegisterTransient<IRemoteFileStore, RemoteFileStore>();
            RegisterSingleton<IFileStoreManager, FileStoreManager>();
            RegisterSingleton<IPDFServiceClient, PDFServiceClient>();

            RegisterTransient<IAuthenticationClient, AuthenticationClient>();
            RegisterTransient<IDocumentImportClient, DocumentImportClient>();
            RegisterTransient<IDynamicImportClient, DynamicImportClient>();
            RegisterTransient<IDynamicExportClient, DynamicExportClient>();
            RegisterTransient<IPDFExportClient, PDFExportClient>();
            RegisterTransient<ILabelServiceClient, LabelServiceClient>();
            RegisterSingleton<IBLabelServiceClient, BLabelServiceClient>();
            RegisterTransient<IHerculesEncode, HerculesEncode>();
            RegisterTransient<IOrderComparerService, OrderComparerService>();
            RegisterTransient<IPDFDocumentService, PDFDocumentServiceClient>();
            RegisterTransient<ISerialNumberClient, SerialNumberClient>();
            RegisterTransient<IDataSyncClient, DataSyncClient>();
            RegisterTransient<ICLSClient, CLSClient>();
            RegisterTransient<IEpcServiceTempe, EpcServiceTempe>();
            RegisterTransient<IJomaSerialSequence, JomaSerialSequence>(); 
            

            RegisterTransient<DynamicDB>();

            // Setup RFID Configuration system
            RegisterTransient<ITagEncodingFactory, TagEncodingFactory>();
           
            // Workflow executing types
            RegisterSingleton<WorkflowDataModel>();
            RegisterSingleton<TaskDataModel>();
            RegisterSingleton<ItemDataModel>();
            RegisterSingleton<WFManager>();
            RegisterTransient(typeof(WFRunner<>), typeof(WFRunner<>));
            RegisterTransient(typeof(InsertTaskRunner<,>), typeof(InsertTaskRunner<,>));
            RegisterTransient(typeof(ExecuteTaskRunner<>), typeof(ExecuteTaskRunner<>));
            RegisterTransient(typeof(WaitTaskRunner<,>), typeof(WaitTaskRunner<,>));
            RegisterTransient(typeof(WorkflowTaskRunner<>), typeof(WorkflowTaskRunner<>));
            RegisterTransient(typeof(RoutingTaskRunner<>), typeof(RoutingTaskRunner<>));
            RegisterTransient(typeof(ConditionalTaskRunner<>), typeof(ConditionalTaskRunner<>));
            RegisterTransient(typeof(WhileTaskRunner<>), typeof(WhileTaskRunner<>));
            RegisterTransient(typeof(TryTaskRunner<>), typeof(TryTaskRunner<>));
            RegisterTransient(typeof(MoveToBranchTaskRunner<>), typeof(MoveToBranchTaskRunner<>));
            RegisterTransient(typeof(MoveToTaskRunner<>), typeof(MoveToTaskRunner<>));
            RegisterTransient(typeof(CompleteItemTaskRunner<>), typeof(CompleteItemTaskRunner<>));
            RegisterTransient(typeof(CancelItemTaskRunner<>), typeof(CancelItemTaskRunner<>));
            RegisterTransient(typeof(RejectItemTaskRunner<>), typeof(RejectItemTaskRunner<>));
            RegisterTransient(typeof(DelayItemTaskRunner<>), typeof(DelayItemTaskRunner<>));
            RegisterTransient(typeof(ActionTaskRunner<>), typeof(ActionTaskRunner<>));
            RegisterTransient(typeof(RootTaskRunner<>), typeof(RootTaskRunner<>));

            // Remote IPC types
            RegisterTransient<IMsgPeer, MsgPeer>();
            RegisterScoped<IMsgStreamService, MsgStreamService>();
            //RegisterScoped<IBufferManager, BufferManager>();

            // Invoke optional callback
            configure?.Invoke(this);
        }
    }
}