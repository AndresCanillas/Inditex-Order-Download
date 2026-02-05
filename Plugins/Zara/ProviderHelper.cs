using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonColor
{
    public static class ProviderVerifier
    {
        public static void ValidateProviderData(int companyID,
            Supplier supplierData, 
            string orderNumber, 
            string projectID, 
            IDBX db,
            ILogService log,
            string FileName)
        {

            var clientReference = supplierData.supplierCode;

            if(int.TryParse(clientReference, out int referenceInt))
            {
                clientReference = referenceInt.ToString();
            }
            // database names defined in DocumentService
           
                // looking the ref inner print central
                if(!IsProviderExist(db, clientReference, companyID))
                {
                    log.LogMessage($"Inditex.ZaraHangtag.Kids.Plugin.OnPrepareFile, no se ha encontrado el proveedor {clientReference}");
                    // notify Customer
                    var companyInfo = GetCompanyInfo(db, companyID);

                    var title = $"The client reference ({clientReference}) not found for Order Number {orderNumber}, CompanyID= {companyID} with ProjectID= {projectID}.";
                    var message = $"Error while procesing file {FileName}.\r\nThe order refers to a supplier code that is not registered in the system: {companyInfo.CompanyCode}";
                    var nkey = message.GetHashCode().ToString();
                    CreateNotification(db, title, message, 1, 0, nkey, Newtonsoft.Json.JsonConvert.SerializeObject(supplierData));

                    // TODO: pending to send Email
                }


            
        }
        private static bool IsProviderExist(IDBX db, string reference, int companyID)
        {
            var exist = true;

            var sql = @"
                SELECT ID, CompanyID, ProviderCompanyID, DefaultProductionLocation, ClientReference
                FROM CompanyProviders pv 
                WHERE CompanyID = @companyID
                AND ClientReference = @clientReference";

            var providerInfo = db.SelectOne<ProviderInfo>(sql, companyID, reference);

            if(providerInfo == null)
                exist = false;

            return exist;
        }

        private static CompanyInfo GetCompanyInfo(IDBX db, int companyID)
        {
            var sql = @"
                SELECT ID, Name, CompanyCode
                FROM Companies c 
                WHERE ID = @companyID";

            var companyInfo = db.SelectOne<CompanyInfo>(sql, companyID);

            return companyInfo;
        }

        // TODO: require check if notification key already registered
        // TODO: esta funcion sse debe montar en el servicecontract

        private static void CreateNotification(IDBX db, string title, string message, int locationId, int projectID, string nkey, string jsonData = null)
        {
            var NotificationTypeFTPFileWhatcher = 3;
            var data = jsonData != null ? jsonData : "{}";
            var msg = message;

            var sql = $@"INSERT INTO [dbo].[Notifications]
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
           (1
           ,{NotificationTypeFTPFileWhatcher}
           ,'{Service.Contracts.Authentication.Roles.IDTCostumerService}'
           ,''
           ,'InditexZaraHangtagKids.Plugin.DocumentImport/{nkey}'
           ,'InditexZaraHangtagKids.Plugin.DocumentImport'
           ,@title
           ,@msg
           ,@data
           ,0
           ,1
           ,null
           ,{locationId}
           ,{projectID}
           ,'SysAdmin'
           ,GETDATE()
           ,'System'
           ,GETDATE())";

            db.ExecuteNonQuery(sql, title, msg, data);


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
