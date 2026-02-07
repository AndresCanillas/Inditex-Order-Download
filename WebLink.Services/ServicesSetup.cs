using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Service.Contracts;
using System;
using WebLink.Contracts;
using WebLink.Contracts.Middleware;
using WebLink.Contracts.Models;
using WebLink.Contracts.Models.Delivery;
using WebLink.Contracts.Models.Repositories;
using WebLink.Contracts.Sage;
using WebLink.Contracts.Services;
using WebLink.Contracts.Services.Wizards;
using WebLink.Services.Sage;
using WebLink.Services.Wizards;

namespace WebLink.Services
{
    public class ServicesSetup
    {
        public ServicesSetup(IFactory factory)
        {
            // Register singleton objects and services
            factory.RegisterSingleton<IUserDataCacheService, UserDataCacheService>();
            factory.RegisterSingleton<IActiveTokenService, ActiveTokenService>();
            factory.RegisterSingleton<IDataImportService, DataImportService>();
            //factory.RegisterSingleton<IOrderImportService, OrderImportService>();
            factory.RegisterSingleton<ISerialRepository, SerialNumberRepository>();
            factory.RegisterSingleton<ICatalogPluginManager, CatalogPluginManager>();
            factory.RegisterSingleton<IOrderLogService, OrderLogService>();
            factory.RegisterSingleton<IOrderUpdateService, OrderUpdateService>();
            factory.RegisterSingleton<IOrderSetValidatorService, OrderSetValidatorService>();
            factory.RegisterSingleton<IOrderActionsService, OrderActionsService>();
            factory.RegisterSingleton<ISageClientService, SageClientService>();
            factory.RegisterSingleton<IOrderRegisterInERP, OrderRegisterInERP>();
            factory.RegisterSingleton<IOrderEmailService, OrderEmailService>();
            factory.RegisterSingleton<IEmailTemplateService, EmailTemplateService>();
            factory.RegisterSingleton<ISageSyncService, SageSyncService>();
            factory.RegisterSingleton<IDBConnectionManager, DBConnectionManager>();
            factory.RegisterSingleton<IOrderDocumentService, OrderDocumentService>();
            factory.RegisterSingleton<IPrintPackageService, PrintPackageService>();
            //factory.RegisterSingleton<IProcessOrderFileService, ProcessOrderFileService>();
            factory.RegisterSingleton<IProductionTypeManagerService, ProductionTypeManagerService>();
            factory.RegisterSingleton<ISetIDTFactoryStrategy, SetIDTFactoryStrategy>();
            factory.RegisterSingleton<ISetLocalAsFirstOptionStrategy, SetLocalAsFirstOptionStrategy>();
            factory.RegisterSingleton<ISetLocalStrategy, SetLocaStrategy>();
            factory.RegisterSingleton<IOrderUtilService, OrderUtilService>();
            factory.RegisterSingleton<ICLSNotificationService, CLSNotificationService>();
            factory.RegisterSingleton<IMonoprixRFIDReportGeneratorService, MonoprixRFIDReportGeneratorService>();
            factory.RegisterSingleton<IBandFRFIDReportGeneratorService, BandFRFIDReportGeneratorService>();
            factory.RegisterSingleton<IBrownieReportGeneratorService, BrownieReportGeneratorService>();
            factory.RegisterSingleton<IArmandThieryRFIDReportGeneratorService, ArmandThieryRFIDReportGeneratorService>();

            factory.RegisterSingleton<IPrinterRepository, PrinterRepository>();
            factory.RegisterSingleton<ILocationRepository, LocationRepository>();
            factory.RegisterSingleton<ILabelRepository, LabelRepository>();
            factory.RegisterSingleton<IVariableDataRepository, VariableDataRepository>();
            factory.RegisterSingleton<IMappingRepository, MappingRepository>();
            factory.RegisterSingleton<IBrandRepository, BrandRepository>();
            factory.RegisterSingleton<IProjectRepository, ProjectRepository>();
            factory.RegisterSingleton<ICompanyRepository, CompanyRepository>();
            factory.RegisterSingleton<IProviderRepository, ProviderRepository>();
            factory.RegisterSingleton<IOrderRepository, OrderRepository>();
            factory.RegisterSingleton<IOrderDetailRepository, OrderDetailRepository>();
            factory.RegisterSingleton<IUserRepository, UserRepository>();
            factory.RegisterSingleton<IRFIDConfigRepository, RFIDConfigRepository>();
            factory.RegisterSingleton<IOrderWorkflowConfigRepository, OrderWorkflowConfigRepository>();
            factory.RegisterSingleton<IPrinterJobRepository, PrinterJobRepository>();
            factory.RegisterSingleton<IEncodedLabelRepository, EncodedLabelRepository>();
            factory.RegisterSingleton<IMaterialRepository, MaterialRepository>();
            factory.RegisterSingleton<IArticleRepository, ArticleRepository>();
            factory.RegisterSingleton<INotificationRepository, NotificationRepository>();
            factory.RegisterSingleton<IPackRepository, PackRepository>();
            factory.RegisterSingleton<IAddressRepository, AddressRepository>();
            factory.RegisterSingleton<ICatalogRepository, CatalogRepository>();
            factory.RegisterSingleton<ICatalogDataRepository, CatalogDataRepository>();
            factory.RegisterSingleton<IProjectImageRepository, ProjectImageRepository>();
            factory.RegisterSingleton<IFontRepository, FontRepository>();
            factory.RegisterSingleton<IArtifactRepository, ArtifactRepository>();
            factory.RegisterSingleton<IConfigurationRepository, ConfigurationRepository>();
            factory.RegisterSingleton<IBillingRepository, BillingRepository>();
            factory.RegisterSingleton<IWizardRepository, WizardRepository>();
            factory.RegisterSingleton<IWizardStepRepository, WizardStepRepository>();
            factory.RegisterSingleton<IWizardCustomStepRepository, WizardCustomStepRepository>();
            factory.RegisterSingleton<IOrderLogRepository, OrderLogRepository>();
            factory.RegisterSingleton<IOrderUpdatePropertiesRepository, OrderUpdatePropertiesRepository>();
            factory.RegisterSingleton<IOrderDataRepository, OrderDataRepository>();
            factory.RegisterSingleton<IOrderGroupRepository, OrderGroupRepository>();
            factory.RegisterSingleton<ICategoryRepository, CategoryRepository>();
            factory.RegisterSingleton<ICountryRepository, CountryRepository>();
            factory.RegisterSingleton<IFtpAccountRepository, FtpAccountRepository>();
            factory.RegisterSingleton<IFtpFileReceivedRepository, FtpFileReceivedRepository>();
            factory.RegisterSingleton<IERPConfigRepository, ERPConfigRepository>();
            factory.RegisterSingleton<IERPCompanyLocationRepository, ERPCompanyLocationRepository>();
            factory.RegisterSingleton<IScalpersOrderValidationService, ScalpersOrderValidationService>();
            factory.RegisterSingleton<IInLayRepository, InLayRepository>();
            factory.RegisterSingleton<IInlayConfigRepository, InlayConfigRepository>();
			factory.RegisterSingleton<IArticleDetailRepository, ArticleDetailRepository>();
			factory.RegisterSingleton<IOrderWithArticleDetailedService, OrderWithArticleDetailedService>();
			factory.RegisterSingleton<EntityCacheService>();
            factory.RegisterSingleton<IArticleTrackingRepository, ArticleTrackingRepository>();
            factory.RegisterSingleton<ICompanyCertificationRepository, CompanyCertificationRepository>();
            factory.RegisterSingleton<IDeliveryRepository, DeliveryRepository>();

            {
                IdentityOptions options = new IdentityOptions();
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;

                factory.RegisterSingleton<IdentityOptions>(options);
                factory.RegisterSingleton<PasswordValidator>();
                factory.RegisterSingleton<IUserManager, UserManager>();
                factory.RegisterSingleton<IRoleManager, RoleManager>();
                factory.RegisterSingleton<ISignInManager, SignInManager>();
                factory.RegisterSingleton<IAuthenticationCookieProtector, AuthenticationCookieProtector>();

            }

            var config = factory.GetInstance<IAppConfig>();

            // Setup PrintDB options and context
            var printDBConnStr = config.GetValue<string>("Databases.MainDB.ConnStr");
            var printDBOptionsBuilder = new DbContextOptionsBuilder<PrintDB>();
            var printDbLogsEnabled = config.GetValue<bool>("Databases.MainDB.LogsEnabled", false);

            if (printDbLogsEnabled)
                printDBOptionsBuilder.UseLoggerFactory(DbCommandConsoleLoggerFactory);

            printDBOptionsBuilder.UseSqlServer(printDBConnStr, options => options.CommandTimeout(120));
            factory.RegisterSingleton<DbContextOptions<PrintDB>>(printDBOptionsBuilder.Options);

            factory.RegisterTransient<PrintDB>(scope =>
            {
                var options = scope.GetInstance<DbContextOptions<PrintDB>>();
                return new PrintDB(options, null);
            });

            // Setup IdentityDB options and context
            var identityDBConnStr = config.GetValue<string>("Databases.IdentityDB.ConnStr");
            var identityDBOptionsBuilder = new DbContextOptionsBuilder<IdentityDB>();
            identityDBOptionsBuilder.UseSqlServer(identityDBConnStr, options => options.CommandTimeout(120));

            factory.RegisterSingleton<DbContextOptions<IdentityDB>>(identityDBOptionsBuilder.Options);

            factory.RegisterTransient<IdentityDB>(scope =>
            {
                var options = scope.GetInstance<DbContextOptions<IdentityDB>>();
                return new IdentityDB(options);
            });

            // Register some model data contracts (Note: some might be missing)
            factory.RegisterTransient<IPrinter, Printer>();
            factory.RegisterTransient<ILocation, Location>();
            factory.RegisterTransient<ICompany, Company>();
            factory.RegisterTransient<ICompanyProvider, CompanyProvider>();
            factory.RegisterTransient<IOrder, Order>();
            factory.RegisterTransient<ILabelData, LabelData>();
            factory.RegisterTransient<IEncodedLabel, EncodedLabel>();
            factory.RegisterTransient<IRFIDConfig, RFIDConfig>();
            factory.RegisterTransient<IMaterial, Material>();
            factory.RegisterTransient<IArticle, Article>();
            factory.RegisterTransient<IPack, Pack>();
            factory.RegisterTransient<IOrderDetail, OrderDetail>();
            factory.RegisterTransient<INotification, Notification>();
            factory.RegisterTransient<IAddress, WebLink.Contracts.Models.Address>();
            factory.RegisterTransient<IBrand, Brand>();
            factory.RegisterTransient<IProject, Project>();
            factory.RegisterTransient<ICatalog, Catalog>();
            factory.RegisterTransient<IPrinterJob, PrinterJob>();
            factory.RegisterTransient<IPrinterJobDetail, PrinterJobDetail>();
            factory.RegisterTransient<IProjectImage, ProjectImage>();
            factory.RegisterTransient<IWizardStep, WizardStep>();
            factory.RegisterTransient<IWizardCustomStep, WizardCustomStep>();
            factory.RegisterTransient<IWizard, Wizard>();
            factory.RegisterTransient<IOrderUpdateProperties, OrderUpdateProperties>();
            factory.RegisterTransient<IOrderGroup, OrderGroup>();
            factory.RegisterTransient<ICountry, Country>();
            factory.RegisterTransient<IProviderTreeView, ProviderTreeView>();
            factory.RegisterTransient<IERPConfig, ERPConfig>();
            factory.RegisterTransient<IERPCompanyLocation, ERPCompanyLocation>();
            factory.RegisterTransient<IMemorySerialRepository, MemorySerialRepository>();
            factory.RegisterTransient<IOrderNotificationManager, OrderNotificationManager>();
			factory.RegisterTransient<IAppUser, AppUser>();


			// Transiend Services - to avoid convert  injeted objects in singletons ->  injeted objects will be converted as singleton if container class is a singleton, use transien for some cases
			factory.RegisterTransient<IAlvaroMorenoOrderValidationService, AlvaroMorenoOrderValidationService>();
            factory.RegisterTransient<IItemAssigmentService, ItemAssigmentService>();// userdata 
            factory.RegisterTransient<IExpandPackService, ExpandPackService>();
            factory.RegisterTransient<IOrderPoolRepository, OrderPoolRepository>();
            factory.RegisterTransient<IManualEntryServiceSelector, ManualEntryServiceSelector>();
            factory.RegisterTransient<IExtractSizeRangeService, ExtractSizeRangeService>();
            factory.RegisterTransient<TempeManualEntryService>();
            factory.RegisterTransient<ManualEntryService>();
            factory.RegisterTransient<ZaraManualEntryService>();
            factory.RegisterTransient<OrderDetailsRetriever>();
            factory.RegisterTransient<OrderFileBuilder>();
            factory.RegisterTransient<BarcaManualEntryService>();
            factory.RegisterTransient<ICatalogLogRepository, CatalogLogRepository>();
            factory.RegisterTransient<ITempeOrderXmlHandler ,TempeOrderXmlHandler>();
            factory.RegisterTransient<TempeManualEntryXmlService>(); 
            factory.RegisterTransient<IBarçaCatalogHandler, BarçaCatalogHandler>();
            factory.RegisterTransient< IPDFZaraExtractorService, PDFZaraExtractorService >();
            factory.RegisterTransient<ICompositionAuditService, CompositionAuditService >();     
            factory.RegisterTransient<IProviderGetLocationService, ProviderGetLocationService >();
            factory.RegisterTransient<GrupoTendamManualEntryFilterService>();
            factory.RegisterTransient<GrupoTendamManualEntryGrouppingService>();
            factory.RegisterTransient<IGrupoTendamWriter,GrupoTendamWriter>();
        }

        private static readonly LoggerFactory DbCommandConsoleLoggerFactory = new LoggerFactory(new ILoggerProvider[] {
            new ConsoleLoggerProvider ((category, level) => category == DbLoggerCategory.Database.Command.Name && level == Microsoft.Extensions.Logging.LogLevel.Information, true)
        });
    }
}
