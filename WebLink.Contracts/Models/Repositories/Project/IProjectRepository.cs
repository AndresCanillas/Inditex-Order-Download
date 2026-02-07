using Service.Contracts.Database;
using System.Collections.Generic;

namespace WebLink.Contracts.Models
{
    public interface IProjectRepository : IGenericRepository<IProject>
    {
        List<IProject> GetByBrandID(int brandid, bool showAll);
        List<IProject> GetByBrandID(PrintDB ctx, int brandid, bool showAll);

        IProject GetSelectedProject();
        IProject GetSelectedProject(PrintDB ctx);

        IProject GetDefaultProject(int companyid);
        IProject GetDefaultProject(PrintDB ctx, int companyid);

        List<DBFieldInfo> GetDBFields(int id);
        List<DBFieldInfo> GetDBFields(PrintDB ctx, int id);

        void Hide(int id);
        void Hide(PrintDB ctx, int id);

        void AssignRFIDConfig(int projectid, int configid);
        void AssignRFIDConfig(PrintDB ctx, int projectid, int configid);

        void AssignOrderWorkflowConfig(int projectid, int configid);
        void AssignOrderWorkflowConfig(PrintDB ctx, int projectid, int configid);

        List<FieldDefinition> GetCatalogFields(int id);
        List<FieldDefinition> GetCatalogFields(PrintDB ctx, int id);

        List<string> GetEmailRecipients(int projectid);
        List<string> GetEmailRecipients(PrintDB ctx, int projectid);

        List<string> GetCustomerEmails(int projectid);
        List<string> GetCustomerEmails(PrintDB ctx, int projectid);

        List<string> GetClientEmails(int projectID);
        List<string> GetClientEmails(PrintDB ctx, int projectID);

        string DecryptString(string data);

        void ExportProject(int projectid, string filePath);
        void ImportProject(int projectId, string filePath);

        bool CompositionCatalogsExist(int projectID);
        bool CompositionCatalogsExist(PrintDB ctx, int projectID);

        ICatalog AddCompositionCatalogs(int id);
        ICatalog AddCompositionCatalogs(PrintDB ctx, int id);

        IEnumerable<IProject> GetByCompanyID(int companyID, bool showAll);
        IEnumerable<IProject> GetByCompanyID(PrintDB ctx, int companyID, bool showAll);
        IEnumerable<int> GetProjectsForCustomerService(string userid);
        IEnumerable<int> GetProjectsForCustomerService(PrintDB ctx, string userid);

        string GetManualEntryUrl(int projectId);

        List<IProject> GetByBrandIDME(int brandid, bool showAll);

        void SendEmailIfErrorSupplier();


    }
}
