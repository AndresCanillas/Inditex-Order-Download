using Newtonsoft.Json;
using Service.Contracts.Database;
using Services.Core;
using StructureInditexOrderFile;

namespace InidtexApi
{
    public interface IProviderVerifier
    {
        void ValidateProviderData(int companyID,
            Supplier supplierData,
            string orderNumber,
            string projectID,
            IDBX db,
            ILogService log,
            string fileName);
    }

    public interface IProviderRepository
    {
        bool ProviderExists(IDBX db, string reference, int companyID);
        CompanyInfo GetCompanyInfo(IDBX db, int companyID);
    }

    public interface INotificationWriter
    {
        void CreateNotification(
            IDBX db,
            int companyID,
            string title,
            string message,
            int locationId,
            int projectID,
            string nkey,
            string jsonData = null);
    }

    public class ProviderVerifier : IProviderVerifier
    {
        private readonly IProviderRepository repository;
        private readonly INotificationWriter notificationWriter;

        public ProviderVerifier(IProviderRepository repository, INotificationWriter notificationWriter)
        {
            this.repository = repository;
            this.notificationWriter = notificationWriter;
        }

        public void ValidateProviderData(int companyID,
            Supplier supplierData,
            string orderNumber,
            string projectID,
            IDBX db,
            ILogService log,
            string fileName)
        {
            var clientReference = supplierData.supplierCode;

            if(int.TryParse(clientReference, out int referenceInt))
            {
                clientReference = referenceInt.ToString();
            }

            if(!repository.ProviderExists(db, clientReference, companyID))
            {
                log.LogMessage($"Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, no se ha encontrado el proveedor {clientReference}");
                var companyInfo = repository.GetCompanyInfo(db, companyID);

                var title = $"The client reference ({clientReference}) not found for Order Number {orderNumber}, CompanyID= {companyID} with ProjectID= {projectID}.";
                var message = $"Error while procesing file {fileName}.\r\nThe order refers to a supplier code that is not registered in the system: {companyInfo.CompanyCode}";
                var nkey = message.GetHashCode().ToString();
                notificationWriter.CreateNotification(db, companyID, title, message, 1, 0, nkey, JsonConvert.SerializeObject(supplierData));

                // TODO: pending to send Email
            }
        }
    }

    public class ProviderRepository : IProviderRepository
    {
        public bool ProviderExists(IDBX db, string reference, int companyID)
        {
            var sql = @"
                SELECT ID, CompanyID, ProviderCompanyID, DefaultProductionLocation, ClientReference
                FROM CompanyProviders pv 
                WHERE CompanyID = @companyID
                AND ClientReference = @clientReference";

            var providerInfo = db.SelectOne<ProviderInfo>(sql, companyID, reference);

            return providerInfo != null;
        }

        public CompanyInfo GetCompanyInfo(IDBX db, int companyID)
        {
            var sql = @"
                SELECT ID, Name, CompanyCode
                FROM Companies c 
                WHERE ID = @companyID";

            return db.SelectOne<CompanyInfo>(sql, companyID);
        }
    }

    public class NotificationWriter : INotificationWriter
    {
        public void CreateNotification(
            IDBX db,
            int companyID,
            string title,
            string message,
            int locationId,
            int projectID,
            string nkey,
            string jsonData = null)
        {
            var notificationTypeFTPFileWatcher = 3;
            var data = jsonData ?? "{}";

            var sql = @"INSERT INTO [dbo].[Notifications]
           ([CompanyID]
           ,[Type]
           ,[IntendedRole]
           ,[IntendedUser]
           ,[NKey]
           ,[Source]
           ,[Title]
           ,[Message]
           ,[Data]
           ,[AutoDismiss]
           ,[Count]
           ,[Action]
           ,[LocationID]
           ,[ProjectID]
           ,[CreatedBy]
           ,[CreatedDate]
           ,[UpdatedBy]
           ,[UpdatedDate])
     VALUES
           (@companyID
           ,@type
           ,@role
           ,@intendedUser
           ,@nkey
           ,@source
           ,@title
           ,@msg
           ,@data
           ,@autoDismiss
           ,@count
           ,@action
           ,@locationId
           ,@projectID
           ,@createdBy
           ,GETDATE()
           ,@updatedBy
           ,GETDATE())";

            db.ExecuteNonQuery(
                sql,
                companyID,
                notificationTypeFTPFileWatcher,
                Service.Contracts.Authentication.Roles.IDTCostumerService,
                string.Empty,
                $"InditexZaraHangtagKids.Plugin.DocumentImport/{nkey}",
                "InditexZaraHangtagKids.Plugin.DocumentImport",
                title,
                message,
                data,
                0,
                1,
                null,
                locationId,
                projectID,
                "SysAdmin",
                "System");
        }
    }

    public class ProviderInfo
    {
        public int ID { get; set; }
        public int CompanyID { get; set; }
        public int ProviderCompanyID { get; set; }
        public int DefaultProductionLocation { get; set; }
        public string ClientReference { get; set; }
    }

    public class CompanyInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string CompanyCode { get; set; }
    }
}
