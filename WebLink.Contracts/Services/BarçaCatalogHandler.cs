using Service.Contracts.Database;
using System.Collections.Generic;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class BarçaCatalogHandler : IBarçaCatalogHandler
    {
        private readonly ICatalogRepository catalogRepo;
        private IConnectionManager connManager;

        public BarçaCatalogHandler(ICatalogRepository catalogRepo, IConnectionManager connManager)
        {
            this.catalogRepo = catalogRepo;
            this.connManager = connManager;
        }
        public Importers GetImporters(int projectID, string providerReference)
        {
            var catalogs = catalogRepo.GetByProjectID(projectID, true);
            var importerCatalog = catalogs.Find(c => c.Name == "Importers");
            if(importerCatalog == null)
            {
                return null;
            }
            using(var dynamicDb = connManager.OpenDB("CatalogDB"))
            {
                var importer = dynamicDb.SelectOne<Importers>($@"SELECT a.* FROM {importerCatalog.TableName} a WHERE a.[Code] = '{providerReference}' ");
                if (importer == null)
                {
                    importer = new Importers() { 
                        MadeIn = string.Empty,
                        ImporterData = string.Empty,
                        IsNew = true 
                    };
                }
                return importer;
            }
        }

        public List<ImportersMadeIN> GetImportersMadeIn(int projectID)
        {
            var catalogs = catalogRepo.GetByProjectID(projectID, true);
            var importerCatalog = catalogs.Find(c => c.Name == "MadeIn");
            if(importerCatalog == null)
            {
                return null;
            }
            using(var dynamicDb = connManager.OpenDB("CatalogDB"))
            {
                var importer = dynamicDb.Select<ImportersMadeIN>($@"SELECT a.English FROM {importerCatalog.TableName} a ");
                return importer;
            }
        }

        public void UpdateImporters(int projectid, Importers importers)
        {
            var catalogs = catalogRepo.GetByProjectID(projectid, true);
            var importerCatalog = catalogs.Find(c => c.Name == "Importers");
            if(importerCatalog == null)
            {
                return;
            }



            using(var dynamicDb = connManager.OpenDB("CatalogDB"))
            {
                if(!importers.IsNew)
                {
                    dynamicDb.ExecuteNonQuery($@"UPDATE {importerCatalog.TableName} SET ImporterData ='{importers.ImporterData}', MadeIn = '{importers.MadeIn}' WHERE ID = {importers.ID} ");
                }else
                {
                    dynamicDb.ExecuteNonQuery($@"INSERT INTO {importerCatalog.TableName} (ImporterData, MadeIN, Code) VALUES ('{importers.ImporterData}', '{importers.MadeIn}', '{importers.VendorCode}') ");
                }
            }
        }
    }
}
