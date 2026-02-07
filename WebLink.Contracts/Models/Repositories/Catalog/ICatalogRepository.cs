using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface ICatalogRepository : IGenericRepository<ICatalog>
    {
        ICatalog GetByCatalogID(int catalogid);
        ICatalog GetByCatalogID(PrintDB ctx, int catalogid);

        ICatalog GetByName(int projectid, string name);
        ICatalog GetByName(PrintDB ctx, int projectid, string name);

        List<ICatalog> GetByProjectID(int projectid, bool byPassChecks = false);
        List<ICatalog> GetByProjectID(PrintDB ctx, int projectid, bool byPassChecks = false);

        List<ICatalog> GetByProjectIDWithRoles(int projectid);
        List<ICatalog> GetByProjectIDWithRoles(PrintDB ctx, int projectid);

        void PrepareDelete(int catalogid);

        List<string> GetCatalogRoles(int catalogid);
        List<string> GetCatalogRoles(PrintDB ctx, int catalogid);

        void AssignCatalogRoles(int catalogid, string roles);
        void AssignCatalogRoles(PrintDB ctx, int catalogid, string roles);

        void ImportCatalog(int id, Catalog catalog, string tableDefinition);
        void AlterCatalog(Catalog catalog);

    }
}
