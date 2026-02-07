using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using Services.Core;
using System;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class RFIDConfigRepository : GenericRepository<IRFIDConfig, RFIDConfig>, IRFIDConfigRepository
    {
        private IConfigurationContext configContext;

        public RFIDConfigRepository(
            IFactory factory,
            IConfigurationContext configContext
            ) : base(factory, (ctx) => ctx.RFIDParameters)
        {
            this.configContext = configContext;
        }


        protected override string TableName { get => "RFIDParameters"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, RFIDConfig actual, IRFIDConfig data)
        {
            actual.SerializedConfig = data.SerializedConfig;
        }


        public IRFIDConfig GetByCompanyID(int companyid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByCompanyID(ctx, companyid);
            }
        }


        public IRFIDConfig GetByCompanyID(PrintDB ctx, int companyid)
        {
            var userData = factory.GetInstance<IUserData>();

            //var isProvider = ctx.CompanyProviders.FirstOrDefault(x => x.CompanyID == companyid && x.ProviderCompanyID == userData.SelectedCompanyID);
            var isProvider = IsProvider(ctx, companyid, userData.SelectedCompanyID);

            if(userData.IsIDT || (userData.SelectedCompanyID == companyid || isProvider))
            {
                var company = ctx.Companies.Where(p => p.ID == companyid).SingleOrDefault();
                if(company != null && company.RFIDConfigID.HasValue)
                    return ctx.RFIDParameters.Where(p => p.ID == company.RFIDConfigID.Value).SingleOrDefault();
                else
                    return null;
            }
            else throw new Exception("Not Authorized");
        }


        public IRFIDConfig GetByBrandID(int brandid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByBrandID(ctx, brandid, out _);
            }
        }


        public IRFIDConfig GetByBrandID(PrintDB ctx, int brandid)
        {
            return GetByBrandID(ctx, brandid, out _);
        }


        private IRFIDConfig GetByBrandID(PrintDB ctx, int brandid, out Brand brand)
        {
            var userData = factory.GetInstance<IUserData>();
            brand = ctx.Brands.Where(p => p.ID == brandid).SingleOrDefault();
            var companyId = brand.CompanyID;

            if(brand != null)
            {
                //var isProvider = ctx.CompanyProviders.FirstOrDefault(x => x.CompanyID == companyId && x.ProviderCompanyID == userData.SelectedCompanyID);
                var isProvider = IsProvider(ctx, companyId, userData.SelectedCompanyID);

                if(userData.IsIDT || (userData.SelectedCompanyID == brand.CompanyID || isProvider != null))
                {
                    if(brand.RFIDConfigID.HasValue)
                    {
                        int configid = brand.RFIDConfigID.Value;
                        return ctx.RFIDParameters.Where(p => p.ID == configid).SingleOrDefault();
                    }
                    else
                        return null;
                }
                else throw new Exception("Not Authorized");
            }
            return null;
        }


        public IRFIDConfig GetByProjectID(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByProjectID(ctx, projectid, out _);
            }
        }


        public IRFIDConfig GetByProjectID(PrintDB ctx, int projectid)
        {
            return GetByProjectID(ctx, projectid, out _);
        }


        private IRFIDConfig GetByProjectID(PrintDB ctx, int projectid, out Project project)
        {
            var userData = factory.GetInstance<IUserData>();
            project = ctx.Projects.Where(p => p.ID == projectid).Include(p => p.Brand).SingleOrDefault();

            var companyID = project.Brand.CompanyID;

            if(project != null)
            {

                //var isProvider = ctx.CompanyProviders.FirstOrDefault(x => x.CompanyID == companyID && x.ProviderCompanyID == userData.SelectedCompanyID);
                var isProvider = IsProvider(ctx, companyID, userData.SelectedCompanyID);

                if(userData.IsIDT || (userData.SelectedCompanyID == project.Brand.CompanyID || isProvider))
                {
                    if(project.RFIDConfigID.HasValue)
                    {
                        int configid = project.RFIDConfigID.Value;
                        return ctx.RFIDParameters.Where(p => p.ID == configid).SingleOrDefault();
                    }
                    else
                        return null;
                }
                else throw new Exception("Not Authorized");
            }
            return null;
        }


        public IRFIDConfig SearchRFIDConfig(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return SearchRFIDConfig(ctx, projectid);
            }
        }


        public IRFIDConfig SearchRFIDConfig(PrintDB ctx, int projectid)
        {
            var config = GetByProjectID(ctx, projectid, out var project);
            if(config != null) return config;
            config = GetByBrandID(ctx, project.BrandID, out var brand);
            if(config != null) return config;
            return GetByCompanyID(ctx, brand.CompanyID);
        }


        public void UpdateSequence(int id, int serial)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                UpdateSequence(ctx, id, serial);
            }
        }


        public void UpdateSequence(PrintDB ctx, int id, int serial)
        {
            var cfg = GetByID(ctx, id);
            var info = configContext.GetInstance<RFIDConfigurationInfo>(cfg.SerializedConfig);
            if(info.Process is IAllocateSerialsProcess p)
            {
                p.Algorithm.Sequence.SetCurrent(serial);
            }
            else
            {
                throw new InvalidOperationException("Cannot use UpdateSequece for this RFID configuration");
            }
        }


        public ITagEncodingProcess GetEncodingProcess(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetEncodingProcess(ctx, projectid);
            }
        }


        public ITagEncodingProcess GetEncodingProcess(PrintDB ctx, int projectid)
        {
            var config = SearchRFIDConfig(ctx, projectid);
            if(config == null)
                throw new InvalidOperationException($"No RFID configuration was found for project {projectid}");

            var encodingProcess = configContext.GetInstance<RFIDConfigurationInfo>(config.SerializedConfig).Process;
            if(encodingProcess == null)
            {
                var log = factory.GetInstance<ILogService>();
                log.LogWarning($"Could not instantiate RFIDConfigurationInfo from saved configuration:\r\n{config.SerializedConfig}");
            }
            return encodingProcess;
        }
    }
}
