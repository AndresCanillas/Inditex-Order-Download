using Service.Contracts;
using Service.Contracts.PrintLocal;
using System;
using System.Collections.Generic;
using System.Text;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace WebLink.Services
{
	public class ProcessArticleStatusChange : EQEventHandler<PLArticleStatusChangeEvent>
	{
		private IPrinterJobRepository repo;
		private IOrderLogService log;

		public ProcessArticleStatusChange(IPrinterJobRepository repo, IOrderLogService log)
		{
			this.repo = repo;
			this.log = log;
		}

		public override EQEventHandlerResult HandleEvent(PLArticleStatusChangeEvent e)
		{
			var job = repo.GetByID(e.PrintJobID, true);
			var status = ConvertFromPrintLocalStatus(e.Status);
			if(status != null)
			{
				repo.UpdateJobState(e.PrintJobID, status.Value);
			}
			return new EQEventHandlerResult();
		}

		private JobStatus? ConvertFromPrintLocalStatus(PLJobStatus status)
		{
			switch (status)
			{
				case PLJobStatus.Printing:
					return JobStatus.Executing;
				case PLJobStatus.Completed:
					return JobStatus.Completed;
				case PLJobStatus.Cancelled:
					return JobStatus.Cancelled;
				default:
					return null;
			}
		}
	}
}

