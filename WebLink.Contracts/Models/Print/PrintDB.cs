using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts.Models.Configuration;
using WebLink.Contracts.Models.Delivery;
using WebLink.Contracts.Models.Print;
using WebLink.Contracts.Models.Print.Configurations;
using WebLink.Contracts.Models.Repositories.ManualEntry.Entities;

namespace WebLink.Contracts.Models
{
    public class PrintDB : DbContext
    {
        private IDBConnectionManager connManager;

        public PrintDB(DbContextOptions<PrintDB> options, IDBConnectionManager connManager) : base(options)
        {
            this.connManager = connManager;
        }

        public IDBConnectionManager ConnectionManager { get { return connManager; } }


        public override int SaveChanges()
        {
            TrimStrings();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TrimStrings();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void TrimStrings()
        {
            foreach(var entry in ChangeTracker.Entries())
            {
                if(entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    foreach(var property in entry.Properties)
                    {
                        if(property.Metadata.ClrType == typeof(string) && property.CurrentValue is string stringValue)
                        {
                            // Obtener los metadatos de la propiedad
                            var propInfo = entry.Entity.GetType().GetProperty(property.Metadata.Name);

                            // Verificar si la propiedad tiene el atributo [NoTrim]
                            if(propInfo != null && Attribute.IsDefined(propInfo, typeof(NoTrimAttribute)))
                            {
                                continue; // Omitir limpieza para este campo
                            }

                            // Aplicar limpieza
                            property.CurrentValue = stringValue.Trim();
                        }
                    }
                }
            }
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new EncodeLabelEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OrderEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OrderUpdatePropertiesEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ArticleEntityConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyProviderEntityConfiguration());
            modelBuilder.ApplyConfiguration(new DeliveryNoteEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PackageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new PackageDetailEntityConfiguration());

            modelBuilder.Entity<SerialSequence>().HasKey(ba => new { ba.ID, ba.Filter });
            modelBuilder.Entity<Category>().HasIndex(p => new { p.ProjectID, p.Name });
            modelBuilder.Entity<EmailToken>().HasIndex(p => new { p.Code, p.UserId });
            modelBuilder.Entity<CompanyProvider>().HasIndex(p => new { p.CompanyID, p.ClientReference }).IsUnique();
            modelBuilder.Entity<ProviderTreeView>().HasKey(e => new { e.CompanyID, e.ParentCompanyID });
            modelBuilder.Entity<ArticleDetail>().HasIndex(e => e.CompanyID);
            modelBuilder.Entity<ArticleTracking>().HasIndex(e => e.ArticleID);
            modelBuilder.Entity<EmailTokenItem>().HasIndex(e => new { e.EmailTokenID, e.OrderID });
            modelBuilder.Entity<OrderLog>().HasIndex(e => new { e.OrderID, e.Level });
            modelBuilder.Entity<CatalogLog>(entity =>
            {
                entity.Property(e => e.OldData)
                      .HasColumnType("VARCHAR(MAX)");

                entity.Property(e => e.NewData)
                      .HasColumnType("VARCHAR(MAX)");
            });
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyProvider> CompanyProviders { get; set; }
        public DbSet<Order> CompanyOrders { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Printer> Printers { get; set; }
        public DbSet<PrinterSettings> PrinterSettings { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<Pack> Packs { get; set; }
        public DbSet<PackArticle> PackArticles { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<LabelData> Labels { get; set; }
        public DbSet<Artifact> Artifacts { get; set; }
        public DbSet<DataImportMapping> DataImportMappings { get; set; }
        public DbSet<DataImportColMapping> DataImportColMapping { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<PrinterJob> PrinterJobs { get; set; }
        public DbSet<PrinterJobDetail> PrinterJobDetails { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RFIDConfig> RFIDParameters { get; set; }
        public DbSet<OrderWorkflowConfig> OrderWorkflowConfigs { get; set; }
        public DbSet<EncodedLabel> EncodedLabels { get; set; }
        public DbSet<SerialSequence> SerialSequences { get; set; }
        public DbSet<GroupFileColumn> GroupFileColumns { get; set; }
        public DbSet<HerculesJobIdRange> HerculesJobIdRange { get; set; }
        public DbSet<ProjectImage> ProjectImages { get; set; }
        public DbSet<CompanyAddress> CompanyAddresses { get; set; }
        public DbSet<BillingInfo> BillingsInfo { get; set; }
        public DbSet<ProviderBillingsInfo> ProviderBillingsInfo { get; set; }
        public DbSet<Wizard> Wizards { get; set; }
        public DbSet<WizardStep> WizardSteps { get; set; }
        public DbSet<WizardCustomStep> WizardCustomSteps { get; set; }
        public DbSet<OrderUpdateProperties> OrderUpdateProperties { get; set; }
        public DbSet<OrderLog> OrderLogs { get; set; }
        public DbSet<ArticlePreviewSettings> ArticlePreviewSettings { get; set; }
        public DbSet<ComparerConfiguration> ComparerConfiguration { get; set; }
        public DbSet<OrderGroup> OrderGroups { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<EmailToken> EmailTokens { get; set; }
        public DbSet<EmailTokenItem> EmailTokenItems { get; set; }
        public DbSet<EmailTokenItemError> EmailTokenItemErrors { get; set; }
        public DbSet<EmailServiceSettings> EmailServiceSettings { get; set; }
        public DbSet<FtpLastRead> FtpLastReads { get; set; }
        public DbSet<FtpWatcherLog> FtpWatcherLogs { get; set; }
        public DbSet<FtpFileReceived> FtpFilesReceived { get; set; }
        public DbSet<ProviderTreeView> ProviderTrewView { get; set; }
        public DbSet<ERPConfig> ERPConfigs { get; set; }
        public DbSet<ERPCompanyLocation> ERPCompanyLocations { get; set; }
        public DbSet<InLay> InLays { get; set; }
        public DbSet<InlayConfig> InlayConfigs { get; set; }
        public DbSet<ArticleDetail> ArticleDetails { get; set; }
        public DbSet<ArticleTracking> ArticleTracking { get; set; }
        public DbSet<OrderPool> OrderPools { get; set; }
        public DbSet<ManualEntryForm> ManualEntryForms { get; set; }
        public DbSet<JomaSerialSequence> JomaSerialSequences { get; set; }
        public DbSet<ArticleCompositionConfig> ArticleCompositionConfigs { get; set; }
        public DbSet<CompanyCertification> CompanyCertifications { get; set; }
        public DbSet<CatalogLog> CatalogLogs { get; set; }
        public DbSet<DeliveryNote> DeliveryNotes { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageDetail> PackageDetails { get; set; }
        public DbSet<Carrier> Carriers { get; set; }
        public DbSet<CompositionAudit> CompositionAudits { get; set; }   
        public DbSet<SystemChangedOrdersLog> SystemChangedOrdersLog { get; set; }
    }

    public enum PrintCountSequenceType
    {
        Single,     // One sequence for all print jobs (all use the same counter)
        Multi       // There will be multiple counters, one of the fields in the variable data is used as discriminant
    }


    public enum FTPMode
    {
        FTP = 1,
        FTPS = 2,
        SFTP = 3
    }

    public enum SyncState
    {
        None = 0,       // No synchronization required, data is not ready yet
        Pending = 1,    // Data is ready to be synchronized
        Completed = 2,  // Data has been synchronized sucessfully
        Processed = 3   // Data has been upload back to the client in a report
    }

    public enum EmailType
    {
        OrderReceived = 1,
        OrderPendingValidation = 2,
        OrderValidated = 3,
        OrderConflict = 4,
        OrderReadyForProduction = 5,
        OrderCompleted = 6,
        OrderProcessingError = 7,
        OrderCancelled = 8,
        OrderResetValidation = 9,
        OrderPoolUpdated = 10
        /*
		ProviderNotFound = 7,
		ArticleNotFound = 8,
		ImportLookupException = 9,
		MappingNotFound = 10,
		ImportError = 11,
		SystemError = 999,
		*/

    }
}

