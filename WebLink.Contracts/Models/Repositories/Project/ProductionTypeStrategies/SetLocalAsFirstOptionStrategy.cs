using Service.Contracts;
using Services.Core;
using WebLink.Contracts.Models;

namespace WebLink.Contracts
{
    public class SetLocalAsFirstOptionStrategy : ISetLocalAsFirstOptionStrategy
    {
		private IFactory factory;
        private IPrinterRepository printerRepo;
        private IArticleRepository articleRepo;
        private ICompanyRepository companyRepo;
        private ILogService log;

        public SetLocalAsFirstOptionStrategy(
			IFactory factory,
			IPrinterRepository printerRepo, 
			IArticleRepository articleRepo, 
			ICompanyRepository companyRepo, 
			ILogService log)
        {
			this.factory = factory;
            this.printerRepo = printerRepo;
            this.articleRepo = articleRepo;
            this.companyRepo = companyRepo;
            this.log = log;
        }

        public ProductionType GetProductionType(string sendToCode, IProject project, string articleCode)
        {
			using(var ctx = factory.GetInstance<PrintDB>())
			{
				// if customer assigned has a printer assigned inner his locations set as local
				// else se as idt

				log.LogMessage("Get Production Type SetLocalAsFirstOptionStrategy {0} {1} {2}", sendToCode, project.ProjectCode, articleCode);

				if (project.ProductionTypeStrategy != ProjectSetProductionType.SetLocalAsFirstStrategy)
				{
					return ProductionType.IDTLocation;
				}

				// has printers and article can be  locally printer
				var sendtocompany = companyRepo.GetByCompanyCodeOrReference(ctx, project.ID, sendToCode);
				var printers = printerRepo.GetByCompanyID(ctx, sendtocompany.ID);
				var article = articleRepo.GetByCodeInProject(ctx, articleCode, project.ID);

				if (printers.Count < 1 || article.EnableLocalPrint == false)
				{
					return ProductionType.IDTLocation;
				}

				// article is configured to print locally
				return ProductionType.CustomerLocation;
			}
		}
    }
}
