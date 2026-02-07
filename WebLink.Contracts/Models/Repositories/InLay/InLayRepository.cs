using Service.Contracts;
using Services.Core;
using System.Collections.Generic;
using System.Linq;

namespace WebLink.Contracts.Models
{
    
    public class InLayRepository : GenericRepository<IInLay, InLay>, IInLayRepository
    {
        private ILogService log;

        public InLayRepository(
            IFactory factory,
            ILogService log
            )
            : base(factory, (ctx) => ctx.InLays)
        {
            this.log = log;
        }

        protected override string TableName { get => "InLay"; }
        protected override void UpdateEntity(PrintDB ctx, IUserData userData, InLay actual, IInLay data)
        {
            // recommendation - only sysadmin can edit this info
            // in future this table can contain many options, like Service Url or User Authentication
            actual.ChipName = data.ChipName;
            actual.Description = data.Description;
            actual.Image = data.Image;
            actual.Model = data.Model;
            actual.ProviderName = data.ProviderName;
        }

        public IEnumerable<IInLay> GetInlays(PrintDB ctx, int ProjectID, int BrandID, int CompanyID)
        {
            IEnumerable<IInLay> data = new List<IInLay>();
            var _projects = (
                from i in ctx.InLays
                join lg in ctx.InlayConfigs on i.ID equals lg.InlayID
                where lg.ProjectID == ProjectID && lg.IsAuthorized == true
                select i).ToList();
            var _brands = (
                from i in ctx.InLays
                join lg in ctx.InlayConfigs on i.ID equals lg.InlayID
                where lg.BrandID == BrandID && lg.IsAuthorized == true
                select i).ToList();
            var _companies = (
                from i in ctx.InLays
                join lg in ctx.InlayConfigs on i.ID equals lg.InlayID
                where lg.CompanyID == CompanyID && lg.IsAuthorized == true
                select i).ToList();


            if (_projects.Count > 0)
                return _projects;
            if (_brands.Count > 0)
                return _brands;
            if (_companies.Count > 0)
                return _companies;
            return data;
        }

        public IEnumerable<IInlayConfig> GetInLayConfig(PrintDB ctx, int Projectid, int BrandId, int CompanyId)
        {
            IEnumerable<IInlayConfig> data = new List<IInlayConfig>();
            var _projects = (
                from i in ctx.InLays
                join lg in ctx.InlayConfigs on i.ID equals lg.InlayID
                where lg.ProjectID == Projectid && lg.IsAuthorized == true
                select lg).ToList();
            var _brands = (
                from i in ctx.InLays
                join lg in ctx.InlayConfigs on i.ID equals lg.InlayID
                where lg.BrandID == BrandId && lg.IsAuthorized == true
                select lg).ToList();
            var _companies = (
                from i in ctx.InLays
                join lg in ctx.InlayConfigs on i.ID equals lg.InlayID
                where lg.CompanyID == CompanyId && lg.IsAuthorized == true
                select lg).ToList();


            if (_projects.Count > 0)
                return _projects;
            if (_brands.Count > 0)
                return _brands;
            if (_companies.Count > 0)
                return _companies;
            return data;
        }
    }
}
