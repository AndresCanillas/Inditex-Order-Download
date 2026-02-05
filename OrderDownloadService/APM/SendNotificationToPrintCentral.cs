using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.PrintCentral;
using System;

namespace OrderDonwLoadService
{
    public class SendNotificationToPrintCentral : EQEventHandler<NotificationReceivedEvent>
    {
        private readonly IConnectionManager _db;
        private readonly IAppLog _log;



        public SendNotificationToPrintCentral(
            IAppLog appLog,
            IConnectionManager db)
        {

            _log = appLog;
            _db = db;
        }



        public override EQEventHandlerResult HandleEvent(NotificationReceivedEvent e)
        {
            using (var conn = _db.OpenDB())
            {
                var companyInfo = GetCompanyInfo(conn, e.CompanyID);
                var messge = e.Message.Replace("#CompanyCode", companyInfo.CompanyCode);
                if (SaveNotificationIntoPrintCentral(conn, e.Title, messge, 1, 0, messge.GetHashCode().ToString(), e.FileName, e.JsonData))
                {
                    _log.LogMessage($"Notication {e.Title} was Sended to PrintCentral.");

                    return EQEventHandlerResult.OK;
                }
                else
                {
                    _log.LogMessage($"Error: Can´t to save notification {e.Title}  into Print central Database.");
                }
            }
            return EQEventHandlerResult.Delay5;
        }
        private bool SaveNotificationIntoPrintCentral(IDBX db, string title, string message, int locationId, int projectID, string nkey, string fileName, string jsonData = null)
        {
            try
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
                       ,'{fileName}'
                       ,'InditexJson.OrderDonwLoadService'
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
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        private CompanyInfo GetCompanyInfo(IDBX db, int companyID)
        {
            var sql = @"
                SELECT ID, Name, CompanyCode
                FROM Companies c 
                WHERE ID = @companyID"
            ;

            var companyInfo = db.SelectOne<CompanyInfo>(sql, companyID);

            return companyInfo;
        }

    }
}