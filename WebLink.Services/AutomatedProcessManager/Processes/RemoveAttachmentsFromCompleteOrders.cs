using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Services.Automated
{
	public class RemoveAttachmentsFromCompleteOrders : IAutomatedProcess
	{
		private IFactory factory;
		private IConnectionManager connManager;
		private IAppConfig config;
		private ILogService log;

		private CancellationTokenSource cts;
		private ManualResetEvent waitHandle;

		public RemoveAttachmentsFromCompleteOrders(IFactory factory, IConnectionManager connManager, IAppConfig config, ILogService log)
		{
			this.factory = factory;
			this.connManager = connManager;
			this.config = config;
			this.log = log;
			waitHandle = new ManualResetEvent(false);
			cts = new CancellationTokenSource();
		}

		public TimeSpan GetIdleTime()
		{
			return TimeSpan.MaxValue;
		}

		public void OnLoad()
		{
			_ = StartLoop();
		}

		public void OnUnload()
		{
			cts.Cancel();
			waitHandle.WaitOne(1000);
			cts.Dispose();
		}

		public void OnExecute()
		{
		}

		private async Task StartLoop()
		{
			while (!cts.Token.IsCancellationRequested)
			{
				await RemoveAttachments();
				await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
			}
			waitHandle.Set();
		}

		private async Task RemoveAttachments()
		{
			try
			{
				using(var conn = await connManager.OpenDBAsync("MainDB"))
				{
					var attachmentList = await conn.SelectAsync<CompleteOrderAttachment>(@"
						select top 100 att.FileID, att.CategoryID, att.ID 
							from FSOrderStore.dbo.FSAttachment att
							join CompanyOrders o on att.FileID = o.ID
							where o.OrderStatus in (6,7)
					");
					if(attachmentList.Count > 0)
					{
						log.LogMessage($"Deleting {attachmentList.Count} attachments from completed and cancelled orders...");
						foreach(var att in attachmentList)
						{
							await conn.ExecuteNonQueryAsync(@"
								delete from FSOrderStore.dbo.FSAttachment
									where
										FileID = @fileid and
										CategoryID = @categoryid and
										ID = @attachid
							", att.FileID, att.CategoryID, att.ID);
						}
					}
				}
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
		}
	}

	public class CompleteOrderAttachment
	{
		public int FileID;
		public int CategoryID;
		public int ID;
	}
}
