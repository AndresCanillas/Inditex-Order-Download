using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebLink.Contracts.Models
{
    public interface IMappingRepository : IGenericRepository<IDataImportMapping>
    {
        List<IDataImportMapping> GetByProjectID(int projectid);
        List<IDataImportMapping> GetByProjectID(PrintDB ctx, int projectid);

        List<IDataImportColMapping> GetColumnMappings(int id);
        List<IDataImportColMapping> GetColumnMappings(PrintDB ctx, int id);

        IDataImportColMapping AddColumn(int id);
        IDataImportColMapping AddColumn(PrintDB ctx, int id);

        IDataImportColMapping InsertColumn(int id, int pos);
        IDataImportColMapping InsertColumn(PrintDB ctx, int id, int pos);

        IDataImportColMapping MoveColumnDown(int id);
        IDataImportColMapping MoveColumnDown(PrintDB ctx, int id);

        IDataImportColMapping MoveColumnUp(int id);
        IDataImportColMapping MoveColumnUp(PrintDB ctx, int id);

        void DeleteColumn(int colid);
        void DeleteColumn(PrintDB ctx, int colid);

        void UpdateColumnMappings(List<IDataImportColMapping> columns);
        void UpdateColumnMappings(PrintDB ctx, List<IDataImportColMapping> columns);

        List<EncodingDTO> GetEncodings();
        List<CultureDTO> GetCultures();

        IDataImportMapping Duplicate(int mappingid, string name);
        IDataImportMapping Duplicate(PrintDB ctx, int mappingid, string name);

        List<IDataImportColMapping> InitializeMappingsFromCatalog(int mappingid, int catalogid);
        List<IDataImportColMapping> InitializeMappingsFromCatalog(PrintDB ctx, int mappingid, int catalogid);

        DocumentImportConfiguration GetDocumentImportConfiguration(string userName, int projectid, IFSFile file);
        DocumentImportConfiguration GetDocumentImportConfiguration(PrintDB ctx, string userName, int projectid, IFSFile file);

        DocumentImportConfiguration GetDocumentImportConfiguration(string userName, int projectid, string catalogName, IFSFile file);
        DocumentImportConfiguration GetDocumentImportConfiguration(PrintDB ctx, string userName, int projectid, string catalogName, IFSFile file);

        Task<DocumentImportConfiguration> GetDocumentImportConfigurationAsync(string userName, int projectid, string catalogName, IFSFile file);
        Task<DocumentImportConfiguration> GetDocumentImportConfigurationAsync(PrintDB ctx, string userName, int projectid, string catalogName, IFSFile file);

        DocumentImportConfiguration GetBatchFileImportConfiguration(string userName, int projectid, IFSFile file);
        DocumentImportConfiguration GetBatchFileImportConfiguration(PrintDB ctx, string userName, int projectid, IFSFile file);

        DocumentImportConfiguration GetCatalogImportConfiguration(string userName, int catalogid, IFSFile file);
        DocumentImportConfiguration GetCatalogImportConfiguration(PrintDB ctx, string userName, int catalogid, IFSFile file);

        void CreateMappingsFromCatalog(DocumentImportConfiguration config, List<FieldDefinition> fields);
        DocumentColumnType GetMappingTypeFromColumnType(ColumnType type);
    }
}
