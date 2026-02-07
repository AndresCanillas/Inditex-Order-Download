using Service.Contracts;
using Services.Core;

namespace WebLink.Contracts.Models
{
    public interface IERPConfigRepository
    {
    }

    public class ERPConfigRepository : GenericRepository<IERPConfig, ERPConfig>, IERPConfigRepository
    {
        private ILogService log;

        public ERPConfigRepository(
            IFactory factory,
            ILogService log
            )
            : base(factory, (ctx) => ctx.ERPConfigs)
        {
            this.log = log;
        }

        protected override string TableName { get => "ERPConfigs"; }
        protected override void UpdateEntity(PrintDB ctx, IUserData userData, ERPConfig actual, IERPConfig data)
        {
            // recommendation - only sysadmin can edit this info
            // in future this table can contain many options, like Service Url or User Authentication
            actual.Name = data.Name;
        }

    }

   
}
