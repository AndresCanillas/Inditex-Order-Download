using Microsoft.EntityFrameworkCore;
using Service.Contracts;
using System;
using System.Linq;

namespace WebLink.Contracts.Models
{
    public class OrderWorkflowConfigRepository : GenericRepository<IOrderWorkflowConfig, OrderWorkflowConfig>, IOrderWorkflowConfigRepository
    {
        private IConfigurationContext configContext;

        public OrderWorkflowConfigRepository(
            IFactory factory,
            IConfigurationContext configContext
            ) : base(factory, (ctx) => ctx.OrderWorkflowConfigs)
        {
            this.configContext = configContext;
        }


        protected override string TableName { get => "OrderWorkflowConfigs"; }


        protected override void UpdateEntity(PrintDB ctx, IUserData userData, OrderWorkflowConfig actual, IOrderWorkflowConfig data)
        {
            actual.SerializedConfig = data.SerializedConfig;
        }


        public IOrderWorkflowConfig GetByProjectID(int projectid)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetByProjectID(ctx, projectid);
            }
        }


        public IOrderWorkflowConfig GetByProjectID(PrintDB ctx, int projectid)
        {
            var userData = factory.GetInstance<IUserData>();
            var project = ctx.Projects.Where(p => p.ID == projectid).Include(p => p.Brand).SingleOrDefault();

            if(project != null)
            {
                var companyID = project.Brand.CompanyID;

                var isProvider = ctx.CompanyProviders.FirstOrDefault(x => x.CompanyID == companyID && x.ProviderCompanyID == userData.SelectedCompanyID);

                if(userData.IsIDT || userData.SelectedCompanyID == project.Brand.CompanyID || isProvider != null)
                {
                    if(project.OrderWorkflowConfigID.HasValue)
                    {
                        int configid = project.OrderWorkflowConfigID.Value;
                        return ctx.OrderWorkflowConfigs.Where(p => p.ID == configid).SingleOrDefault();
                    }
                    else
                        return null;
                }
                else throw new Exception("Not Authorized");
            }
            return null;
        }
    }
}
