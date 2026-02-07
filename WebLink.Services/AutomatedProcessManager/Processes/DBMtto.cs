using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Service.Contracts;
using Service.Contracts.Database;
using WebLink.Contracts;
using Newtonsoft.Json;
using WebLink.Contracts.Models;
using Services.Core;

namespace WebLink.Services.Automated
{
	/* ===================================================================================
	 * DBMtto
	 * 
	 * Executes every 90 minutes to check if there are any records that need to be deleted
	 * from the database to free up space.
	 * 
	 * NOTE: There are different conditions for different tables.
	 * 
	 * ===================================================================================*/

	public class DBMtto : IAutomatedProcess
	{
		private IFactory factory;

		public DBMtto(IFactory factory)
		{
			this.factory = factory;
		}

		public TimeSpan GetIdleTime()
		{
			return TimeSpan.FromMinutes(90);
		}

		public void OnLoad() { }

		public void OnUnload() { }

		public void OnExecute()
		{
			var connManager = factory.GetInstance<IDBConnectionManager>();
			using (var conn = connManager.OpenUsersDB())
			{
				DeleteExpiredPasswordResetTokens(conn);
			}
			using(var conn = connManager.OpenWebLinkDB())
			{
				DeleteUnreferencedRFIDParameters(conn);
				DeleteUnreferencedMappings(conn);
				DeleteAutodismissNotifications(conn, TimeSpan.FromDays(7));
                DeleteUnreferencedOrderWorkflowConfig(conn);
			}
		}


		private void DeleteExpiredPasswordResetTokens(IDBX conn)
		{
			try
			{
				conn.ExecuteNonQuery("delete from [ResetTokens] where ValidUntil < getdate()");
			}
			catch(Exception ex)
			{
				var log = factory.GetInstance<ILogService>();
				log.LogException(ex);
			}
		}


		private void DeleteUnreferencedRFIDParameters(IDBX conn)
		{
			try
			{
				conn.ExecuteNonQuery(@"
					delete from RFIDParameters where 
						ID not in (select RFIDConfigID from Companies where not RFIDConfigID is null) and
						ID not in (select RFIDConfigID from Brands where not RFIDConfigID is null) and
						ID not in (select RFIDConfigID from Projects where not RFIDConfigID is null)");
			}
			catch (Exception ex)
			{
				var log = factory.GetInstance<ILogService>();
				log.LogException(ex);
			}
		}

        private void DeleteUnreferencedOrderWorkflowConfig(IDBX conn)
        {
            try
            {
                conn.ExecuteNonQuery(@"
					delete from OrderWorkflowConfigs where 
						ID not in (select OrderWorkflowConfigID from Projects where not OrderWorkflowConfigID is null)
                        and CreatedDate < @date
                    ", DateTime.Now.AddDays(-1));
            }
            catch(Exception ex)
            {
                var log = factory.GetInstance<ILogService>();
                log.LogException(ex);
            }
        }

        private void DeleteUnreferencedMappings(IDBX conn)
		{
			try
			{
				conn.ExecuteNonQuery(@"
					delete from DataImportColMapping where DataImportMappingID in (
						select ID from DataImportMappings where ProjectID not in (select ID from Projects)
					)
					delete from DataImportMappings where ProjectID not in (select ID from Projects)
				");
			}
			catch (Exception ex)
			{
				var log = factory.GetInstance<ILogService>();
				log.LogException(ex);
			}
		}


		private void DeleteAutodismissNotifications(IDBX conn, TimeSpan threshold)
		{
			try
			{
				DateTime date = DateTime.Now - threshold;
				conn.ExecuteNonQuery("delete from Notifications where AutoDismiss = 1 and UpdatedDate < @date", date);
			}
			catch (Exception ex)
			{
				var log = factory.GetInstance<ILogService>();
				log.LogException(ex);
			}
		}
	}
}
