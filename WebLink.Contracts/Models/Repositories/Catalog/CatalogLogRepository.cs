using Service.Contracts;
using System.Linq;

namespace WebLink.Contracts.Models.Repositories
{
    public class CatalogLogRepository : ICatalogLogRepository
    {
        private IFactory factory;
        public CatalogLogRepository(IFactory factory)
        {
            this.factory = factory;
            
        }

        public void Insert(CatalogLog catalogLog)
        {
            using (var ctx = factory.GetInstance<PrintDB>())
            {
                var catalog = ctx.Catalogs.FirstOrDefault(c => c.CatalogID == catalogLog.CatalogID);
                catalogLog.TableName = catalog.Name;
                ctx.CatalogLogs.Add(catalogLog);
                ctx.SaveChanges();
            }    
        }
    }
}
