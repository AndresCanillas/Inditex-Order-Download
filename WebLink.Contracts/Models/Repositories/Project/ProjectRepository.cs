using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace WebLink.Contracts.Models
{
    public partial class ProjectRepository : GenericRepository<IProject, Project>, IProjectRepository
    {
        private IAppConfig config;
        private ICatalogRepository catalogRepo;
        private IFtpAccountRepository ftpRepo;
        private IEncryptionService dpp;
        private IDBConnectionManager connManager;
        private IRemoteFileStore projectStore;
        private IRemoteFileStore articlePreviewStore;
        private readonly IOrderEmailService orderEmailService;
        private readonly INotificationRepository notificationRepository;
        private string connstr;

        public ProjectRepository(
            IAppConfig config,
            IFactory factory,
            ICatalogRepository catalogRepo,
            IFtpAccountRepository ftpRepo,
            IEncryptionService dpp,
            IDBConnectionManager connManager,
            IFileStoreManager storeManager,
            IOrderEmailService orderEmailService,
            INotificationRepository notificationRepository
            )
            : base(factory, (ctx) => ctx.Projects)
        {
            this.config = config;
            this.catalogRepo = catalogRepo;
            this.ftpRepo = ftpRepo;
            this.dpp = dpp;
            this.connManager = connManager;
            projectStore = storeManager.OpenStore("ProjectStore");
            articlePreviewStore = storeManager.OpenStore("ArticlePreviewStore");
            connstr = config["Databases.CatalogDB.ConnStr"];
            this.orderEmailService = orderEmailService;
            this.notificationRepository = notificationRepository;
        }

        protected override string TableName { get => "Projects"; }

        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Project actual, IProject data)
        {
            // ???: this otptions maybe require permissions

            if(userData.IsIDT)
            {

                actual.BrandID = data.BrandID;
                actual.Name = data.Name;
                actual.Description = data.Description;
                actual.ProjectCode = data.ProjectCode;
                actual.Hidden = data.Hidden;
                actual.CustomerSupport1 = data.CustomerSupport1;
                actual.CustomerSupport2 = data.CustomerSupport2;
                actual.ClientContact1 = data.ClientContact1;
                actual.ClientContact2 = data.ClientContact2;
                actual.DisablePrintLocal = data.DisablePrintLocal;
                actual.ProductionTypeStrategy = data.ProductionTypeStrategy;
                actual.OrderPlugin = data.OrderPlugin;
                actual.WizardCompositionPlugin = data.WizardCompositionPlugin;
                actual.CustomOrderDataReport = data.CustomOrderDataReport;
                actual.EnablePoolFile = data.EnablePoolFile;
                actual.PoolFileHandler = data.PoolFileHandler;
                actual.EnableOrderPool = data.EnableOrderPool;
                actual.OrderSetValidatorPlugin = data.OrderSetValidatorPlugin;
                actual.HasCompoAudit = data.HasCompoAudit;
                actual.ForceOverwriteOrderOnManualValidation = data.ForceOverwriteOrderOnManualValidation;
                actual.AllowQuantityZero = data.AllowQuantityZero;

            }

            if(userData.Admin_Projects_CanEditFTPSettings == true)
            {

                // ???: se esta reutilizando el permiso de configuracion de ftp
                #region workflow config options
                actual.EnableValidationWorkflow = data.EnableValidationWorkflow;
                actual.TakeOrdersAsValid = data.TakeOrdersAsValid;

                actual.AllowOrderChangesAfterValidation = data.AllowOrderChangesAfterValidation;
                actual.AllowVariableDataEdition = data.AllowVariableDataEdition;// no se esta utilizando

                actual.AllowQuantityEdition = data.AllowQuantityEdition;
                actual.MaxQuantityPercentage = data.MaxQuantityPercentage;
                actual.MaxQuantity = data.MaxQuantity;
                actual.AllowUpdateMadeIn = data.AllowUpdateMadeIn;
                actual.DocumentPreviewDownloadOption = data.DocumentPreviewDownloadOption;
                actual.IsApplyAutomaticPercentage = data.IsApplyAutomaticPercentage;
                actual.AllowEditQuantity = data.AllowEditQuantity;

                actual.AllowExtrasDuringValidation = data.AllowExtrasDuringValidation;
                actual.MaxExtrasPercentage = data.MaxExtrasPercentage;
                actual.MaxExtras = data.MaxExtras;
                actual.AllowUserEditExtraQuantities = data.AllowUserEditExtraQuantities;
                actual.IsApplyAutomaticExtraIncrement = data.IsApplyAutomaticExtraIncrement;

                actual.AllowAddOrChangeComposition = data.AllowAddOrChangeComposition;
                actual.TemplateConfiguration = data.TemplateConfiguration;
                actual.AllowExceptions = data.AllowExceptions;
                actual.AllowAdditionals = data.AllowAdditionals;
                actual.AllowMadeInCompoShoesFiber = data.AllowMadeInCompoShoesFiber;
                actual.EnableSectionWeight = data.EnableSectionWeight;
                actual.EnableShoeComposition = data.EnableShoeComposition;
                actual.EnableAllLangs = data.EnableAllLangs;
                actual.FibersSeparator = Regex.Unescape(data.FibersSeparator);
                actual.SectionsSeparator = Regex.Unescape(data.SectionsSeparator);
                actual.SectionLanguageSeparator = Regex.Unescape(data.SectionLanguageSeparator);
                actual.FiberLanguageSeparator = Regex.Unescape(data.FiberLanguageSeparator);
                actual.CISeparator = Regex.Unescape(data.CISeparator);
                actual.CILanguageSeparator = Regex.Unescape(data.CILanguageSeparator);
                actual.IncludeFiles = data.IncludeFiles;
                actual.RequireItemAssignment = data.RequireItemAssignment;




                actual.RemoveDuplicateTextFromComposition = data.RemoveDuplicateTextFromComposition;


                #endregion workflow config options

                #region Order config
                actual.UpdateType = data.UpdateType;
                actual.AllowOrderChangesAfterValidation = data.AllowOrderChangesAfterValidation;
                actual.EnableMultipleFiles = data.EnableMultipleFiles;
                #endregion Order config



                actual.FTPClients = dpp.EncryptString(data.FTPClients); // TODO: role is not validated -> userData.Admin_Projects_CanEditFTPSettings
                actual.EnableFTPFolder = data.EnableFTPFolder;
                if(data.EnableFTPFolder)
                {
                    if(!ftpRepo.IsValidFtpDirectory(data.FTPFolder))
                        throw new InvalidOperationException($"Specified FTP folder \"{data.FTPFolder}\" is not valid");
                    var brand = ctx.Brands.Where(b => b.ID == actual.BrandID).Single();
                    if(!brand.EnableFTPFolder)
                    {
                        brand.EnableFTPFolder = true;
                        brand.FTPFolder = brand.Name;
                    }
                    var otherProject = ctx.Projects.Where(p => p.ID != actual.ID && p.BrandID == actual.BrandID && p.FTPFolder == data.FTPFolder).FirstOrDefault();
                    if(otherProject != null)
                        throw new Exception($"Cannot create ftp folder {data.FTPFolder} because it is being used by another project {otherProject.Name}");
                    string homeDir = ftpRepo.GetCompanyHomeDirectory(brand.CompanyID);
                    string projectDirectory = Path.Combine(homeDir, brand.FTPFolder, data.FTPFolder);
                    if(!String.IsNullOrWhiteSpace(actual.FTPFolder) && actual.FTPFolder != data.FTPFolder)
                    {
                        string originalDirectory = Path.Combine(homeDir, brand.FTPFolder, actual.FTPFolder);
                        if(Directory.Exists(originalDirectory))
                        {
                            Directory.Move(originalDirectory, projectDirectory);
                        }
                        else
                        {
                            if(!Directory.Exists(projectDirectory))
                                Directory.CreateDirectory(projectDirectory);
                        }
                    }
                    else
                    {
                        if(!Directory.Exists(projectDirectory))
                            Directory.CreateDirectory(projectDirectory);
                    }
                }
                actual.FTPFolder = data.FTPFolder;
            }

            if(userData.Admin_Projects_CanEditRFIDSettings)
            {
                actual.RFIDConfigID = data.RFIDConfigID;
                actual.OrderWorkflowConfigID = data.OrderWorkflowConfigID;
            }

            // TODO: 
            // 1) update dynamic catalogs config to block columns.
            // 2) on update/remove columns, check catalog if has block columns
        }

        protected override void AfterInsert(PrintDB ctx, IUserData userData, Project actual)
        {
            var file = projectStore.GetOrCreateFile(actual.ID, Project.FILE_CONTAINER_NAME);
            file.SetContent(Encoding.UTF8.GetBytes($"Container for project {actual.ID}"));
            AddBaseCatalogs(actual.ID);
        }

        protected override void AfterDelete(PrintDB ctx, IUserData userData, Project actual)
        {
            try
            {
                projectStore.DeleteFile(actual.ID);
            }
            catch { }
        }

        protected override void BeforeDelete(PrintDB ctx, IUserData userData, Project actual, out bool cancelOperation)
        {
            cancelOperation = false;
            var catalogs = catalogRepo.GetByProjectID(ctx, actual.ID);
            foreach(var cat in catalogs)
                catalogRepo.PrepareDelete(cat.ID);
            foreach(var cat in catalogs)
                catalogRepo.Delete(ctx, cat.ID);
            var pid = new SqlParameter("pid", actual.ID);
            ctx.Database.ExecuteSqlCommand("delete from [Packs] where ProjectID = @pid", pid);

            ctx.Database.ExecuteSqlCommand(@"
				delete from t1
				from Artifacts t1 join Articles a on t1.ArticleID = a.ID
				where a.ProjectID = @pid", pid);

            ctx.Database.ExecuteSqlCommand("delete from [Articles] where ProjectID = @pid", pid);
            ctx.Database.ExecuteSqlCommand("delete from [Labels] where ProjectID = @pid", pid);
            ctx.Database.ExecuteSqlCommand(@"
				delete from DataImportColMapping 
					from DataImportColMapping a 
						join DataImportMappings b on a.DataImportMappingID = b.ID
					where b.ProjectID = @pid", pid);
            ctx.Database.ExecuteSqlCommand("delete from [DataImportMappings] where ProjectID = @pid", pid);
        }


        private void AddBaseCatalogs(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var project = (Project)GetByID(ctx, id);

                var compoCatalog = AddCompositionCatalogs(ctx, id);
                //var compoFields = JsonConvert.DeserializeObject<List<FieldDefinition>>(compoCatalog.Definition);

                var variableDataDef = typeof(VariableDataTemplate).GetCatalogDefinition();
                var compoRef = variableDataDef.Fields.Single(f => f.Name == "HasComposition");
                //compoRef.FieldID = compoFields.Count;
                compoRef.Name = "HasComposition";
#if DEBUG
                compoRef.IsSystem = false;
                compoRef.IsLocked = false;
#else
                compoRef.IsSystem = true;
                compoRef.IsLocked = true;
#endif
                compoRef.Type = ColumnType.Reference;
                compoRef.CatalogID = compoCatalog.CatalogID;
                compoRef.FieldID = 1000;// XXX: now is a fixed id

                var variableDataCatalog = CreateCatalog(ctx, project, "VariableData", variableDataDef);

                var detailsDef = typeof(OrderDetailTemplate).GetCatalogDefinition();
                var ref1 = detailsDef.Fields.Single(p => p.Name == "Product");
                ref1.Type = ColumnType.Reference;
                ref1.CatalogID = variableDataCatalog.CatalogID;
                ref1.IsSystem = true;
                ref1.IsLocked = true;
                var details = CreateCatalog(ctx, project, "OrderDetails", detailsDef);

                var ordersDef = typeof(OrderTemplate).GetCatalogDefinition();
                ordersDef.Fields.Add(new FieldDefinition() { FieldID = ordersDef.Fields.Count, Name = "Details", IsSystem = true, IsLocked = true, Type = ColumnType.Set, CatalogID = details.CatalogID });
                CreateCatalog(ctx, project, "Orders", ordersDef);


                //create Templates configuration

            }
        }


        private ICatalog CreateCatalog(PrintDB ctx, Project actual, string name, CatalogDefinition catalogDef)
        {
            var catalog = new Catalog();
            catalog.ProjectID = actual.ID;
            catalog.Name = name;
            catalog.Captions = null;
            catalog.Definition = JsonConvert.SerializeObject(catalogDef.Fields, Formatting.Indented);
            catalog.IsSystem = true;
            catalog.IsHidden = catalogDef.IsHidden;
            catalog.IsReadonly = catalogDef.IsReadonly;
            catalog.CatalogType = catalogDef.CatalogType;
            return catalogRepo.Insert(ctx, catalog);
        }

        public List<IProject> GetByBrandID(int brandid, bool showAll)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByBrandID(ctx, brandid, showAll);
            }
        }
        public List<IProject> GetByBrandIDME(int brandid, bool showAll)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByBrandIDME(ctx, brandid, showAll);
            }
        }
        public List<IProject> GetByBrandIDME(PrintDB ctx, int brandid, bool showAll)
        {
            return ctx.Projects.Where(p => (showAll ? p.BrandID == brandid : (p.BrandID == brandid && !p.Hidden))).ToList<IProject>();
        }

        public List<IProject> GetByBrandID(PrintDB ctx, int brandid, bool showAll)
        {
            return new List<IProject>(
                All(ctx)
                .Where(p => (showAll ? p.BrandID == brandid : (p.BrandID == brandid && !p.Hidden)))
            );
        }

        public IProject GetSelectedProject()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetSelectedProject(ctx);
            }
        }

        public IProject GetSelectedProject(PrintDB ctx)
        {
            var userData = factory.GetInstance<IUserData>();
            IProject project = null;
            var companyid = userData.SelectedCompanyID;
            var projectid = userData.SelectedProjectID;
            if(projectid <= 0)
            {
                project = GetDefaultProject(ctx, companyid);
            }
            else
            {
                project = ctx.Projects.Where(p => p.ID == projectid).AsNoTracking().FirstOrDefault();
                if(project == null)
                    project = GetDefaultProject(ctx, companyid);
            }
            if(project != null)
                return project;
            else
                return new Project() { Name = "No Project" };
        }

        public IProject GetDefaultProject(int companyid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetDefaultProject(ctx, companyid);
            }
        }

        public IProject GetDefaultProject(PrintDB ctx, int companyid)
        {
            var project = (
                from p in ctx.Projects
                join b in ctx.Brands on p.BrandID equals b.ID
                where b.CompanyID == companyid
                select p)
            .OrderByDescending(p => p.CreatedDate)
            .Take(1)
            .AsNoTracking()
            .FirstOrDefault();

            return project;
        }

        public List<DBFieldInfo> GetDBFields(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetDBFields(ctx, id);
            }
        }

        public List<DBFieldInfo> GetDBFields(PrintDB ctx, int id)
        {
            var project = GetByID(ctx, id);
            var variableDataCatalog = (from c in ctx.Catalogs where c.ProjectID == id && c.Name == "VariableData" select c).Single();
            var result = new List<DBFieldInfo>();
            AddFields(ctx, result, variableDataCatalog, "");
            AddFields(result, typeof(GlobalSettingsInfo), "GlobalSettings.");
            AddFields(result, typeof(TagEncodingInfoUI), "RFIDEncoding.");

            return result;
        }

        private void AddFields(PrintDB ctx, List<DBFieldInfo> fields, ICatalog catalog, string path)
        {
            var subFields = catalog.Fields;
            foreach(var f in subFields)
            {
                if(f.Type == ColumnType.Reference)
                {
                    var cat = catalogRepo.GetByCatalogID(ctx, f.CatalogID.Value);
                    AddFields(ctx, fields, cat, path + f.Name + ".");
                }
                else if(f.Type != ColumnType.Set)
                {
                    fields.Add(new DBFieldInfo() { Name = path + f.Name });
                }
            }
        }

        private void AddFields(List<DBFieldInfo> fields, Type type, string path)
        {
            var subFields = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
            foreach(var f in subFields)
            {
                if(f is PropertyInfo)
                {
                    fields.Add(new DBFieldInfo() { Name = path + f.Name, Predefined = true });
                }
                else if(f is FieldInfo)
                {
                    fields.Add(new DBFieldInfo() { Name = path + f.Name, Predefined = true });
                }
            }
        }

        public void Hide(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                Hide(ctx, id);
            }
        }

        public void Hide(PrintDB ctx, int id)
        {
            var project = (Project)GetByID(ctx, id);
            project.Hidden = true;
            ctx.SaveChanges();
        }

        public void AssignRFIDConfig(int projectid, int configid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                AssignRFIDConfig(ctx, projectid, configid);
            }
        }

        public void AssignRFIDConfig(PrintDB ctx, int projectid, int configid)
        {
            var project = ctx.Projects.Where(c => c.ID == projectid).Single();
            project.RFIDConfigID = configid;
            ctx.SaveChanges();
        }

        public void AssignOrderWorkflowConfig(int projectid, int configid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                AssignOrderWorkflowConfig(ctx, projectid, configid);
            }
        }

        public void AssignOrderWorkflowConfig(PrintDB ctx, int projectid, int configid)
        {
            var project = ctx.Projects.Where(c => c.ID == projectid).Single();
            project.OrderWorkflowConfigID = configid;
            ctx.SaveChanges();
        }

        public List<FieldDefinition> GetCatalogFields(int id)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetCatalogFields(ctx, id);
            }
        }

        public List<FieldDefinition> GetCatalogFields(PrintDB ctx, int id)
        {
            var data = catalogRepo.GetByProjectID(ctx, id).FirstOrDefault(x => x.Name.Equals("VariableData"));
            return data.Fields.ToList();
        }

        // emails for customer and client configured for proyect and company requester
        public List<string> GetEmailRecipients(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetEmailRecipients(ctx, projectid);
            }
        }

        public List<string> GetEmailRecipients(PrintDB ctx, int projectid)
        {
            List<string> recipients = new List<string>();
            var project = ctx.Projects.First(p => p.ID == projectid);
            var brand = ctx.Brands.First(b => b.ID == project.BrandID);
            var company = ctx.Companies.First(c => c.ID == brand.CompanyID);
            if(!String.IsNullOrWhiteSpace(project.ClientContact1))
                recipients.Add(project.ClientContact1);
            if(!String.IsNullOrWhiteSpace(project.ClientContact2) && !recipients.Contains(project.ClientContact2))
                recipients.Add(project.ClientContact2);
            if(!String.IsNullOrWhiteSpace(project.CustomerSupport1) && !recipients.Contains(project.CustomerSupport1))
                recipients.Add(project.CustomerSupport1);
            if(!String.IsNullOrWhiteSpace(project.CustomerSupport2) && !recipients.Contains(project.CustomerSupport2))
                recipients.Add(project.CustomerSupport2);
            if(!String.IsNullOrWhiteSpace(company.ClientContact1) && !recipients.Contains(company.ClientContact1))
                recipients.Add(company.ClientContact1);
            if(!String.IsNullOrWhiteSpace(company.ClientContact2) && !recipients.Contains(company.ClientContact2))
                recipients.Add(company.ClientContact2);
            if(!String.IsNullOrWhiteSpace(company.CustomerSupport1) && !recipients.Contains(company.CustomerSupport1))
                recipients.Add(company.CustomerSupport1);
            if(!String.IsNullOrWhiteSpace(company.CustomerSupport2) && !recipients.Contains(company.CustomerSupport2))
                recipients.Add(company.CustomerSupport2);
            return recipients;
        }

        public List<string> GetCustomerEmails(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetCustomerEmails(ctx, projectid);
            }
        }

        public List<string> GetCustomerEmails(PrintDB ctx, int projectID)
        {
            List<string> recipients = new List<string>();
            var project = ctx.Projects.First(p => p.ID == projectID);
            var brand = ctx.Brands.First(b => b.ID == project.BrandID);
            var company = ctx.Companies.First(c => c.ID == brand.CompanyID);

            if(!String.IsNullOrWhiteSpace(project.CustomerSupport1) && !recipients.Contains(project.CustomerSupport1))
                recipients.Add(project.CustomerSupport1);
            if(!String.IsNullOrWhiteSpace(project.CustomerSupport2) && !recipients.Contains(project.CustomerSupport2))
                recipients.Add(project.CustomerSupport2);

            if(!String.IsNullOrWhiteSpace(company.CustomerSupport1) && !recipients.Contains(company.CustomerSupport1))
                recipients.Add(company.CustomerSupport1);
            if(!String.IsNullOrWhiteSpace(company.CustomerSupport2) && !recipients.Contains(company.CustomerSupport2))
                recipients.Add(company.CustomerSupport2);

            return recipients;
        }


        public List<string> GetClientEmails(int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetClientEmails(ctx, projectID);
            }
        }

        public List<string> GetClientEmails(PrintDB ctx, int projectID)
        {
            List<string> recipients = new List<string>();
            var project = ctx.Projects.First(p => p.ID == projectID);
            var brand = ctx.Brands.First(b => b.ID == project.BrandID);
            var company = ctx.Companies.First(c => c.ID == brand.CompanyID);

            if(!String.IsNullOrWhiteSpace(project.ClientContact1))
                recipients.Add(project.ClientContact1);
            if(!String.IsNullOrWhiteSpace(project.ClientContact2) && !recipients.Contains(project.ClientContact2))
                recipients.Add(project.ClientContact2);

            if(!String.IsNullOrWhiteSpace(company.ClientContact1) && !recipients.Contains(company.ClientContact1))
                recipients.Add(company.ClientContact1);
            if(!String.IsNullOrWhiteSpace(company.ClientContact2) && !recipients.Contains(company.ClientContact2))
                recipients.Add(company.ClientContact2);

            return recipients;
        }

        public string DecryptString(string data)
        {
            return dpp.DecryptString(data);
        }

        public IEnumerable<IProject> GetByCompanyID(int companyID, bool showAll)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompanyID(ctx, companyID, showAll);
            }
        }

        public IEnumerable<IProject> GetByCompanyID(PrintDB ctx, int companyID, bool showAll)
        {
            var q = ctx.Projects
                .Join(ctx.Brands, p => p.BrandID, b => b.ID, (p, b) => new { Project = p, Brand = b })
                .Where(w => w.Brand.CompanyID == companyID && (showAll || !w.Project.Hidden))
                .AsNoTracking()
                .Select(s => s.Project)
                .ToList();

            return q;
        }

        public IEnumerable<int> GetProjectsForCustomerService(string userid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetProjectsForCustomerService(ctx, userid);
            }
        }

        public IEnumerable<int> GetProjectsForCustomerService(PrintDB ctx, string userid)
        {
            var projects = (
                from p in ctx.Projects
                join b in ctx.Brands on p.BrandID equals b.ID
                join c in ctx.Companies on b.CompanyID equals c.ID
                where userid == c.CustomerSupport1
                    || userid == c.CustomerSupport2
                    || userid == p.CustomerSupport1
                    || userid == p.CustomerSupport2
                select p.ID);

            return projects.ToList();
        }

        public string GetManualEntryUrl(int projectId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var manualEntryForm = ctx.ManualEntryForms.FirstOrDefault(m => m.ProjectID == projectId);
                if(manualEntryForm == null)
                {
                    return string.Empty;
                }
                return manualEntryForm.Url;
            }
        }

        public void SendEmailIfErrorSupplier()
        {
            throw new NotImplementedException();
        }
    }


    public class VariableDataTemplate
    {
        [PK, Identity, Hidden]
        public int ID { get; set; }

        [MaxLength(30), Required, SystemField, Locked]
        public string Barcode { get; set; }

        [MaxLength(255), Required, MainDisplay, SystemField, Locked]
        [Nullable]
        public string TXT1 { get; set; }

        [MaxLength(255), SystemField, Locked]
        [Nullable]
        public string TXT2 { get; set; }

        [MaxLength(255), SystemField, Locked]
        [Nullable]
        public string TXT3 { get; set; }

        [MaxLength(10), SystemField, Locked]
        [Nullable]
        public string Size { get; set; }

        [MaxLength(30), SystemField, Locked]
        [Nullable]
        public string Color { get; set; }

        [MaxLength(10), SystemField, Locked]
        [Nullable]
        public string Price { get; set; }

        [MaxLength(5), SystemField, Locked]
        [Nullable]
        public string Currency { get; set; }

        [Nullable]
        public int HasComposition { get; set; }
    }


    [Hidden, Readonly]
    public class OrderDetailTemplate
    {
        [PK, Identity, Hidden]
        public int ID { get; set; }

        [MaxLength(25), Required, SystemField, Locked]
        public string ArticleCode { get; set; }

        [MaxLength(25), SystemField, Locked]
        public string PackCode { get; set; }

        [Required, SystemField, Locked]
        public int Quantity { get; set; }

        public int Product { get; set; }
    }


    [Readonly]
    public class OrderTemplate
    {
        [PK, Hidden]
        public int ID { get; set; }

        [MaxLength(16), Required, MainDisplay, SystemField, Locked]
        public string OrderNumber { get; set; }

        [MaxLength(50), SystemField, Locked]
        public string MDOrderNumber { get; set; }

        [Readonly, SystemField, Locked]
        public DateTime OrderDate { get; set; }

        [MaxLength(50), Required, SystemField, Locked]
        public string BillTo { get; set; }

        [MaxLength(50), Required, SystemField, Locked]
        public string SendTo { get; set; }

        [SystemField, Locked]
        public int? ParentOrderID { get; set; }       // Reference to the original order, this is used to be able to compare an edited order with its original during validation.
        [SystemField, Locked]
        public int? CompanyID { get; set; }           // ID of the company that issued this order
        [SystemField, Locked]
        public int? ProjectID { get; set; }           // ID of the project with which this order was processed
        [SystemField, Locked]
        public int? OrderID { get; set; }             // ID of the order (in weblink)
    }

    [Hidden, Readonly]
    public class TemplatesTemplate
    {
        [PK, Identity, Hidden]
        public int ID { get; set; }

        [MaxLength(254), Required, SystemField]
        public string Name { get; set; }
    }


}
