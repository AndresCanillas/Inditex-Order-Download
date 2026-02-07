using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Service.Contracts.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLink.Contracts.Models.Repositories.ManualEntry.DTO;

namespace WebLink.Contracts.Models
{
    public class CompanyRepository : GenericRepository<ICompany, Company>, ICompanyRepository
    {
        private IDBConnectionManager connManager;
        private IRFIDConfigRepository rfidRepo;
        private IFtpAccountRepository ftpRepo;
        private IProviderRepository providerRepo;

        public CompanyRepository(
            IFactory factory,
            IDBConnectionManager connManager,
            IRFIDConfigRepository rfidRepo,
            IFtpAccountRepository ftpRepo,
            IProviderRepository providerRepo
            )
        : base(factory, (ctx) => ctx.Companies)
        {
            this.connManager = connManager;
            this.rfidRepo = rfidRepo;
            this.ftpRepo = ftpRepo;
            this.providerRepo = providerRepo;
        }


        protected override string TableName { get => "Companies"; }


        protected virtual async Task AuthorizeOperationAsync(PrintDB ctx, IUserData userData, ICompany data)
        {
            if(userData.IsIDT || userData.UserName == "SYSTEM") return;  // Do not restrict access to IDT Users/SYSTEM

            if(userData.SelectedCompanyID != data.ID && !IsProvider(ctx, userData.SelectedCompanyID, data.ID))
                throw new Exception("Not authorized");

            await Task.CompletedTask;
        }


        protected override void BeforeInsert(PrintDB ctx, IUserData userData, Company actual, out bool cancelOperation)
        {
            cancelOperation = false;
            RFIDConfig cfg = new RFIDConfig();
            var config = rfidRepo.Insert(cfg);
            actual.RFIDConfigID = config.ID;
            actual.HasOrderWorkflow = true;
        }


        protected override void BeforeDelete(PrintDB ctx, IUserData userData, Company actual, out bool cancelOperation)
        {
            cancelOperation = false;
            var brands = ctx.Brands.Where(p => p.CompanyID == actual.ID).AsNoTracking().ToList();
            if(brands.Count > 0)
                throw new Exception("Cannot delete company if it still has brands. Delete all brands first.");
            var companyProvider = ctx.CompanyProviders.Where(p => p.ProviderCompanyID == actual.ID).AsNoTracking().ToList();
            if(companyProvider.Count > 0)
                throw new Exception("Cannot delete company if it is a Provider. Delete CompanyProviders relations first");
        }


        protected override void AfterDelete(PrintDB ctx, IUserData userData, Company actual)
        {
            if(actual.RFIDConfigID.HasValue)
                rfidRepo.Delete(actual.RFIDConfigID.Value);
            if(!String.IsNullOrWhiteSpace(actual.FtpUser))
                ftpRepo.DeleteCompanyFtpAccount(actual.FtpUser);
        }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, Company actual, ICompany data)
        {
            actual.Name = data.Name;
            actual.MainLocationID = data.MainLocationID;
            actual.MainContact = data.MainContact;
            actual.MainContactEmail = data.MainContactEmail;
            actual.Culture = data.Culture;
            actual.Instructions = data.Instructions;
            actual.ShowAsCompany = data.ShowAsCompany;
            actual.OrderSort = data.OrderSort;
            actual.HeaderFields = data.HeaderFields;
            actual.StopFields = data.StopFields;
            if(userData.Principal.IsAnyRole(Roles.SysAdmin, Roles.IDTCostumerService, Roles.IDTProdManager))
            {
                actual.CompanyCode = data.CompanyCode;
                actual.IDTZone = data.IDTZone;
                actual.GSTCode = data.GSTCode;
                actual.GSTID = data.GSTID;
                actual.ClientReference = data.ClientReference;
                actual.SLADays = data.SLADays;
                actual.DefaultProductionLocation = data.DefaultProductionLocation;
                actual.DefaultDeliveryLocation = data.DefaultDeliveryLocation;
                actual.SyncWithSage = data.SyncWithSage;
                actual.SageRef = data.SageRef;
            }
            actual.CustomerSupport1 = data.CustomerSupport1;
            actual.CustomerSupport2 = data.CustomerSupport2;
            actual.ClientContact1 = data.ClientContact1;
            actual.ClientContact2 = data.ClientContact2;

            actual.HasOrderWorkflow = data.HasOrderWorkflow;
        }


        public override List<ICompany> GetList()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var list = GetList(ctx);
                return list;
            }
        }


        public override List<ICompany> GetList(PrintDB ctx)
        {
            return new List<ICompany>(
                All(ctx).Where(p => p.ShowAsCompany == true)
            );
        }


        public override async Task<List<ICompany>> GetListAsync()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return await GetListAsync(ctx);
            }
        }


        public override async Task<List<ICompany>> GetListAsync(PrintDB ctx)
        {
            return new List<ICompany>(
                await All(ctx).Where(p => p.ShowAsCompany == true).ToListAsync()
            );
        }


        public List<ICompany> GetAll()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetAll(ctx);
            }
        }


        public List<ICompany> GetAll(PrintDB ctx)
        {
            return new List<ICompany>(All(ctx));
        }


        public IRFIDConfig GetRFIDParams(int companyid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetRFIDParams(ctx, companyid);
            }
        }


        public IRFIDConfig GetRFIDParams(PrintDB ctx, int companyid)
        {
            var company = GetByID(companyid);
            var parameters = ctx.RFIDParameters
                    .Where(p => p.ID == company.RFIDConfigID)
                    .AsNoTracking()
                    .FirstOrDefault();
            if(parameters == null)
                throw new Exception($"RFIDParams not found {companyid}.");
            return parameters;
        }


        public byte[] GetLogo(int companyid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetLogo(ctx, companyid);
            }
        }


        public byte[] GetLogo(PrintDB ctx, int companyid)
        {
            var company = (Company)GetByID(ctx, companyid);
            return company.Logo;
        }


        public void UpdateLogo(int companyid, byte[] content)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateLogo(ctx, companyid, content);
            }
        }


        public void UpdateLogo(PrintDB ctx, int companyid, byte[] content)
        {
            var company = (Company)GetByID(ctx, companyid);
            company.Logo = ImageProcessing.CreateThumb(content);
            ctx.SaveChanges();
        }


        public List<ICompany> GetProvidersList(int companyId)
        {
            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.Select<ICompany>(@"
                select distinct c.*
            	from Companies c
				where c.ID = @companyId 
				union
				select distinct c.*
            	from Companies c
					join CompanyProviders cp 
                    on (c.ID = cp.ProviderCompanyID and cp.CompanyID = @companyId)", companyId);
            }
        }


        public ICompany GetProjectCompany(int projectid)
        {
            using(var conn = connManager.OpenWebLinkDB())
            {
                return conn.SelectOne<ICompany>(@"select * from Companies c 
					join Brands b on b.CompanyID = c.ID
					join Projects p on p.BrandID = b.ID
					where p.ID = @projectid", projectid);
            }
        }


        public ICompany GetSelectedCompany()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetSelectedCompany(ctx);
            }
        }


        public ICompany GetSelectedCompany(PrintDB ctx)
        {
            var userData = factory.GetInstance<IUserData>();
            return ctx.Companies.Where(p => p.ID == userData.SelectedCompanyID)
                .AsNoTracking()
                .FirstOrDefault();
        }


        public ICompany GetByCompanyCode(string companycode)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompanyCode(ctx, companycode);
            }
        }


        public ICompany GetByCompanyCode(PrintDB ctx, string companycode)
        {
            var company = ctx.Companies.Where(p => p.CompanyCode == companycode).FirstOrDefault();
            if(company != null)
                return company;
            else
                throw new CompanyCodeNotFoundException($"Company with code {companycode} could not be found.", companycode);
        }


        public ICompany GetByCompanyCodeOrReference(int projectID, string code)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompanyCodeOrReference(ctx, projectID, code);
            }
        }

        /// <summary>
        /// Looking the code, firs inner Company that requested the Order through Project
        /// if the requested Company asigned to order to self, return Company
        /// Else looking inner his providers the ClientReferenc
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="projectID"></param>
        /// <param name="code"></param>
        /// <returns></returns>
		public ICompany GetByCompanyCodeOrReference(PrintDB ctx, int projectID, string code)
        {
            var company = (from p in ctx.Projects
                           join b in ctx.Brands on p.BrandID equals b.ID
                           join c in ctx.Companies on b.CompanyID equals c.ID
                           where p.ID == projectID
                           select c).AsNoTracking().FirstOrDefault();

            if(company == null)
                throw new CompanyCodeNotFoundException($"Company no found for project ID [{projectID}]");

            var prv = (from provider in ctx.CompanyProviders
                       join comp in ctx.Companies on provider.ProviderCompanyID equals comp.ID
                       where provider.CompanyID == company.ID && (provider.ClientReference == code || comp.CompanyCode == code)
                       select comp).AsNoTracking().FirstOrDefault();

            if(prv != null)
                return prv;
            var brokers = (from provider in ctx.CompanyProviders
                           join comp in ctx.Companies on provider.ProviderCompanyID equals comp.ID
                           where provider.CompanyID == company.ID && comp.IsBroker
                           select comp.ID
                                   )
                                   .ToList();


            var brokerProviderFound = (from provider in ctx.CompanyProviders
                                       join comp in ctx.Companies on provider.ProviderCompanyID equals comp.ID
                                       where brokers.Contains(provider.CompanyID)
                                       && (provider.ClientReference == code || comp.CompanyCode == code)
                                       select new { Company = comp, Provider = provider }).AsNoTracking().FirstOrDefault();



            if(brokerProviderFound is null)
                throw new CompanyCodeNotFoundException($"Company with ClientReference [{code}] could not be found.", code);

            return brokerProviderFound.Company;
        }

        public ICompany GetByCompanyCodeOrReference(int projectID, string code, out int? providerRecordID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompanyCodeOrReference(ctx, projectID, code, out providerRecordID);
            }
        }

        /// <summary>
        /// looking by company code, return the first providerRecordID Found -> has  problem
        /// looking by clientReference, return the correct providerRecordID
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="projectID"></param>
        /// <param name="codeOrReference"></param>
        /// <param name="providerRecordID"></param>
        /// <returns></returns>
		public ICompany GetByCompanyCodeOrReference(PrintDB ctx, int projectID, string codeOrReference, out int? providerRecordID)
        {
            var company = (from p in ctx.Projects
                           join b in ctx.Brands on p.BrandID equals b.ID
                           join c in ctx.Companies on b.CompanyID equals c.ID
                           where p.ID == projectID
                           select c).AsNoTracking().FirstOrDefault();

            if(company == null)
                throw new CompanyCodeNotFoundException($"Company no found for project ID [{projectID}]");

            var providerFound = (from provider in ctx.CompanyProviders
                                 join comp in ctx.Companies on provider.ProviderCompanyID equals comp.ID
                                 where provider.CompanyID == company.ID && (provider.ClientReference == codeOrReference || comp.CompanyCode == codeOrReference)
                                 select new { Company = comp, Provider = provider }).AsNoTracking().FirstOrDefault();


            if(providerFound != null)
            {
                providerRecordID = providerFound.Provider.ID;

                return providerFound.Company;
            }

            var brokers = (from provider in ctx.CompanyProviders
                           join comp in ctx.Companies on provider.ProviderCompanyID equals comp.ID
                           where provider.CompanyID == company.ID && comp.IsBroker
                           select comp.ID
                                   )
                                   .ToList();




            var brokerProviderFound = (from provider in ctx.CompanyProviders
                                       join comp in ctx.Companies on provider.ProviderCompanyID equals comp.ID
                                       where brokers.Contains(provider.CompanyID)
                                       && (provider.ClientReference == codeOrReference || comp.CompanyCode == codeOrReference)
                                       select new { Company = comp, Provider = provider }).AsNoTracking().FirstOrDefault();

            if(brokerProviderFound is null)
            {
                throw new CompanyCodeNotFoundException($"Company with ClientReference [{codeOrReference}] could not be found. (CCNF:0002)", codeOrReference);
            }

            providerRecordID = brokerProviderFound.Provider.ID;

            return brokerProviderFound.Company;
        }


        //public bool IsValidProvider(int companyid, int providerid)
        //{
        //	if (companyid == providerid) return true;
        //	using (var conn = connManager.OpenWebLinkDB())
        //	{
        //		var isprovider = conn.Exists("select c.ID, c.Name from Companies c join CompanyProviders p on p.ProviderCompanyID = c.ID and p.CompanyID = @companyid where p.ProviderCompanyID = @providerid", companyid, providerid);
        //		return isprovider;
        //	}
        //}


        public void AssignRFIDConfig(int companyid, int configid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                AssignRFIDConfig(ctx, companyid, configid);
            }
        }


        public void AssignRFIDConfig(PrintDB ctx, int companyid, int configid)
        {
            var company = ctx.Companies.Where(c => c.ID == companyid).Single();
            company.RFIDConfigID = configid;
            ctx.SaveChanges();
        }


        public void UpdateOrderSorting(List<Company> companies)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateOrderSorting(ctx, companies);
            }
        }


        public void UpdateOrderSorting(PrintDB ctx, List<Company> companies)
        {
            foreach(var item in companies)
            {
                var company = ctx.Companies.Where(c => c.ID == item.ID).Single();
                company.OrderSort = item.OrderSort;
                company.StopFields = item.StopFields;
                company.HeaderFields = item.HeaderFields;
            }
            ctx.SaveChanges();
        }


        public IEnumerable<ICompany> GetForOwnerOrProvider(int providerID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetForOwnerOrProvider(ctx, providerID);
            }
        }


        public IEnumerable<ICompany> GetForOwnerOrProvider(PrintDB ctx, int providerID)
        {
            var serviceTo = ctx.CompanyProviders
                .Where(w => w.ProviderCompanyID.Equals(providerID))
                .Select(s => s.CompanyID);


            var q = ctx.Companies
                .Where(w => w.ShowAsCompany.Equals(true)
                && (w.ID.Equals(providerID) || serviceTo.Contains(w.ID)));

            return q.ToList();
        }

        public List<string> GetContactEmails(int companyID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetContactEmails(ctx, companyID);
            }
        }

        public List<string> GetContactEmails(PrintDB ctx, int companyID)
        {
            List<string> recipients = new List<string>();

            var company = GetByID(companyID, true);

            if(!String.IsNullOrWhiteSpace(company.ClientContact1) && !recipients.Contains(company.ClientContact1))
                recipients.Add(company.ClientContact1);
            if(!String.IsNullOrWhiteSpace(company.ClientContact2) && !recipients.Contains(company.ClientContact2))
                recipients.Add(company.ClientContact2);

            return recipients;
        }

        public IEnumerable<ICompany> GetListForExternalManager(int factoryID, bool showAsACompany = true)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetListForExternalManager(ctx, factoryID, showAsACompany);
            }
        }

        public IEnumerable<ICompany> GetListForExternalManager(PrintDB ctx, int factoryID, bool showAsACompany = true)
        {
            // TODO: use a date filter
            var companiesInLocation = ctx.CompanyOrders
                .Where(w => w.LocationID == factoryID)
                .GroupBy(g => g.CompanyID)
                .Select(s => (int)s.Key)
                .ToList();


            var q = ctx.Companies
                .Where(w => companiesInLocation.Contains(w.ID));

            if(showAsACompany)
                q = q.Where(w => w.ShowAsCompany);

            return q.ToList();
        }

        public List<ManualEntryDTO> GetAvailablesManualEntry(int companyID, int brandID, int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {

                return GetAvailablesManualEntry(ctx, companyID, brandID, projectID);
            }
        }

        public List<ManualEntryDTO> GetAvailablesOrderPoolManager(int companyID, int brandID, int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {

                return GetAvailablesOrderPoolManager(ctx, companyID, brandID, projectID);
            }
        }

        public List<ManualEntryDTO> GetAvailablesPDFExtractors(int companyID, int brandID, int projectID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {

                return GetAvailablesPDFExtractors(ctx, companyID, brandID, projectID);
            }
        }

        public IEnumerable<ICompany> GetFullList()
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var list = ctx.Companies;
                return list.ToList();
            }
        }

        private List<ManualEntryDTO> GetAvailablesOrderPoolManager(PrintDB ctx, int companyID, int brandID, int projectID)
        {
            var project = ctx.Projects.FirstOrDefault(p => p.ID == projectID);
            if(project != null)
            {
                var manualEntries = ctx.ManualEntryForms.Where(m => m.CompanyID == companyID && m.ProjectID == projectID && m.FormType == Repositories.ManualEntry.Entities.OrderPoolFormType.PoolManagerForm).Select(m => new ManualEntryDTO()
                {
                    CompanyID = companyID,
                    BrandID = brandID,
                    ProjectID = projectID,
                    Url = m.Url,
                    Name = m.Name,
                    EnabledFilePool = project.EnablePoolFile,
                    EnabledOrderPool = project.EnableOrderPool,
                }).ToList();

                if(manualEntries.Any())
                {
                    return manualEntries;
                }

            }
            return new List<ManualEntryDTO>();

            //var query = (from pv in ctx.CompanyProviders
            //            join c in ctx.Companies on pv.CompanyID equals c.ID
            //            join b in ctx.Brands on c.ID equals b.CompanyID
            //            join pj in ctx.Projects on b.ID equals pj.BrandID
            //            join me in ctx.ManualEntryForms on pj.ID equals me.ProjectID
            //            where me.FormType == Repositories.ManualEntry.Entities.OrderPoolFormType.PoolManagerForm
            //            where me.CompanyID == companyID
            //            select new ManualEntryDTO
            //            {
            //                CompanyID = c.ID,
            //                BrandID = b.ID,
            //                ProjectID = pj.ID,
            //                Url = me.Url,
            //                Name = me.Name,
            //                EnabledFilePool = pj.EnablePoolFile,
            //                EnabledOrderPool = pj.EnableOrderPool,

            //            }).Distinct();
            //return query.ToList();

        }

        private List<ManualEntryDTO> GetAvailablesPDFExtractors(PrintDB ctx, int companyID, int brandID, int projectID)
        {
            var project = ctx.Projects.FirstOrDefault(p => p.ID == projectID);
            if(project != null)
            {
                var manualEntries = ctx.ManualEntryForms.Where(m => m.ProjectID == projectID && m.FormType == Repositories.ManualEntry.Entities.OrderPoolFormType.PDFExtractor).Select(m => new ManualEntryDTO()
                {
                    CompanyID = companyID,
                    BrandID = brandID,
                    ProjectID = projectID,
                    Url = m.Url,
                    Name = m.Name,
                    EnabledFilePool = project.EnablePoolFile,
                    EnabledOrderPool = project.EnableOrderPool,
                }).ToList();

                if(manualEntries.Any())
                {
                    return manualEntries;
                }

            }
            return new List<ManualEntryDTO>();

            //var query = (from pv in ctx.CompanyProviders
            //            join c in ctx.Companies on pv.CompanyID equals c.ID
            //            join b in ctx.Brands on c.ID equals b.CompanyID
            //            join pj in ctx.Projects on b.ID equals pj.BrandID
            //            join me in ctx.ManualEntryForms on pj.ID equals me.ProjectID
            //            where me.FormType == Repositories.ManualEntry.Entities.OrderPoolFormType.PoolManagerForm
            //            where me.CompanyID == companyID
            //            select new ManualEntryDTO
            //            {
            //                CompanyID = c.ID,
            //                BrandID = b.ID,
            //                ProjectID = pj.ID,
            //                Url = me.Url,
            //                Name = me.Name,
            //                EnabledFilePool = pj.EnablePoolFile,
            //                EnabledOrderPool = pj.EnableOrderPool,

            //            }).Distinct();
            //return query.ToList();

        }

        private List<ManualEntryDTO> GetAvailablesManualEntry(PrintDB ctx, int companyID, int brandID, int projectID)
        {
            var project = ctx.Projects.FirstOrDefault(p => p.ID == projectID);
            if(project != null)
            {
                var manualEntries = ctx.ManualEntryForms.Where(m => m.ProjectID == projectID && m.FormType == Repositories.ManualEntry.Entities.OrderPoolFormType.ManualEntryForm).Select(m => new ManualEntryDTO()
                {
                    CompanyID = companyID,
                    BrandID = brandID,
                    ProjectID = projectID,
                    Url = m.Url,
                    Name = m.Name,
                    EnabledFilePool = project.EnablePoolFile,
                    EnabledOrderPool = project.EnableOrderPool,
                }).ToList();

                if(manualEntries.Any())
                {
                    return manualEntries;
                }

            }

            var query = (from pv in ctx.CompanyProviders
                         join c in ctx.Companies on pv.CompanyID equals c.ID
                         join b in ctx.Brands on c.ID equals b.CompanyID
                         join pj in ctx.Projects on b.ID equals pj.BrandID
                         join me in ctx.ManualEntryForms on pj.ID equals me.ProjectID
                         where pv.ProviderCompanyID == companyID && me.FormType == Repositories.ManualEntry.Entities.OrderPoolFormType.ManualEntryForm
                         select new ManualEntryDTO
                         {
                             CompanyID = c.ID,
                             BrandID = b.ID,
                             ProjectID = pj.ID,
                             Url = me.Url,
                             Name = me.Name,
                             EnabledFilePool = pj.EnablePoolFile,
                             EnabledOrderPool = pj.EnableOrderPool,
                         }).Distinct();  // 🔥 Elimina duplicados

            return query.ToList();

        }

        #region Filters
        public IList<ICompany> FilterByName(string filterBy)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return FilterByName(ctx, filterBy);
            }
        }

        public IList<ICompany> FilterByName(PrintDB ctx, string filterBy)
        {
            // https://riptutorial.com/ef-core-advanced-topics/learn/100003/collations-and-case-sensitivity#explicit-collation-in-query
            IQueryable<ICompany> query = ctx.Companies;

            var collation = "Modern_Spanish_100_CI_AI";// TODO: GET FORM CONFIG OR SQL QUERY ON DATABASE
            var sql = @"
            SELECT * FROM Companies 
            WHERE Name COLLATE Modern_Spanish_100_CI_AI LIKE {0} COLLATE Modern_Spanish_100_CI_AI";
            //query.Where("ID = @p1", 1);
            var result = query.FromSql(sql, '%' + filterBy + '%');

            return result.ToList();

        }
        #endregion Filters


    }
}
