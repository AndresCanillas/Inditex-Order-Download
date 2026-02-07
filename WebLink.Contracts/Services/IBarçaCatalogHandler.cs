using System.Collections.Generic;

namespace WebLink.Contracts.Services
{
    public interface IBarçaCatalogHandler
    {
        Importers GetImporters(int projectID, string providerReference);
        void UpdateImporters(int projectid, Importers importers);
        List<ImportersMadeIN> GetImportersMadeIn(int projectID);
    }
}