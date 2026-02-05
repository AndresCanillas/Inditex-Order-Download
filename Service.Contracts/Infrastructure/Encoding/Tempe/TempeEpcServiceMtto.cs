using Services.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{
	public class TempeEpcServiceMtto : IAutomatedProcess
	{
		private readonly EpcRepositoryTempe repo;
		private readonly ILogService log;

		public TempeEpcServiceMtto(EpcRepositoryTempe repo, ILogService log)
		{
			this.repo = repo;
			this.log = log;
		}

		public TimeSpan GetIdleTime() => TimeSpan.FromMinutes(1);

		public void OnLoad() { }

		public void OnUnload() { }

		public void OnExecute()
		{
			// Deletes epcs for orders that were validated 90 days ago, by then they should have already been produced.
			// At any rate, deleting these records will simply cause the order to be revalidated and new epcs allocated
			// in case the order is reprocessed again.
			try
			{
				repo.DeleteOldOrders(TimeSpan.FromDays(90));
			}
			catch(Exception ex)
			{
				log.LogException(ex);
			}
		}
	}
}
