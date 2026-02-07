using Service.Contracts;
using Services.Core;

namespace WebLink.Contracts.Models
{
    public interface IInlayConfigRepository{}

    public class InlayConfigRepository : GenericRepository<IInlayConfig, InlayConfig>, IInlayConfigRepository
    {
        private ILogService log;

        public InlayConfigRepository(
            IFactory factory,
            ILogService log
            )
            : base(factory, (ctx) => ctx.InlayConfigs)
        {
            this.log = log;
        }


        protected override string TableName { get => "InLayConfig"; }

        protected override void UpdateEntity(PrintDB ctx, IUserData userData, InlayConfig actual, IInlayConfig data)
        {
            actual.InlayID = data.InlayID;
            actual.Description = data.Description;
            actual.CompanyID = data.CompanyID;
            actual.BrandID = data.BrandID;
            actual.ProjectID = data.ProjectID;
            actual.IsAuthorized = data.IsAuthorized;
        }
    }
}
